using System.Collections.Generic;
using NUnit.Framework;

namespace Neymanoff.HexGrid.Core.Tests
{
    [TestFixture]
    public class HexPathfindingTests
    {
        private class TestTraversalRule : ITraversalRule
        {
            public HashSet<HexCoord> BlockedCells = new();
            public Dictionary<HexCoord, int> CellCosts = new();
            public int DefaultCost = 1;

            public bool CanEnter(HexCoord coord, object traveler = null)
            {
                return !BlockedCells.Contains(coord);
            }

            public int GetMovementCost(HexCoord from, HexCoord to, object traveler = null)
            {
                return CellCosts.TryGetValue(to, out int cost) ? cost : DefaultCost;
            }
        }

        private class MockEntity
        {
            public string Name { get; set; }
            public MockEntity(string name) => Name = name;
        }

        [Test]
        public void ShortestPath_OnFlatGrid_MatchesExactDistance()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(4, 0);

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path);

            Assert.IsTrue(found);
            Assert.IsNotNull(path);
            Assert.AreEqual(5, path.Count, "Path from (0,0) to (4,0) inclusive should have 5 nodes.");
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[^1]);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Assert.AreEqual(1, path[i].DistanceTo(path[i + 1]), "Every step in path must be to an adjacent cell.");
            }
        }

        [Test]
        public void Pathfinding_AvoidsObstacles()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(3, 0);

            // Place a wall between start and goal
            rule.BlockedCells.Add(new HexCoord(1, 0));
            rule.BlockedCells.Add(new HexCoord(1, -1));
            rule.BlockedCells.Add(new HexCoord(1, 1));

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path);

            Assert.IsTrue(found, "Path should route around the wall.");
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[^1]);

            foreach (var step in path)
            {
                Assert.IsFalse(rule.BlockedCells.Contains(step), $"Path stepped onto blocked cell {step}!");
            }
        }

        [Test]
        public void Pathfinding_UnreachableGoal_ReturnsFalse()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(3, 0);

            // Surround goal on all 6 sides
            for (int i = 0; i < 6; i++)
            {
                rule.BlockedCells.Add(goal.Neighbor(i));
            }

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path, stopAdjacentIfBlocked: false);
            Assert.IsFalse(found);
            Assert.IsNull(path);
        }

        [Test]
        public void Pathfinding_ApproachMode_WhenGoalBlocked_StopsNextToGoal()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(3, 0);

            // Goal itself is impassable (e.g. occupied by enemy or obstacle)
            rule.BlockedCells.Add(goal);

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path, stopAdjacentIfBlocked: true);

            Assert.IsTrue(found, "Approach mode should find path adjacent to goal.");
            Assert.IsNotNull(path);
            Assert.AreEqual(start, path[0]);
            Assert.AreNotEqual(goal, path[^1], "Path should not end on the impassable goal itself.");
            Assert.AreEqual(1, path[^1].DistanceTo(goal), "Path end must be adjacent to the goal.");
        }

        [Test]
        public void Pathfinding_AlreadyAdjacentToBlockedGoal_ReturnsSingleStartNode()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(1, 0);
            var goal = new HexCoord(0, 0);

            rule.BlockedCells.Add(goal);

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path, stopAdjacentIfBlocked: true);

            Assert.IsTrue(found);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(start, path[0]);
        }

        [Test]
        public void Pathfinding_ExceedingBudget_ReturnsFalse()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(5, 0); // 5 steps needed

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path, maxBudget: 3);
            Assert.IsFalse(found, "Path requiring 5 movement points should fail when budget is 3.");
            Assert.IsNull(path);
        }

        [Test]
        public void Pathfinding_CostAware_PrefersLowCostDetourOverHighCostDirect()
        {
            var rule = new TestTraversalRule();
            var start = new HexCoord(0, 0);
            var goal = new HexCoord(2, 0);

            // Direct route (1,0) has high cost (e.g. mud)
            rule.CellCosts[new HexCoord(1, 0)] = 20;

            bool found = HexPathfinder.TryFindPath(start, goal, rule, out var path);

            Assert.IsTrue(found);
            Assert.IsFalse(path.Contains(new HexCoord(1, 0)), "Path should take the detour around high cost mud.");
        }

        [Test]
        public void FloodFill_ReachableZone_CalculatesExactCellCount()
        {
            var rule = new TestTraversalRule();
            var origin = new HexCoord(0, 0);

            // Radius 2 on unobstructed grid: 1 (center) + 6 (r1) + 12 (r2) = 19 cells
            var zone = HexFloodFill.GetReachableZone(origin, 2, rule);

            Assert.AreEqual(19, zone.Count);
            Assert.IsTrue(zone.ContainsKey(origin));
            Assert.AreEqual(0, zone[origin]);

            foreach (var kvp in zone)
            {
                Assert.IsTrue(origin.DistanceTo(kvp.Key) <= 2);
                Assert.AreEqual(origin.DistanceTo(kvp.Key), kvp.Value);
            }
        }

        [Test]
        public void FloodFill_RespectsObstacles()
        {
            var rule = new TestTraversalRule();
            var origin = new HexCoord(0, 0);

            rule.BlockedCells.Add(new HexCoord(1, 0));

            var zone = HexFloodFill.GetReachableZone(origin, 1, rule);

            Assert.IsFalse(zone.ContainsKey(new HexCoord(1, 0)));
            Assert.AreEqual(6, zone.Count); // 7 minus 1 blocked
        }

        [Test]
        public void OccupancyMap_TracksEntitiesAndVacatesCorrectly()
        {
            var map = new HexOccupancyMap<MockEntity>();
            var warrior = new MockEntity("Warrior");
            var archer = new MockEntity("Archer");

            var cellA = new HexCoord(1, 2);
            var cellB = new HexCoord(3, 4);

            Assert.IsTrue(map.TryOccupy(cellA, warrior));
            Assert.IsTrue(map.IsOccupied(cellA));
            Assert.IsFalse(map.IsOccupied(cellB));

            // Cannot occupy same cell with another entity
            Assert.IsFalse(map.TryOccupy(cellA, archer));

            // Moving warrior to cellB automatically vacates cellA
            Assert.IsTrue(map.TryOccupy(cellB, warrior));
            Assert.IsFalse(map.IsOccupied(cellA));
            Assert.IsTrue(map.IsOccupied(cellB));

            // Archer can now take cellA
            Assert.IsTrue(map.TryOccupy(cellA, archer));

            // Query by occupant
            Assert.IsTrue(map.TryGetCoordinate(warrior, out var warriorCoord));
            Assert.AreEqual(cellB, warriorCoord);

            // Vacate
            Assert.IsTrue(map.Vacate(warrior));
            Assert.IsFalse(map.IsOccupied(cellB));
            Assert.AreEqual(1, map.Count);
        }
    }
}
