using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Helper service that samples occupied hex cells from Unity Tilemaps.
    /// Preserves Pointy-Top Odd-R staggering and returns sorted canonical results.
    /// </summary>
    public static class HexTilemapSampler
    {
        /// <summary>
        /// Data transfer object holding sampled tile information.
        /// </summary>
        public readonly struct SampledTile
        {
            public readonly HexCoord Coord;
            public readonly Vector3Int GridPos;
            public readonly Color Color;
            public readonly TileBase Tile;

            public SampledTile(HexCoord coord, Vector3Int gridPos, Color color, TileBase tile)
            {
                Coord = coord;
                GridPos = gridPos;
                Color = color;
                Tile = tile;
            }
        }

        /// <summary>
        /// Samples all non-empty tiles from the given Tilemap.
        /// </summary>
        public static List<SampledTile> Sample(Tilemap tilemap, float alphaCull = 0.05f)
        {
            var result = new List<SampledTile>();
            if (tilemap == null) return result;

            var bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y <= bounds.yMax; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(p)) continue;

                    var color = tilemap.GetColor(p);
                    if (color.a <= alphaCull) continue;

                    var coord = HexTilemapBridge.TilemapCellToHex(p);
                    var tile = tilemap.GetTile(p);
                    result.Add(new SampledTile(coord, p, color, tile));
                }
            }

            return result;
        }

        /// <summary>
        /// Samples non-empty tiles and sorts them in canonical order (top-to-bottom, left-to-right).
        /// </summary>
        public static List<SampledTile> SampleCanonical(Tilemap tilemap, float alphaCull = 0.05f)
        {
            var cells = Sample(tilemap, alphaCull);
            cells.Sort((a, b) =>
            {
                int ry = b.GridPos.y.CompareTo(a.GridPos.y); // top -> bottom
                if (ry != 0) return ry;
                return a.GridPos.x.CompareTo(b.GridPos.x);   // left -> right
            });
            return cells;
        }
    }
}
