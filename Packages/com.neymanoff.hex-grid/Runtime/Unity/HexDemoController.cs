using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Interaction modes available in the greybox demo.
    /// </summary>
    public enum DemoInteractionMode
    {
        Move = 0,
        TargetSingle = 1,
        TargetLine = 2,
        TargetCone = 3,
        TargetArea = 4,
        TargetRing = 5
    }

    /// <summary>
    /// Coordinates the greybox demo scene: handles user input, calculates pathfinding
    /// and spatial targeting previews, and commands unit movement.
    /// Fully configurable via the Unity Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Hex Demo Controller")]
    public class HexDemoController : MonoBehaviour
    {
        [Header("Scene References (Inspector-Wired)")]
        [Tooltip("The bridge component connecting the tilemap layers.")]
        [SerializeField] private HexTilemapBridge _bridge;

        [Tooltip("The overlay tilemap for drawing highlights.")]
        [SerializeField] private TilemapHighlightOverlay _overlay;

        [Tooltip("The pointer picker translating mouse/touch to hex coordinates.")]
        [SerializeField] private TilemapPointerPicker _pointerPicker;

        [Tooltip("The moving unit / greybox cube in the scene.")]
        [SerializeField] private GridMover _unit;

        [Header("Demo Settings")]
        [Tooltip("Active interaction mode (Move or various Targeting shapes).")]
        [SerializeField] private DemoInteractionMode _mode = DemoInteractionMode.Move;

        [Tooltip("Maximum movement range / budget for the unit.")]
        [SerializeField] private int _movementBudget = 4;

        [Tooltip("Range for targeting shapes.")]
        [SerializeField] private int _targetingRange = 3;

        [Tooltip("Radius for Area and Ring targeting shapes.")]
        [SerializeField] private int _targetingRadius = 1;

        private HexCoord _currentHoveredCoord;
        private List<HexCoord> _currentPath;
        private Dictionary<HexCoord, int> _reachableZone;

        public DemoInteractionMode CurrentMode => _mode;

        private void OnEnable()
        {
            if (_pointerPicker != null)
            {
                _pointerPicker.OnCellHovered += HandleCellHovered;
                _pointerPicker.OnCellClicked += HandleCellClicked;
            }

            if (_unit != null)
            {
                _unit.OnMovementCompleted += RefreshReachableZone;
            }
        }

        private void OnDisable()
        {
            if (_pointerPicker != null)
            {
                _pointerPicker.OnCellHovered -= HandleCellHovered;
                _pointerPicker.OnCellClicked -= HandleCellClicked;
            }

            if (_unit != null)
            {
                _unit.OnMovementCompleted -= RefreshReachableZone;
            }
        }

        private void Start()
        {
            RefreshReachableZone();
        }

        private void Update()
        {
            HandleModeSwitchingInput();
        }

        /// <summary>
        /// Allows switching demo modes using number keys 1 to 6.
        /// </summary>
        private void HandleModeSwitchingInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) SetMode(DemoInteractionMode.Move);
            else if (keyboard.digit2Key.wasPressedThisFrame) SetMode(DemoInteractionMode.TargetSingle);
            else if (keyboard.digit3Key.wasPressedThisFrame) SetMode(DemoInteractionMode.TargetLine);
            else if (keyboard.digit4Key.wasPressedThisFrame) SetMode(DemoInteractionMode.TargetCone);
            else if (keyboard.digit5Key.wasPressedThisFrame) SetMode(DemoInteractionMode.TargetArea);
            else if (keyboard.digit6Key.wasPressedThisFrame) SetMode(DemoInteractionMode.TargetRing);
        }

        public void SetMode(DemoInteractionMode newMode)
        {
            if (_mode == newMode) return;
            _mode = newMode;
            RefreshDisplay();
        }

        private void HandleCellHovered(HexCoord hoveredCoord)
        {
            _currentHoveredCoord = hoveredCoord;
            RefreshDisplay();
        }

        private void HandleCellClicked(HexCoord clickedCoord)
        {
            if (_mode == DemoInteractionMode.Move)
            {
                if (_unit != null && !_unit.IsMoving && _currentPath != null && _currentPath.Count > 1)
                {
                    _overlay.ClearAll();
                    _unit.FollowPath(_currentPath);
                }
            }
        }

        private void RefreshReachableZone()
        {
            if (_bridge == null || _unit == null) return;

            _reachableZone = HexFloodFill.GetReachableZone(
                _unit.CurrentCoord,
                _movementBudget,
                _bridge);

            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_overlay == null || _unit == null || _bridge == null) return;

            _overlay.ClearAll();

            if (_mode == DemoInteractionMode.Move)
            {
                // Show reachable movement zone
                if (_reachableZone != null)
                {
                    _overlay.ShowReachableZone(_reachableZone.Keys);
                }

                // If hovering over a valid cell, calculate and display path
                if (_reachableZone != null && _reachableZone.ContainsKey(_currentHoveredCoord))
                {
                    if (HexPathfinder.TryFindPath(_unit.CurrentCoord, _currentHoveredCoord, _bridge, out _currentPath, maxBudget: _movementBudget))
                    {
                        _overlay.ShowPath(_currentPath);
                    }
                }
                else
                {
                    _currentPath = null;
                }

                _overlay.ShowHover(_currentHoveredCoord);
            }
            else
            {
                // Targeting preview mode
                TargetShape shape = _mode switch
                {
                    DemoInteractionMode.TargetSingle => TargetShape.SingleCell,
                    DemoInteractionMode.TargetLine => TargetShape.Line,
                    DemoInteractionMode.TargetCone => TargetShape.Cone,
                    DemoInteractionMode.TargetArea => TargetShape.Area,
                    DemoInteractionMode.TargetRing => TargetShape.Ring,
                    _ => TargetShape.SingleCell
                };

                var targetCells = HexTargetResolver.Resolve(
                    _unit.CurrentCoord,
                    _currentHoveredCoord,
                    shape,
                    range: _targetingRange,
                    radius: _targetingRadius);

                _overlay.ShowTargetShape(targetCells);
                _overlay.ShowHover(_currentHoveredCoord);
            }
        }
    }
}
