using System.Collections.Generic;
using NUnit.Framework;

namespace Neymanoff.HexGrid.Core.Tests
{
    [TestFixture]
    public class HexCoordTests
    {
        [Test]
        public void CubicInvariant_HoldsForVariousCoordinates()
        {
            var testCoords = new[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, -1),
                new HexCoord(-5, 3),
                new HexCoord(10, 20),
                new HexCoord(-15, -25)
            };

            foreach (var coord in testCoords)
            {
                Assert.AreEqual(0, coord.Q + coord.R + coord.S, $"Cubic invariant failed for {coord}");
            }
        }

        [Test]
        public void Distance_ToSelf_IsZero()
        {
            var coord = new HexCoord(5, -3);
            Assert.AreEqual(0, coord.DistanceTo(coord));
        }

        [Test]
        public void Distance_ImmediateNeighbors_IsAlwaysOne()
        {
            var center = new HexCoord(2, 3);
            for (int i = 0; i < 6; i++)
            {
                var neighbor = center.Neighbor(i);
                Assert.AreEqual(1, center.DistanceTo(neighbor), $"Distance to neighbor {i} should be 1.");
            }
        }

        [Test]
        public void Distance_IsSymmetric()
        {
            var a = new HexCoord(-4, 7);
            var b = new HexCoord(8, -2);
            Assert.AreEqual(a.DistanceTo(b), b.DistanceTo(a));
        }

        [TestCase(0, 0, 3, 0, 3)]
        [TestCase(0, 0, 0, -4, 4)]
        [TestCase(0, 0, -3, 3, 3)]
        [TestCase(1, 2, 0, 0, 3)]
        [TestCase(1, 2, -2, -1, 6)]
        [TestCase(-2, -2, 0, 0, 4)]
        [TestCase(-2, -2, 2, 2, 8)]
        public void Distance_MatchesExpectedValues(int q1, int r1, int q2, int r2, int expectedDistance)
        {
            var a = new HexCoord(q1, r1);
            var b = new HexCoord(q2, r2);
            Assert.AreEqual(expectedDistance, a.DistanceTo(b));
            Assert.AreEqual(expectedDistance, HexCoord.Distance(a, b));
        }

        [Test]
        public void Neighbors_AreAllUnique()
        {
            var center = new HexCoord(0, 0);
            var set = new HashSet<HexCoord>();

            for (int i = 0; i < 6; i++)
            {
                set.Add(center.Neighbor(i));
            }

            Assert.AreEqual(6, set.Count, "All 6 neighbors must be distinct coordinates.");
        }

        [Test]
        public void Neighbors_OppositeDirections_CancelOut()
        {
            var center = new HexCoord(0, 0);

            var east = center.Neighbor(HexDirection.East);
            var west = center.Neighbor(HexDirection.West);
            Assert.AreEqual(center, (east - center) + (west - center));

            var se = center.Neighbor(HexDirection.SouthEast);
            var nw = center.Neighbor(HexDirection.NorthWest);
            Assert.AreEqual(center, (se - center) + (nw - center));

            var sw = center.Neighbor(HexDirection.SouthWest);
            var ne = center.Neighbor(HexDirection.NorthEast);
            Assert.AreEqual(center, (sw - center) + (ne - center));
        }

        [Test]
        public void Neighbors_IndexWrapping_HandlesNegativeAndOverflowIndices()
        {
            var center = new HexCoord(1, 1);

            Assert.AreEqual(center.Neighbor(HexDirection.East), center.Neighbor(0));
            Assert.AreEqual(center.Neighbor(HexDirection.East), center.Neighbor(6));
            Assert.AreEqual(center.Neighbor(HexDirection.East), center.Neighbor(-6));

            Assert.AreEqual(center.Neighbor(HexDirection.NorthEast), center.Neighbor(5));
            Assert.AreEqual(center.Neighbor(HexDirection.NorthEast), center.Neighbor(11));
            Assert.AreEqual(center.Neighbor(HexDirection.NorthEast), center.Neighbor(-1));
        }

        [Test]
        public void Neighbors_ZeroAllocGetNeighbors_MatchesAllocatingMethod()
        {
            var center = new HexCoord(3, -4);
            var arrayAllocated = center.GetNeighbors();

            var buffer = new HexCoord[6];
            center.GetNeighbors(buffer);

            Assert.AreEqual(6, arrayAllocated.Length);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(arrayAllocated[i], buffer[i]);
            }
        }

        [Test]
        public void OddR_RoundtripConversion_PreservesCoordinates()
        {
            for (int col = -30; col <= 30; col++)
            {
                for (int row = -30; row <= 30; row++)
                {
                    var hex = HexCoord.FromOddR(col, row);
                    var (resCol, resRow) = hex.ToOddR();

                    Assert.AreEqual(col, resCol, $"Col mismatch at input ({col}, {row}) -> Hex {hex}");
                    Assert.AreEqual(row, resRow, $"Row mismatch at input ({col}, {row}) -> Hex {hex}");
                }
            }
        }

        [Test]
        public void Operators_Arithmetic_CalculatesCorrectly()
        {
            var a = new HexCoord(2, 3);
            var b = new HexCoord(1, -2);

            Assert.AreEqual(new HexCoord(3, 1), a + b);
            Assert.AreEqual(new HexCoord(1, 5), a - b);
            Assert.AreEqual(new HexCoord(6, 9), a * 3);
            Assert.AreEqual(new HexCoord(6, 9), 3 * a);
        }

        [Test]
        public void Equality_AndHashCode_WorkCorrectly()
        {
            var a = new HexCoord(5, -2);
            var b = new HexCoord(5, -2);
            var c = new HexCoord(5, 2);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void HexDirection_Opposite_ReturnsOppositeDirection()
        {
            Assert.AreEqual(HexDirection.West, HexDirection.East.Opposite());
            Assert.AreEqual(HexDirection.East, HexDirection.West.Opposite());
            Assert.AreEqual(HexDirection.NorthWest, HexDirection.SouthEast.Opposite());
            Assert.AreEqual(HexDirection.NorthEast, HexDirection.SouthWest.Opposite());
        }

        [Test]
        public void HexDirection_Rotation_WorksCorrectly()
        {
            Assert.AreEqual(HexDirection.SouthEast, HexDirection.East.RotateClockwise(1));
            Assert.AreEqual(HexDirection.East, HexDirection.East.RotateClockwise(6));
            Assert.AreEqual(HexDirection.NorthEast, HexDirection.East.RotateCounterClockwise(1));
            Assert.AreEqual(HexDirection.East, HexDirection.East.RotateCounterClockwise(6));
        }
    }
}
