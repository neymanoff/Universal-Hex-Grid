namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Contract defining cell passability and movement cost for pathfinding and flood-fill operations.
    /// Pure C# interface with zero engine dependencies.
    /// </summary>
    public interface ITraversalRule
    {
        /// <summary>
        /// Checks whether the specified cell coordinate is structurally passable and can be entered.
        /// </summary>
        /// <param name="coord">The target coordinate.</param>
        /// <param name="traveler">Optional reference to the traversing entity (used for team-aware or size-aware passability).</param>
        /// <returns>True if the traveler can step onto this cell, false otherwise.</returns>
        bool CanEnter(HexCoord coord, object traveler = null);

        /// <summary>
        /// Returns the movement cost to transition from <paramref name="from"/> to <paramref name="to"/>.
        /// Must return an integer >= 1 to maintain admissibility for A* heuristic search.
        /// </summary>
        /// <param name="from">The origin coordinate.</param>
        /// <param name="to">The destination coordinate.</param>
        /// <param name="traveler">Optional reference to the traversing entity.</param>
        /// <returns>Movement cost (integer >= 1).</returns>
        int GetMovementCost(HexCoord from, HexCoord to, object traveler = null);
    }
}
