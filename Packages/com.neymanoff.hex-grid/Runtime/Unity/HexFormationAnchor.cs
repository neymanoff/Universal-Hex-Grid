using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Specialized tactical spawner anchoring a squad formation on the hexagonal grid.
    /// Uses <see cref="FormationPatternSO"/> and <see cref="FormationPlanner"/> to position
    /// multi-unit teams (Player party, allied defensive squads, enemy waves) with live WYSIWYG Scene Gizmos.
    /// </summary>
    [AddComponentMenu("Hex Grid/Hex Formation Anchor")]
    public class HexFormationAnchor : HexSpawnerBase
    {
        [Header("Formation Configuration")]
        [Tooltip("The ScriptableObject defining relative slot offsets for this squad.")]
        [SerializeField] private FormationPatternSO _formationPattern;

        [Tooltip("Override alignment mode (e.g. FrontRowCenter vs Origin).")]
        [SerializeField] private FormationAlignmentMode _alignmentMode = FormationAlignmentMode.FrontRowCenter;

        [Header("Squad Unit Prefabs")]
        [Tooltip("List of unit prefabs assigned to formation slots in order (Slot 0, Slot 1, ...).")]
        [SerializeField] private List<GameObject> _unitPrefabs = new();

        [Header("Opponent Facing Target (Optional)")]
        [Tooltip("If true, spawned squad units rotate to face towards a specific target Transform (e.g. opponent anchor).")]
        [SerializeField] private bool _faceTargetTransform = false;

        [Tooltip("Target Transform to face when _faceTargetTransform is enabled.")]
        [SerializeField] private Transform _targetTransform;

        public FormationPatternSO FormationPattern
        {
            get => _formationPattern;
            set => _formationPattern = value;
        }

        public FormationAlignmentMode AlignmentMode
        {
            get => _alignmentMode;
            set => _alignmentMode = value;
        }

        public List<GameObject> UnitPrefabs => _unitPrefabs;

        public bool FaceTargetTransform
        {
            get => _faceTargetTransform;
            set => _faceTargetTransform = value;
        }

        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }

        /// <summary>
        /// Calculates all squad spawn slots using the configured formation pattern and anchor position.
        /// </summary>
        public override IReadOnlyList<HexSpawnSlot> CalculateSlots(HexTilemapBridge bridge)
        {
            if (bridge == null) bridge = ResolveBridge();
            if (bridge == null) return System.Array.Empty<HexSpawnSlot>();

            var anchorCoord = bridge.WorldToHex(transform.position);

            IReadOnlyList<HexCoord> relativeSlots;
            var alignment = _alignmentMode;

            if (_formationPattern != null)
            {
                relativeSlots = _formationPattern.GetRelativeSlots();
                alignment = _formationPattern.AlignmentMode;
            }
            else
            {
                // Fallback to single slot at anchor if no formation SO is assigned
                relativeSlots = new[] { HexCoord.Zero };
            }

            var resolvedCoords = FormationPlanner.BuildAnchoredLayout(
                relativeSlots,
                anchorCoord,
                _facingDirectionStep,
                alignment);

            var slots = new List<HexSpawnSlot>(resolvedCoords.Count);
            for (int i = 0; i < resolvedCoords.Count; i++)
            {
                var slotCoord = resolvedCoords[i];
                Vector3 worldPos = bridge.HexToUnitWorld(slotCoord);
                Quaternion rot;

                if (_faceTargetTransform && _targetTransform != null)
                {
                    Vector3 lookDir = _targetTransform.position - worldPos;
                    lookDir.y = 0f;
                    rot = lookDir.sqrMagnitude > 0.0001f
                        ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                        : CalculateFacingRotation(slotCoord, bridge);
                }
                else
                {
                    rot = CalculateFacingRotation(slotCoord, bridge);
                }

                slots.Add(new HexSpawnSlot(slotCoord, worldPos, rot, i, _faction));
            }

            return slots;
        }

        /// <summary>
        /// Spawns the squad units into their calculated formation slots and binds them to the grid.
        /// </summary>
        public override List<GameObject> Spawn(HexTilemapBridge bridge, HexOccupancyMap<GridOccupant> occupancyMap = null)
        {
            var spawned = new List<GameObject>();
            var slots = CalculateSlots(bridge);
            if (slots.Count == 0) return spawned;

            int count = Mathf.Min(slots.Count, _unitPrefabs.Count);
            if (_unitPrefabs.Count > 0 && count < _unitPrefabs.Count)
            {
                Debug.LogWarning($"[HexFormationAnchor] Unit count ({_unitPrefabs.Count}) exceeds formation slots ({slots.Count}). Spawning {count}.", this);
            }

            for (int i = 0; i < count; i++)
            {
                var prefab = _unitPrefabs[i];
                if (prefab == null) continue;

                var slot = slots[i];
                var instance = Instantiate(prefab, slot.WorldPosition, slot.Rotation);

                var occupant = instance.GetComponent<GridOccupant>();
                if (occupant == null)
                {
                    occupant = instance.AddComponent<GridOccupant>();
                }

                occupant.Bind(slot.Coord, _faction, occupancyMap);
                spawned.Add(instance);
            }

            return spawned;
        }

        private void OnDrawGizmos()
        {
            var bridge = ResolveBridge();
            var color = GetFactionColor(_faction);
            float radius = 0.5f;
            if (bridge != null) radius *= bridge.CellScale;

            // Draw anchor indicator
            DrawHexOutlineGizmo(transform.position, radius * 1.15f, color);

            if (bridge == null) return;

            var slots = CalculateSlots(bridge);
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                Vector3 slotGroundPos = bridge.HexToWorld(slot.Coord);

                // Slot hex outline
                DrawHexOutlineGizmo(slotGroundPos, radius, color);

                // Facing arrow for each slot
                DrawFacingArrowGizmo(slotGroundPos, slot.Rotation, 0.6f, color);

                // Line connecting anchor to slot
                Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.05f, slotGroundPos + Vector3.up * 0.05f);
            }
        }
    }
}
