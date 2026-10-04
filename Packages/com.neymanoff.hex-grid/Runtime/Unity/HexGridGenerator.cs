using System;
using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Geometric shape of the procedural hex grid generation.
    /// </summary>
    public enum HexGridShape
    {
        /// <summary>
        /// Radial hexagon island centered at (0, 0) with a defined radius.
        /// </summary>
        HexagonIsland = 0,

        /// <summary>
        /// Rectangular grid defined by width (columns) and height (rows) in Odd-R coordinate space.
        /// </summary>
        Rectangle = 1,

        /// <summary>
        /// Parallelogram grid defined by axial width (q) and height (r).
        /// </summary>
        Parallelogram = 2
    }

    /// <summary>
    /// Inspector-driven procedural generator for hexagonal battlefields and exploration maps.
    /// Allows level designers to generate fields of arbitrary size, shape, and obstacle density
    /// with one click directly in the Inspector, without writing any code.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Grid Generator")]
    public class HexGridGenerator : MonoBehaviour
    {
        [Header("Grid Shape & Dimensions")]
        [Tooltip("Shape pattern to generate.")]
        [SerializeField] private HexGridShape _shape = HexGridShape.HexagonIsland;

        [Tooltip("Radius of the hexagon island (e.g. 3 = 37 cells, 4 = 61 cells, 5 = 91 cells).")]
        [Min(1)]
        [SerializeField] private int _radius = 3;

        [Tooltip("Width (number of columns) for Rectangular or Parallelogram layouts.")]
        [Min(1)]
        [SerializeField] private int _width = 10;

        [Tooltip("Height (number of rows) for Rectangular or Parallelogram layouts.")]
        [Min(1)]
        [SerializeField] private int _height = 8;

        [Header("Tile Assets")]
        [Tooltip("Ground tile painted on the Walkable Tilemap.")]
        [SerializeField] private TileBase _groundTile;

        [Tooltip("Obstacle tile painted on the Obstacle Tilemap.")]
        [SerializeField] private TileBase _obstacleTile;

        [Header("Obstacle Generation")]
        [Tooltip("Fraction of cells to randomly populate with obstacles (0 = no obstacles, 0.1 = 10% obstacles).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _obstacleDensity = 0.08f;

        [Tooltip("Random seed for reproducible obstacle distribution.")]
        [SerializeField] private int _randomSeed = 42;

        [Tooltip("If true, coordinate (0, 0) is guaranteed to stay free of obstacles (for unit spawn).")]
        [SerializeField] private bool _preserveOrigin = true;

        [Header("Target Tilemap References")]
        [Tooltip("Target Walkable Tilemap.")]
        [SerializeField] private Tilemap _walkableTilemap;

        [Tooltip("Target Obstacle Tilemap.")]
        [SerializeField] private Tilemap _obstacleTilemap;

        [Tooltip("Parent bridge component.")]
        [SerializeField] private HexTilemapBridge _bridge;

        public HexGridShape Shape { get => _shape; set => _shape = value; }
        public int Radius { get => _radius; set => _radius = Mathf.Max(1, value); }
        public int Width { get => _width; set => _width = Mathf.Max(1, value); }
        public int Height { get => _height; set => _height = Mathf.Max(1, value); }
        public TileBase GroundTile { get => _groundTile; set => _groundTile = value; }
        public TileBase ObstacleTile { get => _obstacleTile; set => _obstacleTile = value; }
        public float ObstacleDensity { get => _obstacleDensity; set => _obstacleDensity = Mathf.Clamp(value, 0f, 0.5f); }
        public int RandomSeed { get => _randomSeed; set => _randomSeed = value; }

        private void Reset()
        {
            ResolveReferences();
#if UNITY_EDITOR
            if (_groundTile == null)
                _groundTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Ground.asset");
            if (_obstacleTile == null)
                _obstacleTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Obstacle.asset");
#endif
        }

        /// <summary>
        /// Explicitly wires generator references without reflection.
        /// </summary>
        public void Configure(Tilemap walkableTilemap, Tilemap obstacleTilemap, TileBase groundTile, TileBase obstacleTile, HexTilemapBridge bridge = null)
        {
            _walkableTilemap = walkableTilemap;
            _obstacleTilemap = obstacleTilemap;
            _groundTile = groundTile;
            _obstacleTile = obstacleTile;
            if (bridge != null) _bridge = bridge;
        }

        /// <summary>
        /// Clears both Walkable and Obstacle Tilemaps.
        /// </summary>
        [ContextMenu("Clear Grid")]
        public void ClearGrid()
        {
            ResolveReferences();

            if (_walkableTilemap != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(_walkableTilemap, "Clear Walkable Grid");
#endif
                _walkableTilemap.ClearAllTiles();
            }

            if (_obstacleTilemap != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(_obstacleTilemap, "Clear Obstacle Grid");
#endif
                _obstacleTilemap.ClearAllTiles();
            }
        }

        /// <summary>
        /// Generates the hex grid layout according to current Inspector parameters.
        /// </summary>
        [ContextMenu("Generate Grid")]
        public void GenerateGrid()
        {
            ResolveReferences();

            if (_walkableTilemap == null || _groundTile == null)
            {
                Debug.LogWarning("[HexGridGenerator] Cannot generate grid: Walkable Tilemap or Ground Tile is not assigned.", this);
                return;
            }

            ClearGrid();

            var coords = GetCoordinatesForShape();
            var rng = new System.Random(_randomSeed);

            foreach (var hex in coords)
            {
                var cell = HexTilemapBridge.HexToTilemapCell(hex);

                // Place ground tile
                _walkableTilemap.SetTile(cell, _groundTile);

                // Check obstacle generation
                if (_obstacleTilemap != null && _obstacleTile != null && _obstacleDensity > 0f)
                {
                    bool isOrigin = _preserveOrigin && hex == HexCoord.Zero;
                    if (!isOrigin && rng.NextDouble() < _obstacleDensity)
                    {
                        _obstacleTilemap.SetTile(cell, _obstacleTile);
                    }
                }
            }

            _walkableTilemap.RefreshAllTiles();
            if (_obstacleTilemap != null)
            {
                _obstacleTilemap.RefreshAllTiles();
            }

            Debug.Log($"[HexGridGenerator] Successfully generated {_shape} grid with {coords.Count} cells.", this);
        }

        /// <summary>
        /// Calculates all hex coordinates included in the selected shape.
        /// </summary>
        public List<HexCoord> GetCoordinatesForShape()
        {
            var result = new List<HexCoord>();

            switch (_shape)
            {
                case HexGridShape.HexagonIsland:
                    for (int q = -_radius; q <= _radius; q++)
                    {
                        int rMin = Mathf.Max(-_radius, -q - _radius);
                        int rMax = Mathf.Min(_radius, -q + _radius);
                        for (int r = rMin; r <= rMax; r++)
                        {
                            result.Add(new HexCoord(q, r));
                        }
                    }
                    break;

                case HexGridShape.Rectangle:
                    for (int col = 0; col < _width; col++)
                    {
                        for (int row = 0; row < _height; row++)
                        {
                            result.Add(HexCoord.FromOddR(col, row));
                        }
                    }
                    break;

                case HexGridShape.Parallelogram:
                    for (int q = 0; q < _width; q++)
                    {
                        for (int r = 0; r < _height; r++)
                        {
                            result.Add(new HexCoord(q, r));
                        }
                    }
                    break;
            }

            return result;
        }

        private void ResolveReferences()
        {
            if (_bridge == null) _bridge = GetComponent<HexTilemapBridge>();
            if (_bridge != null)
            {
                if (_walkableTilemap == null) _walkableTilemap = _bridge.WalkableTilemap;
                if (_obstacleTilemap == null) _obstacleTilemap = _bridge.ObstacleTilemap;
            }

            if (_walkableTilemap == null || _obstacleTilemap == null)
            {
                var tilemaps = GetComponentsInChildren<Tilemap>();
                foreach (var tm in tilemaps)
                {
                    if (_walkableTilemap == null && tm.name.IndexOf("Walkable", StringComparison.OrdinalIgnoreCase) >= 0)
                        _walkableTilemap = tm;
                    else if (_obstacleTilemap == null && tm.name.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0)
                        _obstacleTilemap = tm;
                }
            }
        }
    }
}
