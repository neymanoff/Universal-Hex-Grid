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
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Tilemap Bridge")]
    public class HexTilemapBridge : MonoBehaviour, ITraversalRule
    {
        [Header("Grid & Tilemap References")]
        [Tooltip("The parent Grid component configuring cell layout (Hexagon Point Top).")]
        [SerializeField] private Grid _grid;

        [Tooltip("The Tilemap containing walkable terrain tiles.")]
        [SerializeField] private Tilemap _walkableTilemap;

        [Tooltip("Optional Tilemap containing obstacle/blocking tiles (water, rocks, walls).")]
        [SerializeField] private Tilemap _obstacleTilemap;

        [Header("Traversal Configuration")]
        [Tooltip("Default movement cost to enter a walkable cell.")]
        [SerializeField] private int _defaultMovementCost = 1;

        [Tooltip("If true, only cells present in the walkable Tilemap can be entered. If false, all cells except obstacles are walkable.")]
        [SerializeField] private bool _requireWalkableTile = true;

        public Grid Grid => _grid;
        public Tilemap WalkableTilemap => _walkableTilemap;
        public Tilemap ObstacleTilemap => _obstacleTilemap;

        private void Reset()
        {
            if (_grid == null) _grid = GetComponentInParent<Grid>();
            if (_walkableTilemap == null) _walkableTilemap = GetComponent<Tilemap>();
        }

        private void Awake()
        {
            if (_grid == null) _grid = GetComponentInParent<Grid>();
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
        /// Converts a <see cref="HexCoord"/> into the world-space center position of the hex cell.
        /// </summary>
        public Vector3 HexToWorld(HexCoord coord)
        {
            if (_grid == null) return Vector3.zero;
            var cell = HexToTilemapCell(coord);
            return _grid.GetCellCenterWorld(cell);
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
