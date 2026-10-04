# Universal Hex Grid — Architecture & Technical Specification

> **Package ID:** `com.neymanoff.hex-grid`  
> **Target Framework:** .NET Standard 2.1 / Unity 6000.5+  
> **Status:** Specification revised (Tilemap & Inspector-First Foundation)

---

## 1. Architectural Philosophy: Tilemap Foundation & Clean Separation

The **Universal Hex Grid** module is designed around a strict separation between **pure mathematical domain logic** and **Unity's native `Grid` / `Tilemap` presentation systems**.

We do **not** reinvent visual grid generation, per-cell GameObject instantiations, custom mesh builders, or manual coordinate-to-world math. Unity's native `Grid` and `Tilemap` components already provide high-performance, batched, hardware-accelerated 2D/3D tile rendering and visual authoring in the Unity Editor via the Tile Palette.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        UNIVERSAL HEX GRID                              │
│                                                                        │
│  [ LAYER 1: HEADLESS DOMAIN CORE (Pure C# / .NET Standard 2.1) ]       │
│    Assembly: Neymanoff.HexGrid.Core (noEngineReferences: true)         │
│    • HexCoord (Pointy-top axial q, r + cubic x, y, z math)             │
│    • Odd-R offset converters matching Unity Hexagon Point Top          │
│    • O(1) distance, neighbor indexing, ray/beam geometry               │
│    • Symmetrical targeting shapes (Line, Cone 120°, Area, Ring)        │
│    • HexPathfinder (A* shortest path + reachability approach)          │
│    • HexFloodFill (Reachable movement budget zone / Dijkstra)          │
│    • Pure spatial occupancy mapping (Cell -> OccupantId)               │
│    • Zero dependencies on UnityEngine.dll                              │
│                                                                        │
│  [ LAYER 2: UNITY TILEMAP ADAPTER (UPM Presentation Layer) ]           │
│    Assembly: Neymanoff.HexGrid.Unity (References Core + Tilemaps)      │
│    • HexTilemapBridge: Extracts snapshot from painted Tilemaps         │
│    • World ↔ Cell delegation via Unity Grid.WorldToCell / CellToWorld  │
│    • TilemapHighlightOverlay: Overlay Tilemap for paths & AOE previews │
│    • TilemapPointerPicker: Screen raycast to hex cell coordinate       │
│    • GridMover: Smooth interpolation along hex paths                   │
│                                                                        │
│  [ LAYER 3: GREYBOX DEMO & SAMPLES (Samples~/Demo) ]                   │
│    Assembly: Neymanoff.HexGrid.Demo                                    │
│    • Pre-painted Tilemaps (walkable ground, blockers, hazard tiles)    │
│    • Inspector-configured unit primitives (Cube mover)                 │
│    • Interactive pathfinding and targeting showcase                    │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Core Domain Contracts (Pure C#)

The Core domain lives in `Runtime/Core/` and has **zero engine references**.

### 2.1. Coordinate System (`HexCoord`)
The coordinate system uses **Pointy-Top** hexagonal layout with axial coordinates `(q, r)` and implicit cubic coordinate `s = -q - r`:

```csharp
namespace Neymanoff.HexGrid.Core
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;
        public int S => -Q - R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        // O(1) distance calculation
        public int DistanceTo(HexCoord other);

        // 6 axial neighbors (Pointy-Top: +1,0; +1,-1; 0,-1; -1,0; -1,+1; 0,+1)
        public HexCoord Neighbor(int directionIndex);
        public HexCoord Neighbor(HexDirection direction);

        // Exact conversion to/from Unity's Hexagon Point Top (Odd-R offset)
        public static HexCoord FromOddR(int col, int row)
        {
            int q = col - ((row - (row & 1)) / 2);
            int r = row;
            return new HexCoord(q, r);
        }

        public (int col, int row) ToOddR()
        {
            int col = Q + ((R - (R & 1)) / 2);
            int row = R;
            return (col, row);
        }
    }
}
```

### 2.2. Traversal Rules & Map Abstraction
Pathfinding does not care about game-specific RPG stats or factions. It queries a generic traversal provider:

```csharp
namespace Neymanoff.HexGrid.Core
{
    public interface ITraversalRule
    {
        /// <summary>Checks if a cell is structurally walkable and passable by the traveler.</summary>
        bool CanEnter(HexCoord coord, object traveler = null);

        /// <summary>Movement cost to enter target cell (must be >= 1 for admissible A* heuristic).</summary>
        int GetMovementCost(HexCoord from, HexCoord to, object traveler = null);
    }
}
```

### 2.3. Pathfinding Engine (`HexPathfinder`)
Pure A* search operating on `HexCoord`:

```csharp
namespace Neymanoff.HexGrid.Core
{
    public static class HexPathfinder
    {
        /// <summary>
        /// Finds the shortest path from start to goal.
        /// If goal is blocked/occupied and stopAdjacentIfBlocked is true, finds path to the nearest reachable neighbor.
        /// </summary>
        public static bool TryFindPath(
            HexCoord start,
            HexCoord goal,
            ITraversalRule rule,
            out List<HexCoord> path,
            bool stopAdjacentIfBlocked = true,
            int maxBudget = int.MaxValue,
            object traveler = null);
    }
}
```

### 2.4. Movement Range & Flood Fill (`HexFloodFill`)
Calculates reachable movement zone given a movement point budget (Dijkstra):

```csharp
namespace Neymanoff.HexGrid.Core
{
    public static class HexFloodFill
    {
        /// <summary>Returns all coordinates reachable from origin within movement budget.</summary>
        public static Dictionary<HexCoord, int> GetReachableZone(
            HexCoord origin,
            int movementBudget,
            ITraversalRule rule,
            object traveler = null);
    }
}
```

### 2.5. Spatial Targeting Geometry (`HexTargetResolver`)
Calculates deterministic geometric shapes without physics raycasts:

```csharp
namespace Neymanoff.HexGrid.Core
{
    public enum TargetShape
    {
        SingleCell, // Single target cell within range
        Line,       // Straight axis-aligned beam in one of 6 directions
        Cone,       // Symmetrical 120° cone spreading outward from origin
        Area,       // Hexagonal blast radius around target center
        Ring        // 1-cell thick perimeter at exact distance N
    }

    public static class HexTargetResolver
    {
        public static List<HexCoord> Resolve(
            HexCoord origin,
            HexCoord targetOrFacing,
            TargetShape shape,
            int range,
            int radius = 0);
    }
}
```

---

## 3. Unity Presentation Layer (`Runtime/Unity/`)

The presentation layer sits on top of Unity's built-in **`UnityEngine.Grid`** and **`UnityEngine.Tilemaps`**.

### 3.1. `HexTilemapBridge`
* References the scene's `Grid` component and walkable/obstacle `Tilemap` layers.
* Converts Unity's `Vector3Int` cell positions to `HexCoord` via `HexCoord.FromOddR(cell.x, cell.y)`.
* Provides world-to-cell and cell-to-world conversion by delegating directly to `Grid.WorldToCell` and `Grid.GetCellCenterWorld`.
* Builds a clean in-memory map snapshot or queries the painted tilemaps on demand.

### 3.2. `TilemapHighlightOverlay`
* Replaces procedurally generated outline sprites and per-cell GameObjects.
* Uses an overlay `Tilemap` configured in the Inspector with highlight tiles (Path Yellow, Hover Blue, AOE Target Red).
* Setting highlights is as simple as painting tiles onto the overlay Tilemap, rendered in a single batched draw call.

### 3.3. `TilemapPointerPicker`
* Uses Unity 6 Input System (`Pointer.current.position.ReadValue()`).
* Performs a raycast against the grid plane (`Plane` at grid Z/Y).
* Converts world intersection point to cell via `grid.WorldToCell(hitPoint)`.

### 3.4. `GridMover`
* Clean MonoBehaviour component attached to moving units (e.g., greybox Cube).
* Receives a `List<HexCoord>`, converts waypoints to world coordinates via `grid.GetCellCenterWorld`, and moves smoothly with customizable speed, rotation, and arrival threshold.

---

## 4. Inspector-First Architecture Checklist

To adhere to the workspace agreements and eliminate runtime procedural bloat:

| Feature | Anti-Pattern (Forbidden) | Clean Unity Standard (Required) |
| :--- | :--- | :--- |
| **Grid Generation** | Spawning hundreds of Cylinder GameObjects in `Awake` | Level designer paints cells in Unity Tile Palette onto a Hexagonal Tilemap |
| **Highlighting** | Procedural Texture2D generation with outline pixel shaders | Overlay Tilemap with designer-assigned highlight Tile assets |
| **Cell Layout** | Custom trigonometric formulas for cell world positions | Unity `Grid` component handles scale, cell size, and odd-r row staggering |
| **Demo Setup** | 300 lines of procedural C# code constructing the demo scene | Fully authored `.unity` scene with pre-configured Grid, Tilemaps, and prefabs |
| **Configuration** | Hardcoded speeds, ranges, colors, and layers | `[SerializeField]` fields with tooltips and clean Inspector defaults |

---

## 5. Module Boundaries & Consumers

This module is strictly a **spatial foundation**. It does not make gameplay decisions:

* **Universal-Hex-Grid**: "Here is the path between cell A and cell B", "Here are the cells inside this 120° cone".
* **Universal-Turn-Combat**: "Can this unit move right now? Does it have enough Action Points? Did it step on a trap?"
* **Universal-Skill-Constructor**: "Does this skill require line of sight? Are targets in these cells allies or enemies?"
* **Universal-RPG-Roster**: "What are the unit's stats and base movement budget?"
