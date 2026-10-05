using System;
using Neymanoff.HexGrid.Core;
using UnityEngine;

namespace Neymanoff.HexGrid.Unity
{
    /// <summary>
    /// Component attached to any unit or interactive entity occupying a cell on the hexagonal grid.
    /// Synchronizes spatial presence with the headless <see cref="HexOccupancyMap{TOccupant}"/> domain.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hex Grid/Grid Occupant")]
    public class GridOccupant : MonoBehaviour
    {
        [Tooltip("The faction / ownership of this occupant.")]
        [SerializeField] private CellOwner _owner = CellOwner.Player;

        private HexOccupancyMap<GridOccupant> _occupancyMap;

        /// <summary>
        /// Current axial grid coordinate occupied by this entity.
        /// </summary>
        public HexCoord CurrentCoord { get; private set; }

        /// <summary>
        /// Faction ownership of this entity.
        /// </summary>
        public CellOwner Owner
        {
            get => _owner;
            set => _owner = value;
        }

        /// <summary>
        /// Fired whenever this occupant moves to a new hex coordinate.
        /// </summary>
        public event Action<HexCoord, HexCoord> OnCoordChanged;

        /// <summary>
        /// Binds this occupant to a specific coordinate and occupancy map.
        /// </summary>
        public bool Bind(HexCoord coord, CellOwner owner, HexOccupancyMap<GridOccupant> map)
        {
            if (_occupancyMap != null && _occupancyMap.IsOccupied(CurrentCoord))
            {
                _occupancyMap.Vacate(this);
            }

            _owner = owner;
            _occupancyMap = map;

            if (_occupancyMap != null)
            {
                if (!_occupancyMap.TryOccupy(coord, this))
                {
                    Debug.LogWarning($"[GridOccupant] Failed to occupy {coord}: cell already occupied.", this);
                    return false;
                }
            }

            var oldCoord = CurrentCoord;
            CurrentCoord = coord;
            OnCoordChanged?.Invoke(oldCoord, coord);
            return true;
        }

        /// <summary>
        /// Updates the occupied coordinate when the entity moves across the grid.
        /// </summary>
        public bool MoveTo(HexCoord newCoord)
        {
            if (newCoord == CurrentCoord) return true;

            if (_occupancyMap != null)
            {
                if (!_occupancyMap.TryOccupy(newCoord, this))
                {
                    return false;
                }
            }

            var oldCoord = CurrentCoord;
            CurrentCoord = newCoord;
            OnCoordChanged?.Invoke(oldCoord, newCoord);
            return true;
        }

        /// <summary>
        /// Vacates this entity from the grid occupancy map.
        /// </summary>
        public void Vacate()
        {
            if (_occupancyMap != null)
            {
                _occupancyMap.Vacate(this);
            }
        }

        private void OnDestroy()
        {
            Vacate();
        }
    }
}
