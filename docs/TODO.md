# Universal Hex Grid — Implementation Roadmap & Task Tracker (docs/TODO.md)

> This document tracks the implementation milestones for the `com.neymanoff.hex-grid` package.
> Governed by `MODULAR_DEVELOPMENT_AGREEMENTS.md` and `AGENTS.md`.

---

## 🚀 Active Milestones

| Phase | Milestone | Priority | Status |
| :--- | :--- | :---: | :---: |
| **Phase 0** | UPM Package Skeleton & Assembly Definitions | 🔴 Critical | ✅ Done |
| **Phase 1** | Pure C# Hexagonal Coordinate Math (`HexCoord`) | 🔴 Critical | ✅ Done |
| **Phase 2** | Pure C# Pathfinding & Reachable Zone (`HexPathfinder`, `HexFloodFill`) | 🔴 Critical | ✅ Done |
| **Phase 3** | Geometric Targeting & AOE Resolver (`HexTargetResolver`) | 🟡 High | ✅ Done |
| **Phase 4** | Unity Tilemap Presentation Layer (`HexTilemapBridge`, Overlay) | 🟡 High | ✅ Done |
| **Phase 5** | Inspector-Authored Greybox Demo Scene (`Samples~/Demo`) | 🔵 High | ✅ Done |
| **Phase 6** | Comprehensive NUnit Automated Tests & Package Export | 🟢 Medium | ✅ Done |

---

## Phase 0: UPM Package Skeleton & AsmDefs
- [x] **0.1. Directory Structure**:
  - [x] `Packages/com.neymanoff.hex-grid/Runtime/Core/` (Pure C# domain).
  - [x] `Packages/com.neymanoff.hex-grid/Runtime/Unity/` (Tilemap presentation adapter).
  - [x] `Packages/com.neymanoff.hex-grid/Editor/` (Optional Editor helpers).
  - [x] `Packages/com.neymanoff.hex-grid/Tests/Core/` (Pure C# NUnit tests).
  - [x] `Packages/com.neymanoff.hex-grid/Tests/Unity/` (Unity integration tests).
  - [x] `Packages/com.neymanoff.hex-grid/Samples~/Demo/` (Greybox demo scene & assets).
- [x] **0.2. Assembly Definitions**:
  - [x] `Neymanoff.HexGrid.Core.asmdef` (`noEngineReferences: true`, pure .NET Standard 2.1).
  - [x] `Neymanoff.HexGrid.Unity.asmdef` (references `Neymanoff.HexGrid.Core`, `Unity.InputSystem`).
  - [x] `Neymanoff.HexGrid.Core.Tests.asmdef` (tests for pure math/pathfinding).
  - [x] `Neymanoff.HexGrid.Unity.Tests.asmdef` (tests for Tilemap bridge).
  - [x] `Neymanoff.HexGrid.Demo.asmdef` (demo interaction scripts).

---

## Phase 1: Pure C# Hexagonal Coordinate Math
- [x] **1.1. `HexCoord` Immutable Struct**:
  - [x] Axial coordinates `(int Q, int R)` and cubic `int S => -Q - R`.
  - [x] Arithmetic operators `+`, `-`, `*`, `==`, `!=`.
  - [x] Directional neighbor methods using `HexDirection` enum.
  - [x] Exact O(1) distance calculation.
  - [x] Exact roundtrip conversions for Unity's Hexagon Point Top (Odd-R offset).
- [x] **1.2. Core Unit Tests**:
  - [x] Distance between arbitrary hexes.
  - [x] Roundtrip conversion between Axial and Odd-R coordinates.
  - [x] Neighbor direction indexing correctness.

---

## Phase 2: Pure C# Pathfinding & Reachable Zone
- [x] **2.1. Traversal & Occupancy Contracts**:
  - [x] `ITraversalRule` interface for passability and movement cost.
  - [x] `HexOccupancyMap` for tracking spatial occupant locations.
- [x] **2.2. `HexPathfinder` (A* Algorithm)**:
  - [x] Min-Heap / PriorityQueue implementation for pure C#.
  - [x] Shortest path calculation with cost matrices.
  - [x] Support `stopAdjacentIfBlocked` (approach mode for melee/interaction with occupied cells).
  - [x] Traversal limit / movement range cutoff and closed-set optimization.
- [x] **2.3. `HexFloodFill` (Reachable Zone)**:
  - [x] Dijkstra flood-fill to get all reachable coordinates within a movement budget.
- [x] **2.4. Pathfinding Unit Tests**:
  - [x] Direct path on unobstructed map.
  - [x] Obstacle avoidance routing.
  - [x] Reachable zone within budget limit.
  - [x] Adjacent stop behavior when target cell is blocked.

---

## Phase 3: Spatial Targeting & AOE Resolver
- [x] **3.1. `HexTargetResolver` Shapes**:
  - [x] `TargetShape.SingleCell`: Validates range and direction.
  - [x] `TargetShape.Line`: Axis-aligned straight beam along 1 of 6 directions.
  - [x] `TargetShape.Cone`: Symmetrical 120° cone spreading outward from origin.
  - [x] `TargetShape.Area`: Hexagonal blast radius around target center.
  - [x] `TargetShape.Ring`: 1-cell perimeter ring at exact distance N.
- [x] **3.2. Targeting Geometry Tests**:
  - [x] Symmetrical cell counts and coordinate coverage for all shapes.

---

## Phase 4: Unity Tilemap Presentation Adapter
- [x] **4.1. `HexTilemapBridge`**:
  - [x] Binds to scene's Unity `Grid` and `Tilemap` components.
  - [x] Converts `Vector3Int` to `HexCoord` and vice versa.
  - [x] Delegates world position conversions to Unity's native `Grid.CellToWorld` / `Grid.WorldToCell`.
- [x] **4.2. `TilemapHighlightOverlay`**:
  - [x] Renders path, reachable zone, and AOE target highlights on an overlay Tilemap.
  - [x] Configurable highlight tiles and colors set via Inspector.
- [x] **4.3. `TilemapPointerPicker`**:
  - [x] Reads pointer position via Unity 6 Input System and raycasts to grid cell.
- [x] **4.4. `GridMover`**:
  - [x] MonoBehaviour component for smooth movement interpolation along hex waypoints.

---

## Phase 5: Inspector-Authored Greybox Demo Scene
- [x] **5.1. Authored Scene & Tilemaps**:
  - [x] `HexGridMenuCommands` 1-click hierarchy generator (`GameObject -> Hex Grid -> Create Tactical Grid Setup`).
  - [x] Designer-friendly setup in the Unity Inspector without runtime procedural instantiation.
- [x] **5.2. Interactive Unit & Controller**:
  - [x] `HexDemoController` coordinating greybox hero movement and targeting projections.
  - [x] Number keys 1..6 switch targeting shapes (Line, Cone, Blast, Ring) with real-time overlay previews.

---

## Phase 6: Automated Tests & Polish
- [x] **6.1. Full Test Runner Validation**:
  - [x] 51 NUnit EditMode tests passing in Unity Test Framework (100% pass rate).
  - [x] Zero warnings with `<WarningsAsErrors>CS0618</WarningsAsErrors>`.
- [x] **6.2. Documentation & Package Validation**:
  - [x] Full XML documentation on public APIs.
  - [x] Created `CHANGELOG.md` for package release v1.0.0.
