using System;
using System.Collections.Generic;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Pure C# domain registry tracking spatial entity occupancy across hex coordinates.
    /// Maintains bidirectional O(1) mappings between occupants and cells without any engine dependencies.
    /// </summary>
    /// <typeparam name="TOccupant">Reference type of the entity that can occupy a cell.</typeparam>
    public class HexOccupancyMap<TOccupant> where TOccupant : class
    {
        private readonly Dictionary<HexCoord, TOccupant> coordToOccupant = new();
        private readonly Dictionary<TOccupant, HexCoord> occupantToCoord = new();

        /// <summary>
        /// Total number of occupied coordinates.
        /// </summary>
        public int Count => coordToOccupant.Count;

        /// <summary>
        /// Checks whether the specified coordinate is currently occupied.
        /// </summary>
        public bool IsOccupied(HexCoord coord) => coordToOccupant.ContainsKey(coord);

        /// <summary>
        /// Tries to get the occupant sitting at the specified coordinate.
        /// </summary>
        public bool TryGetOccupant(HexCoord coord, out TOccupant occupant)
        {
            return coordToOccupant.TryGetValue(coord, out occupant);
        }

        /// <summary>
        /// Tries to get the coordinate currently occupied by the specified occupant.
        /// </summary>
        public bool TryGetCoordinate(TOccupant occupant, out HexCoord coord)
        {
            if (occupant == null)
            {
                coord = default;
                return false;
            }
            return occupantToCoord.TryGetValue(occupant, out coord);
        }

        /// <summary>
        /// Attempts to occupy the given coordinate with the specified occupant.
        /// If the coordinate is already occupied by another occupant, returns false.
        /// If the occupant is already at another coordinate, it is vacated from the old coordinate first.
        /// </summary>
        public bool TryOccupy(HexCoord coord, TOccupant occupant)
        {
            if (occupant == null) throw new ArgumentNullException(nameof(occupant));

            if (coordToOccupant.TryGetValue(coord, out var currentOccupant))
            {
                if (ReferenceEquals(currentOccupant, occupant))
                    return true; // Already occupying this cell

                return false; // Cell occupied by someone else
            }

            // If occupant was somewhere else, vacate old position
            if (occupantToCoord.TryGetValue(occupant, out var oldCoord))
            {
                coordToOccupant.Remove(oldCoord);
            }

            coordToOccupant[coord] = occupant;
            occupantToCoord[occupant] = coord;
            return true;
        }

        /// <summary>
        /// Vacates the coordinate occupied by the specified occupant.
        /// </summary>
        public bool Vacate(TOccupant occupant)
        {
            if (occupant == null) return false;
            if (occupantToCoord.TryGetValue(occupant, out var coord))
            {
                coordToOccupant.Remove(coord);
                occupantToCoord.Remove(occupant);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Vacates the specified coordinate if occupied.
        /// </summary>
        public bool Vacate(HexCoord coord)
        {
            if (coordToOccupant.TryGetValue(coord, out var occupant))
            {
                coordToOccupant.Remove(coord);
                occupantToCoord.Remove(occupant);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all occupancies.
        /// </summary>
        public void Clear()
        {
            coordToOccupant.Clear();
            occupantToCoord.Clear();
        }

        /// <summary>
        /// Returns all currently occupied coordinates.
        /// </summary>
        public IEnumerable<HexCoord> GetAllOccupiedCoordinates() => coordToOccupant.Keys;

        /// <summary>
        /// Returns all current occupants.
        /// </summary>
        public IEnumerable<TOccupant> GetAllOccupants() => occupantToCoord.Keys;
    }
}
