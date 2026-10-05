using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Pure layout math service: converts discrete hex grid coordinates into UI pixel positions (in RectTransform pixels).
    /// Preserves Pointy-Top hex proportions and Odd-R row staggering without any scene or gameplay dependencies.
    /// </summary>
    public static class HexUiLayoutConverter
    {
        /// <summary>
        /// Result of a calculated UI layout containing positioned slots and layout bounds.
        /// </summary>
        public sealed class HexUiLayout
        {
            public readonly List<HexUiSlot> Slots;
            public readonly float CellHeightPx;
            public readonly float CellWidthPx;
            public readonly Vector2 CenterOffsetAppliedPx;
            public readonly Vector2 TotalExtentsPx;

            public HexUiLayout(List<HexUiSlot> slots, float cellHeightPx, float cellWidthPx, Vector2 centerOffsetPx, Vector2 totalExtentsPx)
            {
                Slots = slots;
                CellHeightPx = cellHeightPx;
                CellWidthPx = cellWidthPx;
                CenterOffsetAppliedPx = centerOffsetPx;
                TotalExtentsPx = totalExtentsPx;
            }
        }

        /// <summary>
        /// A single UI slot pairing a grid coordinate with its pixel anchored position.
        /// </summary>
        public readonly struct HexUiSlot
        {
            public readonly HexCoord Coord;
            public readonly int SlotIndex;
            public readonly Vector2 AnchoredPositionPx;

            public HexUiSlot(HexCoord coord, int slotIndex, Vector2 anchoredPositionPx)
            {
                Coord = coord;
                SlotIndex = slotIndex;
                AnchoredPositionPx = anchoredPositionPx;
            }
        }

        /// <summary>
        /// Builds a UI layout from a list of axial <see cref="HexCoord"/> coordinates.
        /// </summary>
        public static HexUiLayout Build(
            IReadOnlyList<HexCoord> coords,
            float cellHeightPx,
            float cellGapPx = 0f,
            bool center = true,
            bool mirrorX = false,
            bool mirrorY = false)
        {
            float cellWidthPx = cellHeightPx * HexTilemapBridge.PointyTopAspectRatio;
            var slots = new List<HexUiSlot>(coords?.Count ?? 0);

            if (coords == null || coords.Count == 0)
            {
                return new HexUiLayout(slots, cellHeightPx, cellWidthPx, Vector2.zero, Vector2.zero);
            }

            float stepX = cellWidthPx + Mathf.Max(0f, cellGapPx);
            float stepY = 0.75f * cellHeightPx + Mathf.Max(0f, cellGapPx) * 0.5f;

            bool hasAabb = false;
            Vector2 minPx = Vector2.zero;
            Vector2 maxPx = Vector2.zero;

            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                var (col, row) = c.ToOddR();
                float offset = (row & 1) != 0 ? 0.5f : 0f;

                var posPx = new Vector2((col + offset) * stepX, row * stepY);
                if (mirrorX) posPx.x = -posPx.x;
                if (mirrorY) posPx.y = -posPx.y;

                slots.Add(new HexUiSlot(c, i, posPx));

                if (!hasAabb)
                {
                    minPx = maxPx = posPx;
                    hasAabb = true;
                }
                else
                {
                    if (posPx.x < minPx.x) minPx.x = posPx.x;
                    if (posPx.y < minPx.y) minPx.y = posPx.y;
                    if (posPx.x > maxPx.x) maxPx.x = posPx.x;
                    if (posPx.y > maxPx.y) maxPx.y = posPx.y;
                }
            }

            Vector2 offsetAppliedPx = Vector2.zero;
            if (center && hasAabb)
            {
                offsetAppliedPx = -(minPx + maxPx) * 0.5f;
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    slots[i] = new HexUiSlot(s.Coord, s.SlotIndex, s.AnchoredPositionPx + offsetAppliedPx);
                }
            }

            Vector2 totalExtentsPx = hasAabb
                ? (maxPx - minPx) + new Vector2(cellWidthPx, cellHeightPx)
                : Vector2.zero;

            return new HexUiLayout(slots, cellHeightPx, cellWidthPx, offsetAppliedPx, totalExtentsPx);
        }

        /// <summary>
        /// Builds a UI layout directly from a <see cref="FormationPatternSO"/> asset.
        /// </summary>
        public static HexUiLayout Build(
            FormationPatternSO pattern,
            float cellHeightPx,
            float cellGapPx = 0f,
            bool center = true,
            bool mirrorX = false,
            bool mirrorY = false)
        {
            if (pattern == null)
            {
                return Build(System.Array.Empty<HexCoord>(), cellHeightPx, cellGapPx, center, mirrorX, mirrorY);
            }
            return Build(pattern.GetRelativeSlots(), cellHeightPx, cellGapPx, center, mirrorX, mirrorY);
        }

        /// <summary>
        /// Builds a UI layout from sampled Tilemap tiles.
        /// </summary>
        public static HexUiLayout Build(
            IReadOnlyList<HexTilemapSampler.SampledTile> sampledTiles,
            float cellHeightPx,
            float cellGapPx = 0f,
            bool center = true,
            bool mirrorX = false,
            bool mirrorY = false)
        {
            if (sampledTiles == null || sampledTiles.Count == 0)
            {
                return Build(System.Array.Empty<HexCoord>(), cellHeightPx, cellGapPx, center, mirrorX, mirrorY);
            }

            var coords = new List<HexCoord>(sampledTiles.Count);
            for (int i = 0; i < sampledTiles.Count; i++)
            {
                coords.Add(sampledTiles[i].Coord);
            }

            return Build(coords, cellHeightPx, cellGapPx, center, mirrorX, mirrorY);
        }
    }
}
