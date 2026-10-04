using System;
using System.Collections.Generic;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Pure C# A* pathfinding engine for pointy-top hexagonal grids.
    /// Supports weighted movement costs, dynamic passability rules, approach-mode for blocked targets,
    /// and deterministic tie-breaking.
    /// </summary>
    public static class HexPathfinder
    {
        private readonly struct PathPriority : IComparable<PathPriority>
        {
            public readonly int F;
            public readonly int H;
            public readonly int Q;
            public readonly int R;

            public PathPriority(int f, int h, int q, int r)
            {
                F = f;
                H = h;
                Q = q;
                R = r;
            }

            public int CompareTo(PathPriority other)
            {
                int cmp = F.CompareTo(other.F);
                if (cmp != 0) return cmp;
                cmp = H.CompareTo(other.H);
                if (cmp != 0) return cmp;
                cmp = Q.CompareTo(other.Q);
                if (cmp != 0) return cmp;
                return R.CompareTo(other.R);
            }
        }

        /// <summary>
        /// Finds the shortest path from <paramref name="start"/> to <paramref name="goal"/>.
        /// </summary>
        /// <param name="start">The starting cell coordinate.</param>
        /// <param name="goal">The destination cell coordinate.</param>
        /// <param name="rule">Traversal contract defining passability and movement costs.</param>
        /// <param name="path">Output list of coordinates from start to goal (inclusive) if path found.</param>
        /// <param name="stopAdjacentIfBlocked">
        /// If true, and the goal cannot be entered, finds the optimal path to the nearest reachable neighbor of the goal.
        /// </param>
        /// <param name="maxBudget">Maximum total movement cost allowed for the path.</param>
        /// <param name="maxSearchIterations">Maximum number of node expansions before aborting to protect against unbounded search.</param>
        /// <param name="traveler">Optional entity reference passed to the traversal rule.</param>
        /// <returns>True if a path was found; false otherwise.</returns>
        public static bool TryFindPath(
            HexCoord start,
            HexCoord goal,
            ITraversalRule rule,
            out List<HexCoord> path,
            bool stopAdjacentIfBlocked = true,
            int maxBudget = int.MaxValue,
            int maxSearchIterations = 10000,
            object traveler = null)
        {
            path = null;
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            // If start itself cannot be entered, no path can begin
            if (!rule.CanEnter(start, traveler))
                return false;

            // Trivial case: already at goal
            if (start == goal)
            {
                path = new List<HexCoord> { start };
                return true;
            }

            bool goalPassable = rule.CanEnter(goal, traveler);

            // If goal is not passable and approach mode is disabled, path fails immediately
            if (!goalPassable && !stopAdjacentIfBlocked)
                return false;

            // Approach mode: if already adjacent to an impassable goal, we have already arrived next to it
            if (!goalPassable && start.DistanceTo(goal) == 1)
            {
                path = new List<HexCoord> { start };
                return true;
            }

            // Quick reachability pre-check: if all 6 neighbors of the goal are impassable,
            // no path can enter the goal or stop adjacent to it from any start != goal.
            bool hasPassableGoalNeighbor = false;
            for (int i = 0; i < 6; i++)
            {
                if (rule.CanEnter(goal.Neighbor(i), traveler))
                {
                    hasPassableGoalNeighbor = true;
                    break;
                }
            }

            if (!hasPassableGoalNeighbor)
                return false;

            var frontier = new MinBinaryHeap<HexCoord, PathPriority>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var costSoFar = new Dictionary<HexCoord, int>();
            var closedSet = new HashSet<HexCoord>();

            costSoFar[start] = 0;
            int startH = goalPassable ? start.DistanceTo(goal) : Math.Max(0, start.DistanceTo(goal) - 1);
            frontier.Enqueue(start, new PathPriority(startH, startH, start.Q, start.R));

            HexCoord destinationReached = default;
            bool found = false;

            var neighborBuffer = new HexCoord[6];
            int iterations = 0;

            while (frontier.Count > 0)
            {
                if (++iterations > maxSearchIterations)
                    break;

                var current = frontier.Dequeue();

                // If node was already finalized with an optimal path, skip stale queue entries
                if (!closedSet.Add(current))
                    continue;

                // Check termination condition
                if (goalPassable)
                {
                    if (current == goal)
                    {
                        destinationReached = current;
                        found = true;
                        break;
                    }
                }
                else
                {
                    // For impassable goal in approach mode, goal test is any neighbor of goal
                    if (current.DistanceTo(goal) == 1)
                    {
                        destinationReached = current;
                        found = true;
                        break;
                    }
                }

                int currentCost = costSoFar[current];

                current.GetNeighbors(neighborBuffer);
                for (int i = 0; i < 6; i++)
                {
                    var neighbor = neighborBuffer[i];

                    // Skip nodes that are already in the closed set
                    if (closedSet.Contains(neighbor))
                        continue;

                    // Special case: if neighbor is goal, but goal is impassable, we cannot step onto it
                    if (neighbor == goal && !goalPassable)
                        continue;

                    if (!rule.CanEnter(neighbor, traveler))
                        continue;

                    int stepCost = rule.GetMovementCost(current, neighbor, traveler);
                    if (stepCost < 1) stepCost = 1; // Safeguard against non-positive costs

                    int newCost = currentCost + stepCost;

                    if (newCost > maxBudget)
                        continue;

                    if (!costSoFar.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                    {
                        costSoFar[neighbor] = newCost;
                        cameFrom[neighbor] = current;

                        int h = goalPassable
                            ? neighbor.DistanceTo(goal)
                            : Math.Max(0, neighbor.DistanceTo(goal) - 1);

                        int f = newCost + h;
                        frontier.Enqueue(neighbor, new PathPriority(f, h, neighbor.Q, neighbor.R));
                    }
                }
            }

            if (!found)
                return false;

            path = ReconstructPath(cameFrom, start, destinationReached);
            return true;
        }

        private static List<HexCoord> ReconstructPath(
            Dictionary<HexCoord, HexCoord> cameFrom,
            HexCoord start,
            HexCoord end)
        {
            var path = new List<HexCoord>();
            var current = end;
            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }
    }
}
