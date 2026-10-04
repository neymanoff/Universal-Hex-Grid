namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Geometric targeting shapes available for skills, attacks, and area-of-effect calculations on a hex grid.
    /// </summary>
    public enum TargetShape
    {
        /// <summary>Single target cell within range.</summary>
        SingleCell = 0,

        /// <summary>Straight axis-aligned beam along one of 6 hex directions.</summary>
        Line = 1,

        /// <summary>Symmetrical 120-degree cone spreading outward from origin.</summary>
        Cone = 2,

        /// <summary>Hexagonal blast area/radius around target center.</summary>
        Area = 3,

        /// <summary>1-cell thick perimeter ring at exact distance N from center.</summary>
        Ring = 4
    }
}
