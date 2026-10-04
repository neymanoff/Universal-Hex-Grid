using System;
using System.Collections.Generic;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Pure C# Dijkstra flood-fill algorithm for calculating reachable movement zones on a hex grid.
    /// Computes movement range boundaries and cost maps without engine dependencies.
    /// </summary>
    public static class HexFloodFill
    {
        /// <summary>
        /// Computes all coordinates reachable from <paramref name="origin"/> within the specified <paramref name="movementBudget"/>.
        /// </summary>
        /// <param name="origin">The center/starting coordinate.</param>
        /// <param name="movementBudget">Maximum movement points available (must be >= 0).</param>
        /// <param name="rule">Traversal contract defining passability and movement costs.</param>
        /// <param name="traveler">Optional entity reference passed to the traversal rule.</param>
        /// <returns>
        /// A dictionary mapping each reachable <see cref="HexCoord"/> to the minimum movement cost required to enter it.
        /// Includes the origin with cost 0.
        /// </returns>
        public static Dictionary<HexCoord, int> GetReachableZone(
            HexCoord origin,
            int movementBudget,
            ITraversalRule rule,
            object traveler = null)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (movementBudget < 0) return new Dictionary<HexCoord, int>();

            var reachable = new Dictionary<HexCoord, int>();
            if (!rule.CanEnter(origin, traveler))
                return reachable;

            reachable[origin] = 0;
            if (movementBudget == 0)
                return reachable;

            var frontier = new MinBinaryHeap<HexCoord, int>();
            frontier.Enqueue(origin, 0);

            var neighborBuffer = new HexCoord[6];

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int currentCost = reachable[current];

                current.GetNeighbors(neighborBuffer);
                for (int i = 0; i < 6; i++)
                {
                    var neighbor = neighborBuffer[i];
                    if (!rule.CanEnter(neighbor, traveler))
                        continue;

                    int stepCost = rule.GetMovementCost(current, neighbor, traveler);
                    if (stepCost < 1) stepCost = 1;

                    int newCost = currentCost + stepCost;
                    if (newCost > movementBudget)
                        continue;

                    if (!reachable.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                    {
                        reachable[neighbor] = newCost;
                        frontier.Enqueue(neighbor, newCost);
                    }
                }
            }

            return reachable;
        }
    }
}
