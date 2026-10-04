using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Renders pathfinding, reachable zones, targeting shapes, and hover previews
    /// onto an overlay <see cref="Tilemap"/> using Inspector-assigned Tile assets or colors.
    /// Provides batch painting with zero GameObject instantiation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Tilemap Highlight Overlay")]
    public class TilemapHighlightOverlay : MonoBehaviour
    {
        [Header("Overlay Tilemap")]
        [Tooltip("The Tilemap dedicated to rendering highlights.")]
        [SerializeField] private Tilemap _overlayTilemap;

        [Header("Highlight Tiles (Inspector-Authored)")]
        [Tooltip("Tile asset used for the reachable movement zone.")]
        [SerializeField] private TileBase _reachableZoneTile;

        [Tooltip("Tile asset used for path waypoints.")]
        [SerializeField] private TileBase _pathTile;

        [Tooltip("Tile asset used for cell hover highlight.")]
        [SerializeField] private TileBase _hoverTile;

        [Tooltip("Tile asset used for AOE targeting shapes.")]
        [SerializeField] private TileBase _targetTile;

        [Header("Tint Colors")]
        [SerializeField] private Color _reachableColor = new(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color _pathColor = new(1f, 0.9f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new(0.4f, 1f, 0.4f, 0.6f);
        [SerializeField] private Color _targetColor = new(1f, 0.2f, 0.2f, 0.7f);

        private readonly HashSet<Vector3Int> _activeHighlightedCells = new();

        private void Reset()
        {
            if (_overlayTilemap == null) _overlayTilemap = GetComponent<Tilemap>();
        }

        /// <summary>
        /// Clears all highlight tiles from the overlay tilemap.
        /// </summary>
        public void ClearAll()
        {
            if (_overlayTilemap == null) return;
            _overlayTilemap.ClearAllTiles();
            _activeHighlightedCells.Clear();
        }

        /// <summary>
        /// Displays the reachable movement zone for the given coordinates.
        /// </summary>
        public void ShowReachableZone(IEnumerable<HexCoord> coords)
        {
            SetHighlightGroup(coords, _reachableZoneTile, _reachableColor);
        }

        /// <summary>
        /// Displays the path waypoints on the overlay.
        /// </summary>
        public void ShowPath(IEnumerable<HexCoord> path)
        {
            SetHighlightGroup(path, _pathTile, _pathColor);
        }

        /// <summary>
        /// Displays targeting area or AOE shape coordinates.
        /// </summary>
        public void ShowTargetShape(IEnumerable<HexCoord> coords)
        {
            SetHighlightGroup(coords, _targetTile, _targetColor);
        }

        /// <summary>
        /// Displays a single hover tile over the specified hex cell.
        /// </summary>
        public void ShowHover(HexCoord coord)
        {
            if (_overlayTilemap == null) return;
            var cell = HexTilemapBridge.HexToTilemapCell(coord);
            if (_hoverTile != null)
            {
                _overlayTilemap.SetTile(cell, _hoverTile);
                _overlayTilemap.SetColor(cell, _hoverColor);
                _activeHighlightedCells.Add(cell);
            }
        }

        /// <summary>
        /// Clears a specific cell from the overlay tilemap.
        /// </summary>
        public void ClearCell(HexCoord coord)
        {
            if (_overlayTilemap == null) return;
            var cell = HexTilemapBridge.HexToTilemapCell(coord);
            _overlayTilemap.SetTile(cell, null);
            _activeHighlightedCells.Remove(cell);
        }

        private void SetHighlightGroup(IEnumerable<HexCoord> coords, TileBase tile, Color color)
        {
            if (_overlayTilemap == null || tile == null || coords == null) return;

            foreach (var coord in coords)
            {
                var cell = HexTilemapBridge.HexToTilemapCell(coord);
                _overlayTilemap.SetTile(cell, tile);
                _overlayTilemap.SetColor(cell, color);
                _activeHighlightedCells.Add(cell);
            }
        }
    }
}
