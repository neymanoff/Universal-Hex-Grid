using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Lightweight data describing a resolved spawn slot on the hexagonal grid.
    /// </summary>
    public readonly struct HexSpawnSlot
    {
        public readonly HexCoord Coord;
        public readonly Vector3 WorldPosition;
        public readonly Quaternion Rotation;
        public readonly int SlotIndex;
        public readonly CellOwner Faction;

        public HexSpawnSlot(HexCoord coord, Vector3 worldPosition, Quaternion rotation, int slotIndex, CellOwner faction)
        {
            Coord = coord;
            WorldPosition = worldPosition;
            Rotation = rotation;
            SlotIndex = slotIndex;
            Faction = faction;
        }
    }
}
