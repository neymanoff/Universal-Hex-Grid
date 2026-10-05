using System.Collections.Generic;
using NUnit.Framework;

namespace Neymanoff.HexGrid.Core.Tests
{
    [TestFixture]
    public class FormationPlannerTests
    {
        [Test]
        public void HexCoord_RotateCw_RotatesCorrectlyThroughAllDirections()
        {
            var initial = HexCoord.GetDirectionVector(0); // East (1, 0)

            for (int step = 0; step < 6; step++)
            {
                var rotated = initial.RotateCw(step);
                var expected = HexCoord.GetDirectionVector(step);
                Assert.AreEqual(expected, rotated, $"RotateCw step {step} mismatch.");
            }

            // Full 360 wrap
            Assert.AreEqual(initial, initial.RotateCw(6));
            Assert.AreEqual(initial, initial.RotateCw(-6));
        }

        [Test]
        public void BuildAnchoredLayout_EmptyInput_ReturnsEmpty()
        {
            var result = FormationPlanner.BuildAnchoredLayout(new List<HexCoord>(), new HexCoord(5, 5));
            Assert.IsEmpty(result);
        }

        [Test]
        public void BuildAnchoredLayout_OriginMode_OffsetsRelativeToAnchor()
        {
            var pattern = new List<HexCoord>
            {
                new(0, 0),
                new(1, 0),
                new(0, 1)
            };

            var anchor = new HexCoord(10, -5);
            var result = FormationPlanner.BuildAnchoredLayout(pattern, anchor, rotationStepsCw: 0, FormationAlignmentMode.Origin);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(new HexCoord(10, -5), result[0]);
            Assert.AreEqual(new HexCoord(11, -5), result[1]);
            Assert.AreEqual(new HexCoord(10, -4), result[2]);
        }

        [Test]
        public void BuildAnchoredLayout_OriginMode_Rotated120Degrees()
        {
            var pattern = new List<HexCoord>
            {
                new(1, 0) // East direction
            };

            var anchor = new HexCoord(0, 0);
            // 120° CW is step 2 -> should be SouthWest (0, -1)
            var result = FormationPlanner.BuildAnchoredLayout(pattern, anchor, rotationStepsCw: 2, FormationAlignmentMode.Origin);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new HexCoord(0, -1), result[0]);
        }

        [Test]
        public void BuildAnchoredLayout_FrontRowCenterMode_AlignsFrontRowToAnchor()
        {
            // 2-3 Formation:
            // Front row (Q=1): (1, -1), (1, 0)
            // Back row  (Q=0): (0, -1), (0, 0), (0, 1)
            var pattern = new List<HexCoord>
            {
                new(1, -1), new(1, 0),
                new(0, -1), new(0, 0), new(0, 1)
            };

            var anchor = new HexCoord(4, 2);
            var result = FormationPlanner.BuildAnchoredLayout(pattern, anchor, rotationStepsCw: 0, FormationAlignmentMode.FrontRowCenter);

            Assert.AreEqual(5, result.Count);
            // Front row should have maximum Q equal to anchor.Q (4)
            int maxQ = int.MinValue;
            foreach (var slot in result)
            {
                if (slot.Q > maxQ) maxQ = slot.Q;
            }
            Assert.AreEqual(anchor.Q, maxQ, "Front row Q must align with anchor Q");
        }

        [Test]
        public void BuildAnchoredLayoutOddR_PreservesLegendsCompatibility()
        {
            // 3-2 Formation cells in Odd-R
            var absCells = new List<(int col, int row)>
            {
                (0, 0), (1, 0), (2, 0),
                (0, 1), (1, 1)
            };

            var anchor = (5, 5);
            var playerLayout = FormationPlanner.BuildAnchoredLayoutOddR(absCells, anchor, flip180: false, applyBase120: true);
            var enemyLayout = FormationPlanner.BuildAnchoredLayoutOddR(absCells, anchor, flip180: true, applyBase120: true);

            Assert.AreEqual(5, playerLayout.Count);
            Assert.AreEqual(5, enemyLayout.Count);
            // Player and enemy should be mirrored/facing differently
            Assert.AreNotEqual(playerLayout[0], enemyLayout[0]);
        }
    }
}
