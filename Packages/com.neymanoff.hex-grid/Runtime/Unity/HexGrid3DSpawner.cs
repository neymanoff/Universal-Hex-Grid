using System;
using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Maps a 2D authoring <see cref="TileBase"/> asset to a physical 3D hex prefab.
    /// </summary>
    [Serializable]
    public struct TilePrefabMapping
    {
        [Tooltip("The 2D Tile asset painted on the authoring Tilemap.")]
        public TileBase Tile;

        [Tooltip("The corresponding 3D physical prefab (e.g. low-poly terrain mesh) to instantiate.")]
        public GameObject Prefab;

        [Tooltip("Custom scale applied specifically to this tile's spawned instance (defaults to (1, 1, 1) if left at (0, 0, 0)).")]
        public Vector3 Scale;

        [Tooltip("Individual Euler rotation offset applied specifically to this tile's spawned instance.")]
        public Vector3 RotationOffset;

        [Tooltip("Per-prefab vertical height adjustment applied on top of the surface alignment.")]
        public float VerticalOffset;

        public Vector3 EffectiveScale => Scale == Vector3.zero ? Vector3.one : Scale;
    }

    /// <summary>
    /// Spawns 3D physical hex meshes and prefabs driven by 2D authoring Tilemap layouts.
    /// Supports multi-layer tilemap scanning (Walkable, Obstacles, Props), bottom-to-surface alignment,
    /// and automatic procedural Pointy-Top Greybox hex fallbacks.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Grid 3D Spawner")]
    public class HexGrid3DSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Bridge component providing coordinate conversion and layout dimensions.")]
        [SerializeField] private HexTilemapBridge _bridge;

        [Tooltip("Source Tilemaps to read cells from (e.g. Walkable Tilemap, Obstacle Tilemap). If empty, auto-populates from Bridge.")]
        [SerializeField] private List<Tilemap> _sourceTilemaps = new();

        [Tooltip("Transform container under which spawned 3D hexes are parented.")]
        [SerializeField] private Transform _spawnContainer;

        [Header("Prefabs & Mappings")]
        [Tooltip("Default 3D prefab used for cells without a specific tile mapping. If null, a procedural Pointy-Top 3D hex is generated.")]
        [SerializeField] private GameObject _defaultHexPrefab;

        [Tooltip("Specific tile-to-prefab mappings for varied biomes and obstacles (e.g. grass, water, trees, rocks).")]
        [SerializeField] private List<TilePrefabMapping> _tileMappings = new();

        [Header("Spawn Options")]
        [Tooltip("If true, automatically spawns the 3D grid in Start().")]
        [SerializeField] private bool _autoSpawnOnStart = false;

        [Tooltip("If true, automatically offsets each object so its bottom (via Collider/Renderer) rests flush on the cell surface (Y = 0).")]
        [SerializeField] private bool _alignBottomToSurface = true;

        [Tooltip("If true, temporarily disables 2D TilemapRenderers while 3D meshes are active to avoid visual Z-fighting.")]
        [SerializeField] private bool _hideTilemapsOnSpawn = false;

        [Tooltip("If true, generates procedural 3D Pointy-Top hex prisms for unmapped tiles when Default Hex Prefab is not set. If false, unmapped tiles are skipped.")]
        [SerializeField] private bool _generateProceduralHexForUnmapped = false;

        [Tooltip("Local scale applied to each spawned 3D hex instance.")]
        [SerializeField] private Vector3 _instanceScale = Vector3.one;

        [Tooltip("Euler rotation offset applied to each spawned 3D hex instance.")]
        [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

        [Tooltip("Global vertical height offset applied to spawned 3D instances.")]
        [SerializeField] private float _verticalOffset = 0f;

        private readonly List<GameObject> _spawnedInstances = new();
        private static Mesh _proceduralHexMesh;
        private static Material _proceduralDefaultMaterial;

        public IReadOnlyList<GameObject> SpawnedInstances => _spawnedInstances;
        public IReadOnlyList<Tilemap> SourceTilemaps => _sourceTilemaps;
        public GameObject DefaultHexPrefab { get => _defaultHexPrefab; set => _defaultHexPrefab = value; }
        public bool AlignBottomToSurface { get => _alignBottomToSurface; set => _alignBottomToSurface = value; }
        public bool HideTilemapsOnSpawn { get => _hideTilemapsOnSpawn; set => _hideTilemapsOnSpawn = value; }
        public bool GenerateProceduralHexForUnmapped { get => _generateProceduralHexForUnmapped; set => _generateProceduralHexForUnmapped = value; }
        public List<TilePrefabMapping> TileMappings => _tileMappings;

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        /// <summary>
        /// Automatically resolves bridge and source tilemap references if unassigned.
        /// </summary>
        public void ResolveReferences()
        {
            if (_bridge == null) _bridge = GetComponentInParent<HexTilemapBridge>();
            if (_spawnContainer == null) _spawnContainer = transform;

            if (_sourceTilemaps == null) _sourceTilemaps = new List<Tilemap>();
            if (_sourceTilemaps.Count == 0 && _bridge != null)
            {
                if (_bridge.WalkableTilemap != null && !_sourceTilemaps.Contains(_bridge.WalkableTilemap))
                    _sourceTilemaps.Add(_bridge.WalkableTilemap);
                if (_bridge.ObstacleTilemap != null && !_sourceTilemaps.Contains(_bridge.ObstacleTilemap))
                    _sourceTilemaps.Add(_bridge.ObstacleTilemap);
            }
        }

        /// <summary>
        /// Explicit configuration method for tests and procedural setup.
        /// </summary>
        public void Configure(HexTilemapBridge bridge, IEnumerable<Tilemap> sourceTilemaps = null, GameObject defaultPrefab = null)
        {
            _bridge = bridge;
            _defaultHexPrefab = defaultPrefab;
            if (sourceTilemaps != null)
            {
                _sourceTilemaps.Clear();
                _sourceTilemaps.AddRange(sourceTilemaps);
            }
            ResolveReferences();
        }

        private void Start()
        {
            if (_autoSpawnOnStart)
            {
                SpawnGrid();
            }
        }

        /// <summary>
        /// Instantiates 3D physical hex prefabs across all configured source Tilemaps.
        /// </summary>
        [ContextMenu("Spawn 3D Grid")]
        public void SpawnGrid()
        {
            ClearGrid();
            ResolveReferences();

            var tilemapsToScan = GetUniqueSourceTilemaps();
            if (tilemapsToScan.Count == 0)
            {
                Debug.LogWarning("[HexGrid3DSpawner] No source Tilemaps assigned or available to spawn 3D hexes.", this);
                return;
            }

            var container = _spawnContainer != null ? _spawnContainer : transform;
            int totalSpawned = 0;

            foreach (var tilemap in tilemapsToScan)
            {
                var bounds = tilemap.cellBounds;

                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        var tile = tilemap.GetTile(cell);
                        if (tile == null) continue;

                        var prefab = ResolvePrefabForTile(tile, out float perTileOffset, out Vector3 perTileScale, out Vector3 perTileRotation);

                        var worldPos = _bridge != null
                            ? _bridge.HexToWorld(HexTilemapBridge.TilemapCellToHex(cell))
                            : tilemap.GetCellCenterWorld(cell);

                        worldPos.y += _verticalOffset + perTileOffset;

                        if (prefab != null)
                        {
                            var rot = Quaternion.Euler(_rotationOffset + perTileRotation);
                            var instance = Instantiate(prefab, worldPos, rot, container);
                            Vector3 effectiveScale = Vector3.Scale(prefab.transform.localScale, Vector3.Scale(_instanceScale, perTileScale));
                            instance.transform.localScale = effectiveScale;
                            instance.name = $"Hex3D_{cell.x}_{cell.y}_{tile.name}";

                            // Apply bottom-to-surface alignment so bottom rests at worldPos.y
                            if (_alignBottomToSurface)
                            {
                                float bottomOffset = CalculateBottomOffset(instance);
                                instance.transform.position += Vector3.up * bottomOffset;
                                Physics.SyncTransforms();
                            }

                            _spawnedInstances.Add(instance);
                            totalSpawned++;
                        }
                        else if (_generateProceduralHexForUnmapped)
                        {
                            // Procedural Greybox Hexagon fallback (top face at worldPos.y)
                            var instance = CreateProceduralHexInstance(worldPos, $"Hex3D_{cell.x}_{cell.y}_{tile.name}_Procedural", container);
                            _spawnedInstances.Add(instance);
                            totalSpawned++;
                        }
                    }
                }
            }

            if (_hideTilemapsOnSpawn)
            {
                SetTilemapsVisibility(tilemapsToScan, false);
            }

            Debug.Log($"[HexGrid3DSpawner] Successfully spawned {totalSpawned} 3D hex instances across {tilemapsToScan.Count} tilemap layer(s).", this);
        }

        /// <summary>
        /// Clears all previously spawned 3D hex instances and restores Tilemap visibility.
        /// </summary>
        [ContextMenu("Clear 3D Grid")]
        public void ClearGrid()
        {
            for (int i = _spawnedInstances.Count - 1; i >= 0; i--)
            {
                var instance = _spawnedInstances[i];
                if (instance != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(instance);
                    else
                        Destroy(instance);
#else
                    Destroy(instance);
#endif
                }
            }
            _spawnedInstances.Clear();

            var container = _spawnContainer != null ? _spawnContainer : transform;
            if (container != null && container != transform)
            {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    var child = container.GetChild(i).gameObject;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(child);
                    else
                        Destroy(child);
#else
                    Destroy(child);
#endif
                }
            }

            var tilemapsToScan = GetUniqueSourceTilemaps();
            SetTilemapsVisibility(tilemapsToScan, true);
        }

        private List<Tilemap> GetUniqueSourceTilemaps()
        {
            var result = new List<Tilemap>();
            if (_sourceTilemaps != null)
            {
                for (int i = 0; i < _sourceTilemaps.Count; i++)
                {
                    if (_sourceTilemaps[i] != null && !result.Contains(_sourceTilemaps[i]))
                        result.Add(_sourceTilemaps[i]);
                }
            }

            if (result.Count == 0 && _bridge != null)
            {
                if (_bridge.WalkableTilemap != null) result.Add(_bridge.WalkableTilemap);
                if (_bridge.ObstacleTilemap != null && !result.Contains(_bridge.ObstacleTilemap))
                    result.Add(_bridge.ObstacleTilemap);
            }
            return result;
        }

        private void SetTilemapsVisibility(List<Tilemap> tilemaps, bool visible)
        {
            for (int i = 0; i < tilemaps.Count; i++)
            {
                if (tilemaps[i] != null)
                {
                    var renderer = tilemaps[i].GetComponent<TilemapRenderer>();
                    if (renderer != null) renderer.enabled = visible;
                }
            }
        }

        private GameObject ResolvePrefabForTile(TileBase tile, out float perTileOffset, out Vector3 perTileScale, out Vector3 perTileRotation)
        {
            perTileOffset = 0f;
            perTileScale = Vector3.one;
            perTileRotation = Vector3.zero;

            if (tile != null && _tileMappings != null)
            {
                for (int i = 0; i < _tileMappings.Count; i++)
                {
                    if (_tileMappings[i].Tile == tile && _tileMappings[i].Prefab != null)
                    {
                        var mapping = _tileMappings[i];
                        perTileOffset = mapping.VerticalOffset;
                        perTileScale = mapping.EffectiveScale;
                        perTileRotation = mapping.RotationOffset;
                        return mapping.Prefab;
                    }
                }
            }
            return _defaultHexPrefab;
        }

        /// <summary>
        /// Calculates the vertical delta required to lift an object so its lowest point (collider or renderer) rests on its position.
        /// </summary>
        public static float CalculateBottomOffset(GameObject instance)
        {
            if (instance == null) return 0f;

            var col = instance.GetComponentInChildren<Collider>();
            if (col != null)
            {
                if (col is CapsuleCollider capsule)
                {
                    float halfHeight = Mathf.Max(capsule.radius, capsule.height * 0.5f) * instance.transform.lossyScale.y;
                    float localBottom = capsule.center.y * instance.transform.lossyScale.y - halfHeight;
                    return -localBottom;
                }
                if (col is BoxCollider box)
                {
                    float halfHeight = box.size.y * 0.5f * instance.transform.lossyScale.y;
                    float localBottom = box.center.y * instance.transform.lossyScale.y - halfHeight;
                    return -localBottom;
                }
                if (col is SphereCollider sphere)
                {
                    float localBottom = (sphere.center.y - sphere.radius) * instance.transform.lossyScale.y;
                    return -localBottom;
                }

                float bottomY = col.bounds.min.y;
                return instance.transform.position.y - bottomY;
            }

            var rend = instance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float bottomY = rend.bounds.min.y;
                return instance.transform.position.y - bottomY;
            }

            return 0f;
        }

        /// <summary>
        /// Generates or retrieves a shared procedural Pointy-Top 3D hexagonal prism mesh.
        /// Top face is flush at Y = 0; base extends downward with depth 0.25.
        /// </summary>
        public static Mesh GetOrCreateProceduralHexMesh()
        {
            if (_proceduralHexMesh != null) return _proceduralHexMesh;

            var mesh = new Mesh { name = "Procedural_PointyTop_Hex_Prism" };
            const float radius = 0.5f;
            const float depth = 0.25f;

            var topCenter = Vector3.zero;
            var bottomCenter = new Vector3(0, -depth, 0);

            var topVertices = new Vector3[6];
            var bottomVertices = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angleRad = (30f + 60f * i) * Mathf.Deg2Rad;
                float x = radius * Mathf.Cos(angleRad);
                float z = radius * Mathf.Sin(angleRad);
                topVertices[i] = new Vector3(x, 0f, z);
                bottomVertices[i] = new Vector3(x, -depth, z);
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Top cap (6 triangles facing Vector3.up)
            int topStartIndex = vertices.Count;
            vertices.Add(topCenter);
            normals.Add(Vector3.up);
            for (int i = 0; i < 6; i++)
            {
                vertices.Add(topVertices[i]);
                normals.Add(Vector3.up);
            }
            for (int i = 0; i < 6; i++)
            {
                triangles.Add(topStartIndex);
                triangles.Add(topStartIndex + 1 + i);
                triangles.Add(topStartIndex + 1 + ((i + 1) % 6));
            }

            // Bottom cap (6 triangles facing Vector3.down)
            int botStartIndex = vertices.Count;
            vertices.Add(bottomCenter);
            normals.Add(Vector3.down);
            for (int i = 0; i < 6; i++)
            {
                vertices.Add(bottomVertices[i]);
                normals.Add(Vector3.down);
            }
            for (int i = 0; i < 6; i++)
            {
                triangles.Add(botStartIndex);
                triangles.Add(botStartIndex + 1 + ((i + 1) % 6));
                triangles.Add(botStartIndex + 1 + i);
            }

            // 6 side quad faces
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                var p0 = topVertices[i];
                var p1 = topVertices[next];
                var p2 = bottomVertices[next];
                var p3 = bottomVertices[i];

                var sideNormal = Vector3.Cross(p1 - p0, p3 - p0).normalized;

                int sideStart = vertices.Count;
                vertices.Add(p0); normals.Add(sideNormal);
                vertices.Add(p1); normals.Add(sideNormal);
                vertices.Add(p2); normals.Add(sideNormal);
                vertices.Add(p3); normals.Add(sideNormal);

                triangles.Add(sideStart);
                triangles.Add(sideStart + 1);
                triangles.Add(sideStart + 2);

                triangles.Add(sideStart);
                triangles.Add(sideStart + 2);
                triangles.Add(sideStart + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            _proceduralHexMesh = mesh;
            return _proceduralHexMesh;
        }

        private GameObject CreateProceduralHexInstance(Vector3 worldPos, string name, Transform container)
        {
            var go = new GameObject(name);
            go.transform.SetParent(container, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(_rotationOffset);
            go.transform.localScale = _instanceScale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrCreateProceduralHexMesh();

            var mr = go.AddComponent<MeshRenderer>();
            if (_proceduralDefaultMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                _proceduralDefaultMaterial = new Material(shader) { color = new Color(0.82f, 0.82f, 0.82f) };
            }
            mr.sharedMaterial = _proceduralDefaultMaterial;

            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mf.sharedMesh;

            return go;
        }
    }
}
