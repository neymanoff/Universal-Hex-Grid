using System;
using System.Collections.Generic;
using System.Linq;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Alignment mode specifying how formation slot coordinates are positioned relative to the anchor cell.
    /// </summary>
    public enum FormationAlignmentMode
    {
        /// <summary>
        /// The formation origin (0, 0) is placed directly at the anchor coordinate.
        /// Slot offsets are rotated around (0, 0) and added directly to the anchor.
        /// </summary>
        Origin = 0,

        /// <summary>
        /// The front-row center of the formation is aligned to the anchor coordinate.
        /// Matches frontline tactical battlefield deployment.
        /// </summary>
        FrontRowCenter = 1
    }

    /// <summary>
    /// Pure C# domain service computing anchored, rotated battlefield squad formations.
    /// Supports arbitrary squad sizes, custom layouts, and 6-directional rotation.
    /// </summary>
    public static class FormationPlanner
    {
        /// <summary>
        /// Builds an anchored formation layout from relative slot coordinates.
        /// </summary>
        /// <param name="relativeSlots">Collection of slot coordinates relative to formation origin.</param>
        /// <param name="anchor">Target world hex coordinate for the formation anchor.</param>
        /// <param name="rotationStepsCw">Number of 60-degree clockwise rotation steps [0..5].</param>
        /// <param name="alignmentMode">How the formation is anchored (Origin vs FrontRowCenter).</param>
        /// <returns>List of final axial HexCoords for each squad member in order.</returns>
        public static List<HexCoord> BuildAnchoredLayout(
            IReadOnlyList<HexCoord> relativeSlots,
            HexCoord anchor,
            int rotationStepsCw = 0,
            FormationAlignmentMode alignmentMode = FormationAlignmentMode.Origin)
        {
            if (relativeSlots == null || relativeSlots.Count == 0)
                return new List<HexCoord>();

            var rotated = new List<HexCoord>(relativeSlots.Count);
            for (int i = 0; i < relativeSlots.Count; i++)
            {
                rotated.Add(relativeSlots[i].RotateCw(rotationStepsCw));
            }

            if (alignmentMode == FormationAlignmentMode.Origin)
            {
                var result = new List<HexCoord>(rotated.Count);
                for (int i = 0; i < rotated.Count; i++)
                {
                    result.Add(anchor + rotated[i]);
                }
                return result;
            }

            // FrontRowCenter alignment:
            // Find front row. In axial coordinates facing forward (+Q direction),
            // find max Q of the rotated slots.
            int frontQ = rotated.Max(c => c.Q);
            var frontRow = rotated.Where(c => c.Q == frontQ).OrderBy(c => c.R).ToList();

            int dQ = frontQ;
            int dR;
            if (frontRow.Count == 1)
            {
                dR = frontRow[0].R;
            }
            else if (frontRow.Count % 2 != 0)
            {
                dR = frontRow[frontRow.Count / 2].R;
            }
            else
            {
                int r0 = frontRow[frontRow.Count / 2 - 1].R;
                int r1 = frontRow[frontRow.Count / 2].R;
                dR = (r0 + r1) / 2;
            }

            var alignedResult = new List<HexCoord>(rotated.Count);
            for (int i = 0; i < rotated.Count; i++)
            {
                var c = rotated[i];
                alignedResult.Add(new HexCoord(c.Q - dQ + anchor.Q, c.R - dR + anchor.R));
            }

            return alignedResult;
        }

        /// <summary>
        /// Backwards-compatible formation planner method matching Legends: Legacy of the Lost.
        /// Takes Odd-R integer pairs, converts to Axial, applies base +120° and optional +180° flip,
        /// aligns front row median to anchor, and returns resulting Odd-R coordinates.
        /// </summary>
        public static List<(int col, int row)> BuildAnchoredLayoutOddR(
            IReadOnlyList<(int col, int row)> absCellsOddR,
            (int col, int row) anchorOddR,
            bool flip180 = false,
            bool applyBase120 = true)
        {
            if (absCellsOddR == null || absCellsOddR.Count == 0)
                return new List<(int col, int row)>();

            var anchorAxial = HexCoord.FromOddR(anchorOddR.col, anchorOddR.row);

            // Compute rotation steps (120° = 2 steps CW, 180° = 3 steps CW)
            int steps = 0;
            if (applyBase120) steps += 2;
            if (flip180) steps += 3;

            var axialSlots = new List<HexCoord>(absCellsOddR.Count);
            for (int i = 0; i < absCellsOddR.Count; i++)
            {
                axialSlots.Add(HexCoord.FromOddR(absCellsOddR[i].col, absCellsOddR[i].row));
            }

            // Rotate
            var tmp = new List<HexCoord>(axialSlots.Count);
            for (int i = 0; i < axialSlots.Count; i++)
            {
                tmp.Add(axialSlots[i].RotateCw(steps));
            }

            // Detect front row after transform exactly as Legends did
            int frontQ = flip180 ? tmp.Min(v => v.Q) : tmp.Max(v => v.Q);
            var frontRow = tmp.Where(v => v.Q == frontQ).OrderBy(v => v.R).ToList();

            int dQ = frontQ;
            int dR;
            if (frontRow.Count == 1)
                dR = frontRow[0].R;
            else if (frontRow.Count % 2 != 0)
                dR = frontRow[frontRow.Count / 2].R;
            else
            {
                int r0 = frontRow[frontRow.Count / 2 - 1].R;
                int r1 = frontRow[frontRow.Count / 2].R;
                dR = (r0 + r1) / 2;
            }

            var result = new List<(int col, int row)>(tmp.Count);
            for (int i = 0; i < tmp.Count; i++)
            {
                var v = tmp[i];
                var shifted = new HexCoord(v.Q - dQ + anchorAxial.Q, v.R - dR + anchorAxial.R);
                result.Add(shifted.ToOddR());
            }

            return result;
        }
    }
}
