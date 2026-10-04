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
    }

    /// <summary>
    /// Spawns 3D physical hex meshes and prefabs driven by a 2D authoring Tilemap layout.
    /// Enables seamless conversion from 2D Tilemap designer painting to full 3D tactical battlegrounds or exploration maps.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Grid 3D Spawner")]
    public class HexGrid3DSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Bridge component providing coordinate conversion and layout dimensions.")]
        [SerializeField] private HexTilemapBridge _bridge;

        [Tooltip("Source Tilemap to read cells from. If unassigned, defaults to bridge's WalkableTilemap.")]
        [SerializeField] private Tilemap _sourceTilemap;

        [Tooltip("Transform container under which spawned 3D hexes are parented.")]
        [SerializeField] private Transform _spawnContainer;

        [Header("Prefabs & Mappings")]
        [Tooltip("Default 3D prefab used for cells without a specific tile mapping.")]
        [SerializeField] private GameObject _defaultHexPrefab;

        [Tooltip("Specific tile-to-prefab mappings for varied biomes (e.g. grass, water, mountain).")]
        [SerializeField] private List<TilePrefabMapping> _tileMappings = new();

        [Header("Spawn Options")]
        [Tooltip("If true, automatically spawns the 3D grid in Start().")]
        [SerializeField] private bool _autoSpawnOnStart = false;

        [Tooltip("Local scale applied to each spawned 3D hex instance.")]
        [SerializeField] private Vector3 _instanceScale = Vector3.one;

        [Tooltip("Euler rotation offset applied to each spawned 3D hex instance.")]
        [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

        [Tooltip("Vertical height offset applied to the spawned 3D hex position.")]
        [SerializeField] private float _verticalOffset = 0f;

        private readonly List<GameObject> _spawnedInstances = new();

        public IReadOnlyList<GameObject> SpawnedInstances => _spawnedInstances;

        private void Reset()
        {
            if (_bridge == null) _bridge = GetComponentInParent<HexTilemapBridge>();
            if (_bridge != null && _sourceTilemap == null) _sourceTilemap = _bridge.WalkableTilemap;
            if (_spawnContainer == null) _spawnContainer = transform;
        }

        private void Start()
        {
            if (_autoSpawnOnStart)
            {
                SpawnGrid();
            }
        }

        /// <summary>
        /// Instantiates 3D physical hex prefabs for every tile in the source Tilemap.
        /// </summary>
        [ContextMenu("Spawn 3D Grid")]
        public void SpawnGrid()
        {
            ClearGrid();

            if (_bridge == null) _bridge = GetComponentInParent<HexTilemapBridge>();
            var tilemap = _sourceTilemap != null ? _sourceTilemap : (_bridge != null ? _bridge.WalkableTilemap : null);
            if (tilemap == null)
            {
                Debug.LogWarning("[HexGrid3DSpawner] No source Tilemap available to spawn 3D hexes.", this);
                return;
            }

            var container = _spawnContainer != null ? _spawnContainer : transform;
            var bounds = tilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var tile = tilemap.GetTile(cell);
                    if (tile == null) continue;

                    var prefab = ResolvePrefabForTile(tile);
                    if (prefab == null) continue;

                    var worldPos = _bridge != null
                        ? _bridge.HexToWorld(HexTilemapBridge.TilemapCellToHex(cell))
                        : tilemap.GetCellCenterWorld(cell);

                    worldPos += Vector3.up * _verticalOffset;

                    var rot = Quaternion.Euler(_rotationOffset);
                    var instance = Instantiate(prefab, worldPos, rot, container);
                    instance.transform.localScale = _instanceScale;
                    instance.name = $"Hex3D_{cell.x}_{cell.y}_{tile.name}";

                    _spawnedInstances.Add(instance);
                }
            }
        }

        /// <summary>
        /// Clears all previously spawned 3D hex instances.
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

            // Also clean up any lingering child transforms if container was set
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
        }

        private GameObject ResolvePrefabForTile(TileBase tile)
        {
            if (tile != null && _tileMappings != null)
            {
                for (int i = 0; i < _tileMappings.Count; i++)
                {
                    if (_tileMappings[i].Tile == tile && _tileMappings[i].Prefab != null)
                    {
                        return _tileMappings[i].Prefab;
                    }
                }
            }
            return _defaultHexPrefab;
        }
    }
}
