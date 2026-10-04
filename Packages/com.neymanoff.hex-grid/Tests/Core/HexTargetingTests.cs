using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Neymanoff.HexGrid.Core.Tests
{
    [TestFixture]
    public class HexTargetingTests
    {
        [Test]
        public void BestDirection_ExactAxes_ReturnCorrectDirections()
        {
            var origin = new HexCoord(0, 0);

            Assert.AreEqual(HexDirection.East, HexTargetResolver.GetBestDirection(origin, new HexCoord(4, 0)));
            Assert.AreEqual(HexDirection.SouthEast, HexTargetResolver.GetBestDirection(origin, new HexCoord(3, -3)));
            Assert.AreEqual(HexDirection.SouthWest, HexTargetResolver.GetBestDirection(origin, new HexCoord(0, -2)));
            Assert.AreEqual(HexDirection.West, HexTargetResolver.GetBestDirection(origin, new HexCoord(-5, 0)));
            Assert.AreEqual(HexDirection.NorthWest, HexTargetResolver.GetBestDirection(origin, new HexCoord(-2, 2)));
            Assert.AreEqual(HexDirection.NorthEast, HexTargetResolver.GetBestDirection(origin, new HexCoord(0, 3)));
            Assert.AreEqual(HexDirection.East, HexTargetResolver.GetBestDirection(origin, origin), "Origin to origin defaults to East.");
        }

        [Test]
        public void SingleCell_WithinRange_ReturnsTarget()
        {
            var origin = new HexCoord(0, 0);
            var target = new HexCoord(2, 0);

            var cells = HexTargetResolver.Resolve(origin, target, TargetShape.SingleCell, range: 3);

            Assert.AreEqual(1, cells.Count);
            Assert.AreEqual(target, cells[0]);
        }

        [Test]
        public void SingleCell_BeyondRange_ReturnsEmpty()
        {
            var origin = new HexCoord(0, 0);
            var target = new HexCoord(5, 0);

            var cells = HexTargetResolver.Resolve(origin, target, TargetShape.SingleCell, range: 2);

            Assert.IsEmpty(cells);
        }

        [Test]
        public void Line_AlongAxis_GeneratesSequentialCells()
        {
            var origin = new HexCoord(0, 0);
            var cells = HexTargetResolver.ResolveLine(origin, HexDirection.East, length: 3, includeOrigin: false);

            Assert.AreEqual(3, cells.Count);
            Assert.AreEqual(new HexCoord(1, 0), cells[0]);
            Assert.AreEqual(new HexCoord(2, 0), cells[1]);
            Assert.AreEqual(new HexCoord(3, 0), cells[2]);

            var withOrigin = HexTargetResolver.ResolveLine(origin, HexDirection.East, length: 3, includeOrigin: true);
            Assert.AreEqual(4, withOrigin.Count);
            Assert.AreEqual(origin, withOrigin[0]);
        }

        [Test]
        public void Line_ZeroOrNegativeLength_ReturnsEmpty()
        {
            var origin = new HexCoord(0, 0);
            var cells = HexTargetResolver.ResolveLine(origin, HexDirection.East, length: 0, includeOrigin: false);
            Assert.IsEmpty(cells);

            cells = HexTargetResolver.ResolveLine(origin, HexDirection.East, length: -2, includeOrigin: false);
            Assert.IsEmpty(cells);
        }

        [Test]
        public void Cone_SymmetricalAndExactDistances_NoRangeDrift()
        {
            var origin = new HexCoord(0, 0);
            int range = 3;

            var cells = HexTargetResolver.ResolveCone(origin, HexDirection.East, range, includeOrigin: false);

            // Total cells for 120-degree cone of range R: R * (R + 2) => 3 * 5 = 15 cells
            Assert.AreEqual(15, cells.Count, "Cone of range 3 should contain exactly 15 cells.");

            // All cells must be strictly unique
            var uniqueSet = new HashSet<HexCoord>(cells);
            Assert.AreEqual(cells.Count, uniqueSet.Count, "All cone coordinates must be unique.");

            // Verify no range drift: every cell distance from origin must be between 1 and range
            foreach (var cell in cells)
            {
                int dist = origin.DistanceTo(cell);
                Assert.IsTrue(dist >= 1 && dist <= range, $"Cell {cell} has distance {dist}, which is outside [1..{range}].");
            }

            // Verify symmetrical width per depth layer: depth d has 2*d + 1 cells
            var depth1Cells = cells.Where(c => origin.DistanceTo(c) == 1).ToList();
            var depth2Cells = cells.Where(c => origin.DistanceTo(c) == 2).ToList();
            var depth3Cells = cells.Where(c => origin.DistanceTo(c) == 3).ToList();

            Assert.AreEqual(3, depth1Cells.Count, "Depth 1 should have 2*1 + 1 = 3 cells.");
            Assert.AreEqual(5, depth2Cells.Count, "Depth 2 should have 2*2 + 1 = 5 cells.");
            Assert.AreEqual(7, depth3Cells.Count, "Depth 3 should have 2*3 + 1 = 7 cells.");
        }

        [Test]
        public void Cone_AllSixFacings_PreserveIdenticalCellCountAndDistances()
        {
            var origin = new HexCoord(0, 0);
            int range = 2; // Expect 2 * 4 = 8 cells

            for (int d = 0; d < 6; d++)
            {
                var facing = (HexDirection)d;
                var cells = HexTargetResolver.ResolveCone(origin, facing, range);

                Assert.AreEqual(8, cells.Count, $"Facing {facing} must produce exactly 8 cells.");
                Assert.AreEqual(8, new HashSet<HexCoord>(cells).Count, $"Facing {facing} cells must all be unique.");

                foreach (var cell in cells)
                {
                    int dist = origin.DistanceTo(cell);
                    Assert.IsTrue(dist >= 1 && dist <= range, $"Facing {facing} cell {cell} has invalid distance {dist}.");
                }
            }
        }

        [Test]
        public void Area_CalculatesExactCellCounts()
        {
            var center = new HexCoord(2, 3);

            // Radius 0: 1 cell
            var r0 = HexTargetResolver.ResolveArea(center, radius: 0);
            Assert.AreEqual(1, r0.Count);
            Assert.AreEqual(center, r0[0]);

            // Radius 1: 3*1*2 + 1 = 7 cells
            var r1 = HexTargetResolver.ResolveArea(center, radius: 1);
            Assert.AreEqual(7, r1.Count);
            Assert.AreEqual(7, new HashSet<HexCoord>(r1).Count);
            Assert.IsTrue(r1.All(c => center.DistanceTo(c) <= 1));

            // Radius 2: 3*2*3 + 1 = 19 cells
            var r2 = HexTargetResolver.ResolveArea(center, radius: 2);
            Assert.AreEqual(19, r2.Count);
            Assert.AreEqual(19, new HashSet<HexCoord>(r2).Count);
            Assert.IsTrue(r2.All(c => center.DistanceTo(c) <= 2));

            // Radius 3: 3*3*4 + 1 = 37 cells
            var r3 = HexTargetResolver.ResolveArea(center, radius: 3);
            Assert.AreEqual(37, r3.Count);
            Assert.AreEqual(37, new HashSet<HexCoord>(r3).Count);
            Assert.IsTrue(r3.All(c => center.DistanceTo(c) <= 3));
        }

        [Test]
        public void Area_ExcludeCenter_ReturnsExpectedCount()
        {
            var center = new HexCoord(0, 0);
            var cells = HexTargetResolver.ResolveArea(center, radius: 2, includeCenter: false);

            Assert.AreEqual(18, cells.Count, "Radius 2 excluding center should have 19 - 1 = 18 cells.");
            Assert.IsFalse(cells.Contains(center), "Center should be excluded.");
        }

        [Test]
        public void Ring_CalculatesExactCellCountAndDistances()
        {
            var center = new HexCoord(-1, 2);

            // Radius 0: 1 cell
            var r0 = HexTargetResolver.ResolveRing(center, radius: 0);
            Assert.AreEqual(1, r0.Count);
            Assert.AreEqual(center, r0[0]);

            // Radius 1: 6 * 1 = 6 cells, all at distance exactly 1
            var r1 = HexTargetResolver.ResolveRing(center, radius: 1);
            Assert.AreEqual(6, r1.Count);
            Assert.AreEqual(6, new HashSet<HexCoord>(r1).Count);
            Assert.IsTrue(r1.All(c => center.DistanceTo(c) == 1));

            // Radius 2: 6 * 2 = 12 cells, all at distance exactly 2
            var r2 = HexTargetResolver.ResolveRing(center, radius: 2);
            Assert.AreEqual(12, r2.Count);
            Assert.AreEqual(12, new HashSet<HexCoord>(r2).Count);
            Assert.IsTrue(r2.All(c => center.DistanceTo(c) == 2));

            // Radius 3: 6 * 3 = 18 cells, all at distance exactly 3
            var r3 = HexTargetResolver.ResolveRing(center, radius: 3);
            Assert.AreEqual(18, r3.Count);
            Assert.AreEqual(18, new HashSet<HexCoord>(r3).Count);
            Assert.IsTrue(r3.All(c => center.DistanceTo(c) == 3));
        }

        [Test]
        public void Resolve_GenericDispatch_DispatchesAllShapesCorrectly()
        {
            var origin = new HexCoord(0, 0);
            var target = new HexCoord(3, 0);

            // SingleCell
            var single = HexTargetResolver.Resolve(origin, target, TargetShape.SingleCell, range: 5);
            Assert.AreEqual(1, single.Count);
            Assert.AreEqual(target, single[0]);

            // Line
            var line = HexTargetResolver.Resolve(origin, target, TargetShape.Line, range: 3);
            Assert.AreEqual(3, line.Count);
            Assert.AreEqual(target, line[^1]);

            // Cone
            var cone = HexTargetResolver.Resolve(origin, target, TargetShape.Cone, range: 2);
            Assert.AreEqual(8, cone.Count);

            // Area
            var area = HexTargetResolver.Resolve(origin, target, TargetShape.Area, range: 5, radius: 1);
            Assert.AreEqual(7, area.Count);
            Assert.IsTrue(area.Contains(target));

            // Ring
            var ring = HexTargetResolver.Resolve(origin, target, TargetShape.Ring, range: 5, radius: 1);
            Assert.AreEqual(6, ring.Count);
            Assert.IsTrue(ring.All(c => target.DistanceTo(c) == 1));
        }
    }
}
