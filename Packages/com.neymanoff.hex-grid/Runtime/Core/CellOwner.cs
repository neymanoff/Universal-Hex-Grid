namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Faction or ownership type for grid cells, spawn points, and grid occupants.
    /// Fully compatible with Legends: Legacy of the Lost, extended with multi-team ally support.
    /// </summary>
    public enum CellOwner
    {
        /// <summary>Unowned or neutral cell / obstacle.</summary>
        Neutral = 0,

        /// <summary>Player hero, player squad, or player deployment zone.</summary>
        Player = 1,

        /// <summary>Enemy unit, squad, or hostile spawn zone.</summary>
        Enemy = 2,

        /// <summary>Allied unit or squad holding defensive positions.</summary>
        Ally = 3
    }
}
