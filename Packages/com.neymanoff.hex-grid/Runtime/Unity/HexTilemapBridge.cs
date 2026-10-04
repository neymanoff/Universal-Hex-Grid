using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Bridges Unity's native Hexagonal <see cref="Grid"/> and <see cref="Tilemap"/> components with
    /// the pure C# <see cref="HexCoord"/> domain.
    /// Implements <see cref="ITraversalRule"/> to query passability and movement costs directly from painted Tilemaps.
    /// Exposes reactive Inspector controls for cell dimensions (Cell Scale) and spacing (Cell Spacing) matching Legends: Legacy of the Lost.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Tilemap Bridge")]
    public class HexTilemapBridge : MonoBehaviour, ITraversalRule
    {
        /// <summary>
        /// Exact mathematical aspect ratio for Pointy-Top hexagons (sqrt(3)/2 ~ 0.8659766).
        /// </summary>
        public const float PointyTopAspectRatio = 0.8659766f;

        [Header("Grid Geometry Controls (Inspector)")]
        [Tooltip("Visual size multiplier of the hexagon cells (controls cell scale via Grid transform scale).")]
        [SerializeField] private float _cellScale = 1.0f;

        [Tooltip("Distance multiplier between hexagon cell centers (controls cell spacing and gaps via Grid cellSize. 1.0 = edge-to-edge touch).")]
        [SerializeField] private float _cellSpacing = 1.0f;

        [Header("Grid & Tilemap References")]
        [Tooltip("The parent Grid component configuring cell layout (Hexagon Point Top).")]
        [SerializeField] private Grid _grid;

        [Tooltip("The Tilemap containing walkable terrain tiles.")]
        [SerializeField] private Tilemap _walkableTilemap;

        [Tooltip("Optional Tilemap containing obstacle/blocking tiles (water, rocks, walls).")]
        [SerializeField] private Tilemap _obstacleTilemap;

        [Tooltip("The overlay component handling visual highlights on child Highlight Overlay object.")]
        [SerializeField] private TilemapHighlightOverlay _highlightOverlay;

        [Header("Traversal Configuration")]
        [Tooltip("Default movement cost to enter a walkable cell.")]
        [SerializeField] private int _defaultMovementCost = 1;

        [Tooltip("If true, only cells present in the walkable Tilemap can be entered. If false, all cells except obstacles are walkable.")]
        [SerializeField] private bool _requireWalkableTile = true;

        [Header("Unit Alignment Settings")]
        [Tooltip("Vertical height offset added to cell center for units standing on the ground.")]
        [SerializeField] private float _unitHeightOffset = 0.3f;

        public Grid Grid => _grid;
        public Tilemap WalkableTilemap => _walkableTilemap;
        public Tilemap ObstacleTilemap => _obstacleTilemap;
        public TilemapHighlightOverlay HighlightOverlay => _highlightOverlay;

        public float CellScale
        {
            get => _cellScale;
            set
            {
                _cellScale = Mathf.Max(0.01f, value);
                ApplyGridDimensions();
            }
        }

        public float CellSpacing
        {
            get => _cellSpacing;
            set
            {
                _cellSpacing = Mathf.Max(0.01f, value);
                ApplyGridDimensions();
            }
        }

        public float UnitHeightOffset
        {
            get => _unitHeightOffset;
            set => _unitHeightOffset = value;
        }

        /// <summary>
        /// Dynamically updates the parent <see cref="Grid.transform.localScale"/> (visual cell size)
        /// and <see cref="Grid.cellSize"/> (distance between cell centers) maintaining pointy-top proportions.
        /// </summary>
        public void ApplyGridDimensions()
        {
            if (_grid == null) return;
            var targetScale = new Vector3(_cellScale, _cellScale, 1f);
            if (_grid.transform.localScale != targetScale)
            {
                _grid.transform.localScale = targetScale;
            }

            var targetCellSize = new Vector3(PointyTopAspectRatio * _cellSpacing, 1f * _cellSpacing, 1f);
            if (_grid.cellSize != targetCellSize)
            {
                _grid.cellSize = targetCellSize;
            }
        }

        /// <summary>
        /// Explicitly wires the bridge dependencies without requiring reflection.
        /// </summary>
        public void Configure(Grid grid, Tilemap walkableTilemap, Tilemap obstacleTilemap, TilemapHighlightOverlay highlightOverlay = null)
        {
            _grid = grid;
            _walkableTilemap = walkableTilemap;
            _obstacleTilemap = obstacleTilemap;
            if (highlightOverlay != null) _highlightOverlay = highlightOverlay;
            ApplyGridDimensions();
        }

        private void Reset()
        {
            if (_grid == null) _grid = GetComponentInParent<Grid>();
            if (_walkableTilemap == null) _walkableTilemap = GetComponentInChildren<Tilemap>();
            if (_highlightOverlay == null) _highlightOverlay = GetComponentInChildren<TilemapHighlightOverlay>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && _grid != null)
                        ApplyGridDimensions();
                };
            }
#endif
        }

#if UNITY_EDITOR
        private bool _isUpdatePending;

        private void OnValidate()
        {
            if (_grid == null) _grid = GetComponentInParent<Grid>();
            if (_walkableTilemap == null) _walkableTilemap = GetComponentInChildren<Tilemap>();
            if (_highlightOverlay == null) _highlightOverlay = GetComponentInChildren<TilemapHighlightOverlay>();
            if (_cellScale < 0.01f) _cellScale = 1.0f;
            if (_cellSpacing < 0.01f) _cellSpacing = 1.0f;

            if (!_isUpdatePending)
            {
                _isUpdatePending = true;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    _isUpdatePending = false;
                    if (this != null && _grid != null)
                    {
                        ApplyGridDimensions();
                    }
                };
            }
        }
#endif

        private void Awake()
        {
            if (_grid == null) _grid = GetComponentInParent<Grid>();
            if (_highlightOverlay == null) _highlightOverlay = GetComponentInChildren<TilemapHighlightOverlay>();
        }

        private void Start()
        {
            ApplyGridDimensions();
        }

        /// <summary>
        /// Converts an axial <see cref="HexCoord"/> into Unity's Odd-R <see cref="Vector3Int"/> tilemap cell coordinate.
        /// </summary>
        public static Vector3Int HexToTilemapCell(HexCoord coord)
        {
            var (col, row) = coord.ToOddR();
            return new Vector3Int(col, row, 0);
        }

        /// <summary>
        /// Converts Unity's Odd-R <see cref="Vector3Int"/> tilemap cell coordinate into an axial <see cref="HexCoord"/>.
        /// </summary>
        public static HexCoord TilemapCellToHex(Vector3Int cell)
        {
            return HexCoord.FromOddR(cell.x, cell.y);
        }

        /// <summary>
        /// Converts a world-space position to the corresponding <see cref="HexCoord"/>.
        /// </summary>
        public HexCoord WorldToHex(Vector3 worldPosition)
        {
            if (_grid == null) return HexCoord.Zero;
            var cell = _grid.WorldToCell(worldPosition);
            return TilemapCellToHex(cell);
        }

        /// <summary>
        /// Converts a <see cref="HexCoord"/> into the world-space center position on the cell surface.
        /// </summary>
        public Vector3 HexToWorld(HexCoord coord)
        {
            if (_grid == null) return Vector3.zero;
            var cell = HexToTilemapCell(coord);
            return _grid.GetCellCenterWorld(cell);
        }

        /// <summary>
        /// Converts a <see cref="HexCoord"/> into the world-space position elevated by the specified unit height offset.
        /// </summary>
        public Vector3 HexToUnitWorld(HexCoord coord, float customHeightOffset = -1f)
        {
            Vector3 center = HexToWorld(coord);
            float offset = customHeightOffset >= 0f ? customHeightOffset : _unitHeightOffset;
            return center + Vector3.up * offset;
        }

        /// <summary>
        /// Checks if a cell is passable based on painted tilemap layers.
        /// Implements <see cref="ITraversalRule.CanEnter"/>.
        /// </summary>
        public bool CanEnter(HexCoord coord, object traveler = null)
        {
            var cell = HexToTilemapCell(coord);

            // Obstacle check: if obstacle tilemap has a tile at this cell, impassable
            if (_obstacleTilemap != null && _obstacleTilemap.HasTile(cell))
                return false;

            // Walkable check: if required, must have a tile on the walkable tilemap
            if (_requireWalkableTile)
            {
                if (_walkableTilemap == null || !_walkableTilemap.HasTile(cell))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the movement cost to enter the target cell.
        /// Implements <see cref="ITraversalRule.GetMovementCost"/>.
        /// </summary>
        public int GetMovementCost(HexCoord from, HexCoord to, object traveler = null)
        {
            return _defaultMovementCost;
        }

        /// <summary>
        /// Returns all valid walkable coordinates currently painted on the walkable Tilemap.
        /// </summary>
        public List<HexCoord> GetAllWalkableCoordinates()
        {
            var result = new List<HexCoord>();
            if (_walkableTilemap == null) return result;

            var bounds = _walkableTilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (_walkableTilemap.HasTile(cell))
                    {
                        var hex = TilemapCellToHex(cell);
                        if (CanEnter(hex))
                            result.Add(hex);
                    }
                }
            }
            return result;
        }
    }
}
