using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// ScriptableObject defining reusable squad formation patterns (2-3, 3-2, 1-2-1, Line, Wedge, etc.).
    /// Holds relative slot coordinates and can bake custom shapes directly from authored Tilemaps.
    /// </summary>
    [CreateAssetMenu(fileName = "Formation_New", menuName = "Hex Grid/Formation Pattern", order = 100)]
    public class FormationPatternSO : ScriptableObject
    {
        [Tooltip("Human-readable formation identifier (e.g. 'Front 2, Back 3').")]
        [SerializeField] private string _formationName = "Standard Formation";

        [Tooltip("How this formation aligns relative to the anchor cell.")]
        [SerializeField] private FormationAlignmentMode _alignmentMode = FormationAlignmentMode.FrontRowCenter;

        [Tooltip("Slot coordinates relative to the formation origin (Q = X, R = Y in Axial space).")]
        [SerializeField] private List<Vector2Int> _slotOffsets = new();

        public string FormationName => _formationName;
        public FormationAlignmentMode AlignmentMode => _alignmentMode;
        public int SlotCount => _slotOffsets.Count;

        /// <summary>
        /// Returns all slot coordinates as pure axial <see cref="HexCoord"/> objects.
        /// </summary>
        public IReadOnlyList<HexCoord> GetRelativeSlots()
        {
            var result = new HexCoord[_slotOffsets.Count];
            for (int i = 0; i < _slotOffsets.Count; i++)
            {
                result[i] = new HexCoord(_slotOffsets[i].x, _slotOffsets[i].y);
            }
            return result;
        }

        /// <summary>
        /// Sets slot offsets directly from axial coordinates.
        /// </summary>
        public void SetSlots(IEnumerable<HexCoord> slots, FormationAlignmentMode alignmentMode = FormationAlignmentMode.FrontRowCenter)
        {
            _slotOffsets.Clear();
            _alignmentMode = alignmentMode;
            if (slots != null)
            {
                foreach (var s in slots)
                {
                    _slotOffsets.Add(new Vector2Int(s.Q, s.R));
                }
            }
        }

        /// <summary>
        /// Bakes formation slots directly from a painted Tilemap by sampling occupied tiles.
        /// Converts from Tilemap Odd-R to Axial coordinates and centers the formation relative to minimum bounds.
        /// </summary>
        public void BakeFromTilemap(Tilemap sourceTilemap, bool centerToMedian = true)
        {
            if (sourceTilemap == null) return;

            var sampledCoords = new List<HexCoord>();
            var bounds = sourceTilemap.cellBounds;

            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y <= bounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (sourceTilemap.HasTile(pos))
                    {
                        sampledCoords.Add(HexTilemapBridge.TilemapCellToHex(pos));
                    }
                }
            }

            if (sampledCoords.Count == 0) return;

            // Sort top-to-bottom, left-to-right (canonical order)
            sampledCoords.Sort((a, b) =>
            {
                int cmpR = b.R.CompareTo(a.R);
                if (cmpR != 0) return cmpR;
                return a.Q.CompareTo(b.Q);
            });

            if (centerToMedian)
            {
                // Align relative to median / first slot
                var origin = sampledCoords[0];
                for (int i = 0; i < sampledCoords.Count; i++)
                {
                    sampledCoords[i] = sampledCoords[i] - origin;
                }
            }

            SetSlots(sampledCoords, _alignmentMode);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // --- Static Factory Presets ---

        public static FormationPatternSO CreatePreset2_3()
        {
            var so = CreateInstance<FormationPatternSO>();
            so._formationName = "2-3 Standard Squad";
            so._alignmentMode = FormationAlignmentMode.FrontRowCenter;
            so.SetSlots(new[]
            {
                new HexCoord(1, -1), new HexCoord(1, 0),        // Front row (2 units)
                new HexCoord(0, -1), new HexCoord(0, 0), new HexCoord(0, 1) // Back row (3 units)
            });
            return so;
        }

        public static FormationPatternSO CreatePreset3_2()
        {
            var so = CreateInstance<FormationPatternSO>();
            so._formationName = "3-2 Vanguard Squad";
            so._alignmentMode = FormationAlignmentMode.FrontRowCenter;
            so.SetSlots(new[]
            {
                new HexCoord(1, -1), new HexCoord(1, 0), new HexCoord(1, 1), // Front row (3 units)
                new HexCoord(0, -1), new HexCoord(0, 0)         // Back row (2 units)
            });
            return so;
        }

        public static FormationPatternSO CreatePreset1_2_1()
        {
            var so = CreateInstance<FormationPatternSO>();
            so._formationName = "1-2-1 Diamond Squad";
            so._alignmentMode = FormationAlignmentMode.FrontRowCenter;
            so.SetSlots(new[]
            {
                new HexCoord(2, 0),                            // Vanguard (1)
                new HexCoord(1, -1), new HexCoord(1, 1),       // Flanks (2)
                new HexCoord(0, 0)                             // Rearguard (1)
            });
            return so;
        }

        public static FormationPatternSO CreatePresetLine(int count = 4)
        {
            var so = CreateInstance<FormationPatternSO>();
            so._formationName = $"Line ({count}) Defense";
            so._alignmentMode = FormationAlignmentMode.FrontRowCenter;
            var list = new List<HexCoord>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new HexCoord(0, i - count / 2));
            }
            so.SetSlots(list);
            return so;
        }

        public static FormationPatternSO CreatePresetWedge()
        {
            var so = CreateInstance<FormationPatternSO>();
            so._formationName = "Wedge Spearhead";
            so._alignmentMode = FormationAlignmentMode.FrontRowCenter;
            so.SetSlots(new[]
            {
                new HexCoord(2, 0),                            // Point
                new HexCoord(1, -1), new HexCoord(1, 1),       // Wing
                new HexCoord(0, -2), new HexCoord(0, 2)        // Flank
            });
            return so;
        }
    }
}
