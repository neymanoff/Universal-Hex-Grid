using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Supported visual highlight rendering styles.
    /// </summary>
    public enum HighlightRenderMode
    {
        /// <summary>
        /// Solid filled tile polygon.
        /// </summary>
        Solid = 0,

        /// <summary>
        /// Outline contour ring with transparent interior, preserving 100% visibility of underlying map artwork.
        /// </summary>
        Outline = 1
    }

    /// <summary>
    /// Renders pathfinding, reachable zones, targeting shapes, and hover previews
    /// onto an overlay <see cref="Tilemap"/> using Inspector-assigned Tile assets and colors.
    /// Supports both solid fill and outline/contour framing modes with zero GameObject instantiation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Tilemap Highlight Overlay")]
    public class TilemapHighlightOverlay : MonoBehaviour
    {
        [Header("Overlay Tilemap")]
        [Tooltip("The Tilemap dedicated to rendering highlights.")]
        [SerializeField] private Tilemap _overlayTilemap;

        [Header("Render Mode")]
        [Tooltip("Selects whether cells are highlighted with solid fills or transparent-center outline rings (preserving underlying artwork).")]
        [SerializeField] private HighlightRenderMode _renderMode = HighlightRenderMode.Outline;

        [Header("Solid Highlight Tiles")]
        [Tooltip("Tile asset used for the reachable movement zone in Solid mode.")]
        [SerializeField] private TileBase _reachableZoneTile;

        [Tooltip("Tile asset used for path waypoints in Solid mode.")]
        [SerializeField] private TileBase _pathTile;

        [Tooltip("Tile asset used for cell hover highlight in Solid mode.")]
        [SerializeField] private TileBase _hoverTile;

        [Tooltip("Tile asset used for AOE targeting shapes in Solid mode.")]
        [SerializeField] private TileBase _targetTile;

        [Header("Outline Highlight Tiles (Preserves Background Artwork)")]
        [Tooltip("Default outline ring tile used as a fallback for all outline highlights.")]
        [SerializeField] private TileBase _defaultOutlineTile;

        [Tooltip("Optional custom outline tile for reachable movement zones.")]
        [SerializeField] private TileBase _outlineReachableTile;

        [Tooltip("Optional custom outline tile for path waypoints.")]
        [SerializeField] private TileBase _outlinePathTile;

        [Tooltip("Optional custom outline tile for cell hover highlight.")]
        [SerializeField] private TileBase _outlineHoverTile;

        [Tooltip("Optional custom outline tile for AOE targeting shapes.")]
        [SerializeField] private TileBase _outlineTargetTile;

        [Header("Tint Colors")]
        [SerializeField] private Color _reachableColor = new(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color _pathColor = new(1f, 0.9f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new(0.4f, 1f, 0.4f, 0.6f);
        [SerializeField] private Color _targetColor = new(1f, 0.2f, 0.2f, 0.7f);

        private readonly HashSet<Vector3Int> _activeHighlightedCells = new();

        public HighlightRenderMode RenderMode
        {
            get => _renderMode;
            set => _renderMode = value;
        }

        public Color ReachableColor { get => _reachableColor; set => _reachableColor = value; }
        public Color PathColor { get => _pathColor; set => _pathColor = value; }
        public Color HoverColor { get => _hoverColor; set => _hoverColor = value; }
        public Color TargetColor { get => _targetColor; set => _targetColor = value; }

        public TileBase ReachableTile => GetEffectiveTile(_reachableZoneTile, _outlineReachableTile);
        public TileBase PathTile => GetEffectiveTile(_pathTile, _outlinePathTile);
        public TileBase HoverTile => GetEffectiveTile(_hoverTile, _outlineHoverTile);
        public TileBase TargetTile => GetEffectiveTile(_targetTile, _outlineTargetTile);

        /// <summary>
        /// Explicitly wires overlay dependencies and tiles.
        /// </summary>
        public void Configure(Tilemap overlayTilemap, TileBase defaultHighlightTile = null, TileBase defaultOutlineTile = null)
        {
            _overlayTilemap = overlayTilemap;
            if (defaultHighlightTile != null)
            {
                if (_reachableZoneTile == null) _reachableZoneTile = defaultHighlightTile;
                if (_pathTile == null) _pathTile = defaultHighlightTile;
                if (_hoverTile == null) _hoverTile = defaultHighlightTile;
                if (_targetTile == null) _targetTile = defaultHighlightTile;
            }

            if (defaultOutlineTile != null)
            {
                _defaultOutlineTile = defaultOutlineTile;
                if (_outlineReachableTile == null) _outlineReachableTile = defaultOutlineTile;
                if (_outlinePathTile == null) _outlinePathTile = defaultOutlineTile;
                if (_outlineHoverTile == null) _outlineHoverTile = defaultOutlineTile;
                if (_outlineTargetTile == null) _outlineTargetTile = defaultOutlineTile;
            }
        }

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
            SetHighlightGroup(coords, ReachableTile, _reachableColor);
        }

        /// <summary>
        /// Displays the path waypoints on the overlay.
        /// </summary>
        public void ShowPath(IEnumerable<HexCoord> path)
        {
            SetHighlightGroup(path, PathTile, _pathColor);
        }

        /// <summary>
        /// Displays targeting area or AOE shape coordinates.
        /// </summary>
        public void ShowTargetShape(IEnumerable<HexCoord> coords)
        {
            SetHighlightGroup(coords, TargetTile, _targetColor);
        }

        /// <summary>
        /// Displays a single hover tile over the specified hex cell.
        /// </summary>
        public void ShowHover(HexCoord coord)
        {
            if (_overlayTilemap == null) return;
            var tile = HoverTile;
            if (tile == null) return;

            var cell = HexTilemapBridge.HexToTilemapCell(coord);
            _overlayTilemap.SetTile(cell, tile);
            _overlayTilemap.SetTileFlags(cell, TileFlags.None);
            _overlayTilemap.SetColor(cell, _hoverColor);
            _activeHighlightedCells.Add(cell);
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
                _overlayTilemap.SetTileFlags(cell, TileFlags.None);
                _overlayTilemap.SetColor(cell, color);
                _activeHighlightedCells.Add(cell);
            }
        }

        private TileBase GetEffectiveTile(TileBase solidTile, TileBase outlineTile)
        {
            if (_renderMode == HighlightRenderMode.Outline)
            {
                if (outlineTile != null) return outlineTile;
                if (_defaultOutlineTile != null) return _defaultOutlineTile;
                return solidTile;
            }

            return solidTile != null ? solidTile : (outlineTile != null ? outlineTile : _defaultOutlineTile);
        }
    }
}
