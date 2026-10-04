using System;
using System.Collections;
using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Smoothly moves a GameObject along a sequence of hex waypoints.
    /// Provides speed control, waypoint reached events, and completion callbacks.
    /// Uses modern Unity 6 FindAnyObjectByType fallback without obsolete APIs.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Grid Mover")]
    public class GridMover : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("HexTilemapBridge used for translating HexCoords into world positions.")]
        [SerializeField] private HexTilemapBridge _bridge;

        [Header("Movement Settings")]
        [Tooltip("Linear movement speed in world units per second.")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Whether to rotate towards the next waypoint during movement.")]
        [SerializeField] private bool _rotateTowardsMovement = false;

        [Tooltip("Rotation speed in degrees per second if rotation is enabled.")]
        [SerializeField] private float _rotationSpeed = 720f;

        [Tooltip("Threshold distance to consider a waypoint reached.")]
        [SerializeField] private float _arrivalThreshold = 0.01f;

        /// <summary>Indicates whether the entity is currently traversing a path.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>The current hex coordinate occupied by the entity.</summary>
        public HexCoord CurrentCoord { get; private set; }

        /// <summary>Fired whenever the entity reaches an intermediate hex waypoint.</summary>
        public event Action<HexCoord> OnWaypointReached;

        /// <summary>Fired when the entire path traversal has completed.</summary>
        public event Action OnMovementCompleted;

        private Coroutine _moveCoroutine;

        /// <summary>
        /// Explicitly wires the bridge dependency without requiring reflection.
        /// </summary>
        public void Configure(HexTilemapBridge bridge)
        {
            _bridge = bridge;
        }

        private void Awake()
        {
            if (_bridge == null)
                _bridge = FindAnyObjectByType<HexTilemapBridge>();
        }

        private void Start()
        {
            if (_bridge != null)
                CurrentCoord = _bridge.WorldToHex(transform.position);
        }

        /// <summary>
        /// Snaps the object immediately to the center of the specified hex coordinate (elevated by unit height offset).
        /// </summary>
        public void TeleportTo(HexCoord coord)
        {
            StopMovement();
            CurrentCoord = coord;
            if (_bridge != null)
                transform.position = _bridge.HexToUnitWorld(coord);
        }

        /// <summary>
        /// Initiates movement along the specified list of hex coordinates.
        /// </summary>
        /// <param name="path">Waypoints to traverse in order.</param>
        /// <param name="onComplete">Optional completion callback.</param>
        public void FollowPath(IReadOnlyList<HexCoord> path, Action onComplete = null)
        {
            if (path == null || path.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            StopMovement();
            _moveCoroutine = StartCoroutine(MoveRoutine(path, onComplete));
        }

        /// <summary>
        /// Stops any ongoing path traversal.
        /// </summary>
        public void StopMovement()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            IsMoving = false;
        }

        private IEnumerator MoveRoutine(IReadOnlyList<HexCoord> path, Action onComplete)
        {
            IsMoving = true;

            int startIndex = 0;
            // If the first waypoint is the current position, skip it
            if (path.Count > 0 && path[0] == CurrentCoord)
                startIndex = 1;

            for (int i = startIndex; i < path.Count; i++)
            {
                var targetCoord = path[i];
                Vector3 targetWorld = _bridge != null
                    ? _bridge.HexToUnitWorld(targetCoord)
                    : transform.position;

                while (Vector3.Distance(transform.position, targetWorld) > _arrivalThreshold)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetWorld,
                        _moveSpeed * Time.deltaTime);

                    if (_rotateTowardsMovement)
                    {
                        Vector3 dir = (targetWorld - transform.position).normalized;
                        if (dir != Vector3.zero)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(dir);
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                targetRot,
                                _rotationSpeed * Time.deltaTime);
                        }
                    }

                    yield return null;
                }

                transform.position = targetWorld;
                CurrentCoord = targetCoord;
                OnWaypointReached?.Invoke(targetCoord);
            }

            IsMoving = false;
            _moveCoroutine = null;
            OnMovementCompleted?.Invoke();
            onComplete?.Invoke();
        }
    }
}
