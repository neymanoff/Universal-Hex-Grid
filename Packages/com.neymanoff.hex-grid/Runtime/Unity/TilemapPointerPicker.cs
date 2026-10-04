using System;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Specifies the 3D plane orientation on which the hexagonal grid lies.
    /// </summary>
    public enum GridPlaneOrientation
    {
        /// <summary>Flat 2D grid in XY plane (Z is constant depth).</summary>
        XY = 0,

        /// <summary>Top-down / Isometric 3D grid in XZ plane (Y is height).</summary>
        XZ = 1
    }

    /// <summary>
    /// Captures pointer interactions using the Unity 6 Input System and translates screen positions
    /// to <see cref="HexCoord"/> coordinates via plane raycasts.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Tilemap Pointer Picker")]
    public class TilemapPointerPicker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The HexTilemapBridge component used for coordinate transformation.")]
        [SerializeField] private HexTilemapBridge _bridge;

        [Tooltip("The Camera to raycast from (defaults to Camera.main if null).")]
        [SerializeField] private Camera _targetCamera;

        [Header("Plane Configuration")]
        [Tooltip("Orientation of the grid plane in world space (XY for 2D, XZ for 3D).")]
        [SerializeField] private GridPlaneOrientation _planeOrientation = GridPlaneOrientation.XY;

        [Tooltip("Offset position along the plane normal (e.g. Z for XY, Y for XZ).")]
        [SerializeField] private float _planeOffset = 0f;

        /// <summary>Fired when the pointer hovers over a different hex cell.</summary>
        public event Action<HexCoord> OnCellHovered;

        /// <summary>Fired when the pointer is clicked on a hex cell.</summary>
        public event Action<HexCoord> OnCellClicked;

        private HexCoord _lastHoveredCoord;
        private bool _hasHoveredCell;

        private void Awake()
        {
            if (_bridge == null) _bridge = GetComponent<HexTilemapBridge>();
            if (_targetCamera == null) _targetCamera = Camera.main;
        }

        private void Update()
        {
            if (TryGetPointerHexCoord(out var currentHex))
            {
                if (!_hasHoveredCell || currentHex != _lastHoveredCoord)
                {
                    _lastHoveredCoord = currentHex;
                    _hasHoveredCell = true;
                    OnCellHovered?.Invoke(currentHex);
                }

                // Check click via modern Input System Pointer
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                {
                    OnCellClicked?.Invoke(currentHex);
                }
            }
        }

        /// <summary>
        /// Attempts to raycast the current pointer position onto the grid plane and return the resulting <see cref="HexCoord"/>.
        /// </summary>
        public bool TryGetPointerHexCoord(out HexCoord coord)
        {
            coord = HexCoord.Zero;
            if (_bridge == null) return false;

            var cam = _targetCamera != null ? _targetCamera : Camera.main;
            if (cam == null) return false;

            var pointer = Pointer.current;
            if (pointer == null) return false;

            Vector2 pointerPos = pointer.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(pointerPos);

            Plane plane = _planeOrientation == GridPlaneOrientation.XY
                ? new Plane(Vector3.forward, new Vector3(0, 0, _planeOffset))
                : new Plane(Vector3.up, new Vector3(0, _planeOffset, 0));

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 worldHit = ray.GetPoint(enter);
                coord = _bridge.WorldToHex(worldHit);
                return true;
            }

            return false;
        }
    }
}
