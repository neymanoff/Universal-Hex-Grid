using System;
using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Abstract base component for hexagonal grid spawners.
    /// Supports multi-team factions, wave/group assignment, facing direction, and WYSIWYG Scene Gizmos.
    /// </summary>
    [ExecuteAlways]
    public abstract class HexSpawnerBase : MonoBehaviour
    {
        [Header("Faction & Identity")]
        [Tooltip("The faction / owner of units spawned by this spawner.")]
        [SerializeField] protected CellOwner _faction = CellOwner.Player;

        [Tooltip("Identifier used to trigger specific spawn waves or groups (e.g. 'InitialSquad', 'Wave_1', 'Ambush').")]
        [SerializeField] protected string _waveGroup = "Default";

        [Header("Orientation & Placement")]
        [Tooltip("Clockwise hex direction step [0..5] that spawned units will face (0 = East, 1 = SE, 2 = SW, 3 = West, 4 = NW, 5 = NE).")]
        [Range(0, 5)]
        [SerializeField] protected int _facingDirectionStep = 0;

        [Tooltip("Automatically snaps this spawner's transform position to the center of the nearest hex cell in the Editor.")]
        [SerializeField] protected bool _autoSnapToGrid = true;

        [Header("Grid References (Optional)")]
        [Tooltip("Explicit bridge reference. If null, automatically discovers HexTilemapBridge in the active scene.")]
        [SerializeField] protected HexTilemapBridge _bridge;

        public CellOwner Faction
        {
            get => _faction;
            set => _faction = value;
        }

        public string WaveGroup
        {
            get => _waveGroup;
            set => _waveGroup = value;
        }

        public int FacingDirectionStep
        {
            get => _facingDirectionStep;
            set => _facingDirectionStep = (value % 6 + 6) % 6;
        }

        /// <summary>
        /// Resolves the active <see cref="HexTilemapBridge"/> in the scene.
        /// </summary>
        public HexTilemapBridge ResolveBridge()
        {
            if (_bridge != null) return _bridge;
            _bridge = FindAnyObjectByType<HexTilemapBridge>();
            return _bridge;
        }

        /// <summary>
        /// Calculates all spawn slots (coordinates, world positions, and rotations) for this spawner.
        /// </summary>
        public abstract IReadOnlyList<HexSpawnSlot> CalculateSlots(HexTilemapBridge bridge);

        /// <summary>
        /// Instantiates and binds units to the grid at their resolved spawn slots.
        /// </summary>
        public abstract List<GameObject> Spawn(HexTilemapBridge bridge, HexOccupancyMap<GridOccupant> occupancyMap = null);

        /// <summary>
        /// Computes the world-space rotation facing the neighbor in direction <see cref="_facingDirectionStep"/>.
        /// </summary>
        public Quaternion CalculateFacingRotation(HexCoord origin, HexTilemapBridge bridge)
        {
            if (bridge == null) return transform.rotation;
            var neighbor = origin.Neighbor(_facingDirectionStep);
            Vector3 originPos = bridge.HexToWorld(origin);
            Vector3 targetPos = bridge.HexToWorld(neighbor);
            Vector3 dir = targetPos - originPos;
            dir.y = 0f;

            return dir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                : transform.rotation;
        }

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying && _autoSnapToGrid)
            {
                var bridge = ResolveBridge();
                if (bridge != null)
                {
                    var hex = bridge.WorldToHex(transform.position);
                    Vector3 snapped = bridge.HexToWorld(hex);
                    if (Vector3.Distance(transform.position, snapped) > 0.001f)
                    {
                        transform.position = snapped;
                    }
                }
            }
        }

        // --- Visual Gizmos ---

        public static Color GetFactionColor(CellOwner faction)
        {
            return faction switch
            {
                CellOwner.Player => new Color(0.2f, 0.6f, 1f, 0.85f),
                CellOwner.Ally => new Color(0.2f, 0.9f, 0.6f, 0.85f),
                CellOwner.Enemy => new Color(1f, 0.25f, 0.25f, 0.85f),
                _ => new Color(0.95f, 0.85f, 0.2f, 0.85f)
            };
        }

        protected void DrawHexOutlineGizmo(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            var corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                // Pointy-top angles: 30, 90, 150, 210, 270, 330 degrees
                float rad = (60f * i + 30f) * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * radius * HexTilemapBridge.PointyTopAspectRatio;
                float z = Mathf.Sin(rad) * radius;
                corners[i] = center + new Vector3(x, 0.02f, z);
            }

            for (int i = 0; i < 6; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
            }
        }

        protected void DrawFacingArrowGizmo(Vector3 center, Quaternion rotation, float length, Color color)
        {
            Gizmos.color = color;
            Vector3 forward = rotation * Vector3.forward * length;
            Vector3 tip = center + forward + Vector3.up * 0.05f;
            Vector3 basePos = center + Vector3.up * 0.05f;

            Gizmos.DrawLine(basePos, tip);
            // Arrowhead
            Vector3 right = rotation * Vector3.right * (length * 0.25f);
            Vector3 back = -forward * 0.25f;
            Gizmos.DrawLine(tip, tip + back + right);
            Gizmos.DrawLine(tip, tip + back - right);
        }
    }
}
