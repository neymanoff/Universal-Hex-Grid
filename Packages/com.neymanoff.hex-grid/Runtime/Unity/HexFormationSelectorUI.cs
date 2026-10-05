using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// UI menu / sidebar presenting available squad formation options to the player.
    /// Allows selecting formations before battle, updating a linked <see cref="HexFormationPreviewUI"/> in real time.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/UI/Hex Formation Selector UI")]
    public class HexFormationSelectorUI : MonoBehaviour
    {
        [Header("Formation Database")]
        [Tooltip("List of formation patterns available for player selection.")]
        [SerializeField] private List<FormationPatternSO> _formations = new();

        [Header("UI Hierarchy")]
        [Tooltip("Container (e.g. VerticalLayoutGroup) where option buttons are instantiated.")]
        [SerializeField] private Transform _buttonContainer;

        [Tooltip("Prefab for a formation selection button (must have Button and TextMeshProUGUI child).")]
        [SerializeField] private GameObject _buttonPrefab;

        [Tooltip("Optional linked preview component to update immediately upon selection.")]
        [SerializeField] private HexFormationPreviewUI _linkedPreview;

        [Header("Behavior")]
        [Tooltip("Automatically selects the first formation on Start.")]
        [SerializeField] private bool _selectFirstOnStart = true;

        [Header("Events")]
        [Tooltip("Fired when the player selects a formation.")]
        [SerializeField] private UnityEvent<FormationPatternSO> _onFormationChanged = new();

        public event Action<FormationPatternSO> OnFormationSelected;
        public UnityEvent<FormationPatternSO> OnFormationChanged => _onFormationChanged;

        public IReadOnlyList<FormationPatternSO> Formations => _formations;
        public FormationPatternSO SelectedFormation { get; private set; }
        public int SelectedIndex { get; private set; } = -1;

        public HexFormationPreviewUI LinkedPreview
        {
            get => _linkedPreview;
            set => _linkedPreview = value;
        }

        public Transform ButtonContainer
        {
            get => _buttonContainer;
            set => _buttonContainer = value;
        }

        public GameObject ButtonPrefab
        {
            get => _buttonPrefab;
            set => _buttonPrefab = value;
        }

        private readonly List<Button> _spawnedButtons = new();

        private void Start()
        {
            BuildUI();

            if (_selectFirstOnStart && _formations.Count > 0 && SelectedIndex < 0)
            {
                SelectFormation(0);
            }
        }

        /// <summary>
        /// Populates the UI container with buttons for all configured formations.
        /// </summary>
        public void BuildUI()
        {
            ClearButtons();

            if (_buttonContainer == null || _buttonPrefab == null)
            {
                return;
            }

            for (int i = 0; i < _formations.Count; i++)
            {
                var formation = _formations[i];
                if (formation == null) continue;

                int index = i;
                var buttonInstance = Instantiate(_buttonPrefab, _buttonContainer);
                var btn = buttonInstance.GetComponent<Button>();

                var label = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = !string.IsNullOrEmpty(formation.FormationName)
                        ? formation.FormationName
                        : $"Formation {index + 1} ({formation.SlotCount} units)";
                }

                if (btn != null)
                {
                    btn.onClick.AddListener(() => SelectFormation(index));
                    _spawnedButtons.Add(btn);
                }
            }
        }

        /// <summary>
        /// Clears all instantiated formation buttons.
        /// </summary>
        public void ClearButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_spawnedButtons[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_spawnedButtons[i].gameObject);
                    }
                }
            }
            _spawnedButtons.Clear();
        }

        /// <summary>
        /// Selects the formation at the specified index, updating the linked preview and emitting events.
        /// </summary>
        public void SelectFormation(int index)
        {
            if (index < 0 || index >= _formations.Count) return;

            SelectedIndex = index;
            SelectedFormation = _formations[index];

            if (_linkedPreview != null && SelectedFormation != null)
            {
                _linkedPreview.SetFormation(SelectedFormation);
            }

            OnFormationSelected?.Invoke(SelectedFormation);
            _onFormationChanged?.Invoke(SelectedFormation);
        }

        /// <summary>
        /// Selects the formation by reference.
        /// </summary>
        public void SelectFormation(FormationPatternSO formation)
        {
            int idx = _formations.IndexOf(formation);
            if (idx >= 0)
            {
                SelectFormation(idx);
            }
            else
            {
                SelectedFormation = formation;
                if (_linkedPreview != null && formation != null)
                {
                    _linkedPreview.SetFormation(formation);
                }
                OnFormationSelected?.Invoke(formation);
                _onFormationChanged?.Invoke(formation);
            }
        }

        /// <summary>
        /// Replaces the list of available formations and rebuilds the UI.
        /// </summary>
        public void SetFormations(IEnumerable<FormationPatternSO> newFormations, bool selectFirst = true)
        {
            _formations.Clear();
            if (newFormations != null)
            {
                _formations.AddRange(newFormations);
            }
            BuildUI();

            if (selectFirst && _formations.Count > 0)
            {
                SelectFormation(0);
            }
        }
    }
}
