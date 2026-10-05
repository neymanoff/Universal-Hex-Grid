using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Specialized spawner for single entities on the hexagonal grid.
    /// Ideal for World Map player start positions, roaming enemy mobs, quest NPCs, bosses, or interactable objects.
    /// </summary>
    [AddComponentMenu("Hex Grid/Hex Spawn Point")]
    public class HexSpawnPoint : HexSpawnerBase
    {
        [Header("Single Unit Setup")]
        [Tooltip("The prefab to instantiate at this spawn point (e.g. Player Hero, Enemy mob, NPC).")]
        [SerializeField] private GameObject _unitPrefab;

        public GameObject UnitPrefab
        {
            get => _unitPrefab;
            set => _unitPrefab = value;
        }

        /// <summary>
        /// Returns the single spawn slot at this point's grid coordinate.
        /// </summary>
        public override IReadOnlyList<HexSpawnSlot> CalculateSlots(HexTilemapBridge bridge)
        {
            if (bridge == null) bridge = ResolveBridge();
            if (bridge == null) return System.Array.Empty<HexSpawnSlot>();

            var coord = bridge.WorldToHex(transform.position);
            Vector3 worldPos = bridge.HexToUnitWorld(coord);
            Quaternion rot = CalculateFacingRotation(coord, bridge);

            return new[]
            {
                new HexSpawnSlot(coord, worldPos, rot, 0, _faction)
            };
        }

        /// <summary>
        /// Spawns the assigned unit prefab at this point and registers it with the occupancy map.
        /// </summary>
        public override List<GameObject> Spawn(HexTilemapBridge bridge, HexOccupancyMap<GridOccupant> occupancyMap = null)
        {
            var spawned = new List<GameObject>(1);
            if (_unitPrefab == null)
            {
                Debug.LogWarning($"[HexSpawnPoint] Cannot spawn: UnitPrefab is null on '{gameObject.name}'.", this);
                return spawned;
            }

            var slots = CalculateSlots(bridge);
            if (slots.Count == 0) return spawned;

            var slot = slots[0];
            var instance = Instantiate(_unitPrefab, slot.WorldPosition, slot.Rotation);

            var occupant = instance.GetComponent<GridOccupant>();
            if (occupant == null)
            {
                occupant = instance.AddComponent<GridOccupant>();
            }

            occupant.Bind(slot.Coord, _faction, occupancyMap);
            spawned.Add(instance);

            return spawned;
        }

        private void OnDrawGizmos()
        {
            var color = GetFactionColor(_faction);
            float radius = 0.5f;
            if (_bridge != null) radius *= _bridge.CellScale;

            DrawHexOutlineGizmo(transform.position, radius, color);
            Quaternion rot = CalculateFacingRotation(_bridge ? _bridge.WorldToHex(transform.position) : HexCoord.Zero, _bridge);
            DrawFacingArrowGizmo(transform.position, rot, 0.7f, color);
        }
    }
}
