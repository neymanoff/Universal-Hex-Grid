namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Represents the six primary neighbor directions on a pointy-top hexagonal grid.
    /// Indexed clockwise starting from East (0) to NorthEast (5).
    /// </summary>
    public enum HexDirection
    {
        /// <summary>Direction (+1, 0) — Right / East.</summary>
        East = 0,

        /// <summary>Direction (+1, -1) — Down-Right / South-East.</summary>
        SouthEast = 1,

        /// <summary>Direction (0, -1) — Down-Left / South-West.</summary>
        SouthWest = 2,

        /// <summary>Direction (-1, 0) — Left / West.</summary>
        West = 3,

        /// <summary>Direction (-1, +1) — Up-Left / North-West.</summary>
        NorthWest = 4,

        /// <summary>Direction (0, +1) — Up-Right / North-East.</summary>
        NorthEast = 5
    }

    /// <summary>
    /// Extension methods and helpers for <see cref="HexDirection"/>.
    /// </summary>
    public static class HexDirectionExtensions
    {
        /// <summary>
        /// Returns the opposite direction (180 degree rotation).
        /// </summary>
        public static HexDirection Opposite(this HexDirection direction)
        {
            return (HexDirection)(((int)direction + 3) % 6);
        }

        /// <summary>
        /// Rotates the direction clockwise by the given number of 60-degree steps.
        /// </summary>
        public static HexDirection RotateClockwise(this HexDirection direction, int steps = 1)
        {
            return (HexDirection)(((int)direction + (steps % 6) + 6) % 6);
        }

        /// <summary>
        /// Rotates the direction counter-clockwise by the given number of 60-degree steps.
        /// </summary>
        public static HexDirection RotateCounterClockwise(this HexDirection direction, int steps = 1)
        {
            return (HexDirection)(((int)direction - (steps % 6) + 6) % 6);
        }
    }
}
