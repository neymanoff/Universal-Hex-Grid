using System;
using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// <summary>
    /// Renders a UI preview grid (UI slots inside a RectTransform container) for squad formations.
    /// Used in battle preparation screens, hero party deployment, and opponent squad previews.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Hex Grid/UI/Hex Formation Preview UI")]
    public class HexFormationPreviewUI : MonoBehaviour
    {
        [Header("UI Hierarchy")]
        [Tooltip("The parent container (RectTransform) where slot elements are spawned. Defaults to this transform.")]
        [SerializeField] private RectTransform _container;

        [Tooltip("Prefab representing a single hex slot in UI (must have RectTransform).")]
        [SerializeField] private GameObject _slotPrefab;

        [Header("Visual Dimensions (Pixels)")]
        [Tooltip("Height of each hex slot in UI pixels.")]
        [SerializeField] private float _cellHeightPx = 96f;

        [Tooltip("Spacing gap between hex cell centers in UI pixels.")]
        [SerializeField] private float _cellGapPx = 8f;

        [Header("Layout Options")]
        [Tooltip("Centers the formation around the container origin (0, 0).")]
        [SerializeField] private bool _center = true;

        [Tooltip("Mirrors the formation horizontally (useful for enemy/opponent squads on the right).")]
        [SerializeField] private bool _mirrorX = false;

        [Tooltip("Mirrors the formation vertically.")]
        [SerializeField] private bool _mirrorY = false;

        [Header("Initial Pattern")]
        [Tooltip("Optional initial formation to show automatically on Start.")]
        [SerializeField] private FormationPatternSO _initialPattern;

        [Header("Events")]
        [Tooltip("Invoked after the preview layout is rebuilt with the spawned slot RectTransforms.")]
        [SerializeField] private UnityEvent<IReadOnlyList<RectTransform>> _onLayoutRebuilt;

        private readonly List<RectTransform> _spawnedSlots = new();

        public IReadOnlyList<RectTransform> Slots => _spawnedSlots;
        public int Count => _spawnedSlots.Count;

        public RectTransform Container
        {
            get
            {
                if (_container == null) _container = GetComponent<RectTransform>();
                if (_container == null) _container = gameObject.AddComponent<RectTransform>();
                return _container;
            }
            set => _container = value;
        }

        public GameObject SlotPrefab
        {
            get => _slotPrefab;
            set => _slotPrefab = value;
        }

        public float CellHeightPx
        {
            get => _cellHeightPx;
            set => _cellHeightPx = Mathf.Max(1f, value);
        }

        public float CellGapPx
        {
            get => _cellGapPx;
            set => _cellGapPx = Mathf.Max(0f, value);
        }

        public bool MirrorX
        {
            get => _mirrorX;
            set => _mirrorX = value;
        }

        public bool MirrorY
        {
            get => _mirrorY;
            set => _mirrorY = value;
        }

        public UnityEvent<IReadOnlyList<RectTransform>> OnLayoutRebuilt => _onLayoutRebuilt;

        private void Awake()
        {
            if (_container == null)
            {
                _container = GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            if (_initialPattern != null && _spawnedSlots.Count == 0)
            {
                SetFormation(_initialPattern);
            }
        }

        /// <summary>
        /// Clears currently displayed slots.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_spawnedSlots[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_spawnedSlots[i].gameObject);
                    }
                }
            }
            _spawnedSlots.Clear();
        }

        /// <summary>
        /// Builds and displays the given formation pattern in UI.
        /// </summary>
        public void SetFormation(FormationPatternSO pattern)
        {
            if (pattern == null)
            {
                Clear();
                return;
            }
            SetFormation(pattern.GetRelativeSlots());
        }

        /// <summary>
        /// Builds and displays the given hex coordinates in UI.
        /// </summary>
        public void SetFormation(IReadOnlyList<HexCoord> coords)
        {
            Clear();
            if (coords == null || coords.Count == 0) return;

            var targetContainer = Container;
            if (targetContainer == null || _slotPrefab == null)
            {
                Debug.LogWarning("[HexFormationPreviewUI] Missing container or slotPrefab.", this);
                return;
            }

            var layout = HexUiLayoutConverter.Build(
                coords,
                _cellHeightPx,
                _cellGapPx,
                _center,
                _mirrorX,
                _mirrorY);

            for (int i = 0; i < layout.Slots.Count; i++)
            {
                var slotData = layout.Slots[i];
                var instance = Instantiate(_slotPrefab, targetContainer);
                var rt = instance.GetComponent<RectTransform>();

                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(layout.CellWidthPx, layout.CellHeightPx);
                    rt.anchoredPosition = slotData.AnchoredPositionPx;
                    _spawnedSlots.Add(rt);
                }
            }

            _onLayoutRebuilt?.Invoke(_spawnedSlots);
        }

        /// <summary>
        /// Returns the RectTransform of the slot at the specified index.
        /// </summary>
        public RectTransform GetSlot(int index)
        {
            return (index >= 0 && index < _spawnedSlots.Count) ? _spawnedSlots[index] : null;
        }
    }
}
