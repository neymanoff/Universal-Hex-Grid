# Universal Hex Grid — Implementation Roadmap & Task Tracker (docs/TODO.md)

> This document tracks the implementation milestones for the `com.neymanoff.hex-grid` package.
> Governed by `MODULAR_DEVELOPMENT_AGREEMENTS.md` and `AGENTS.md`.

---

## 🚀 Active Milestones

| Phase | Milestone | Priority | Status |
| :--- | :--- | :---: | :---: |
| **Phase 0** | UPM Package Skeleton & Assembly Definitions | 🔴 Critical | ✅ Done |
| **Phase 1** | Pure C# Hexagonal Coordinate Math (`HexCoord`) | 🔴 Critical | ✅ Done |
| **Phase 2** | Pure C# Pathfinding & Reachable Zone (`HexPathfinder`, `HexFloodFill`) | 🔴 Critical | ⏳ Pending |
| **Phase 3** | Geometric Targeting & AOE Resolver (`HexTargetResolver`) | 🟡 High | ⏳ Pending |
| **Phase 4** | Unity Tilemap Presentation Layer (`HexTilemapBridge`, Overlay) | 🟡 High | ⏳ Pending |
| **Phase 5** | Inspector-Authored Greybox Demo Scene (`Samples~/Demo`) | 🔵 High | ⏳ Pending |
| **Phase 6** | Comprehensive NUnit Automated Tests & Package Export | 🟢 Medium | ⏳ Pending |

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
- [ ] **2.1. Traversal & Occupancy Contracts**:
  - [ ] `ITraversalRule` interface for passability and movement cost.
  - [ ] `HexOccupancyMap` for tracking spatial occupant locations.
- [ ] **2.2. `HexPathfinder` (A* Algorithm)**:
  - [ ] Min-Heap / PriorityQueue implementation for pure C#.
  - [ ] Shortest path calculation with cost matrices.
  - [ ] Support `stopAdjacentIfBlocked` (approach mode for melee/interaction with occupied cells).
  - [ ] Traversal limit / movement range cutoff.
- [ ] **2.3. `HexFloodFill` (Reachable Zone)**:
  - [ ] Dijkstra flood-fill to get all reachable coordinates within a movement budget.
- [ ] **2.4. Pathfinding Unit Tests**:
  - [ ] Direct path on unobstructed map.
  - [ ] Obstacle avoidance routing.
  - [ ] Reachable zone within budget limit.
  - [ ] Adjacent stop behavior when target cell is blocked.

---

## Phase 3: Spatial Targeting & AOE Resolver
- [ ] **3.1. `HexTargetResolver` Shapes**:
  - [ ] `TargetShape.SingleCell`: Validates range and direction.
  - [ ] `TargetShape.Line`: Axis-aligned straight beam along 1 of 6 directions.
  - [ ] `TargetShape.Cone`: Symmetrical 120° cone spreading outward from origin.
  - [ ] `TargetShape.Area`: Hexagonal blast radius around target center.
  - [ ] `TargetShape.Ring`: 1-cell perimeter ring at exact distance N.
- [ ] **3.2. Targeting Geometry Tests**:
  - [ ] Symmetrical cell counts and coordinate coverage for all shapes.

---

## Phase 4: Unity Tilemap Presentation Adapter
- [ ] **4.1. `HexTilemapBridge`**:
  - [ ] Binds to scene's Unity `Grid` and `Tilemap` components.
  - [ ] Converts `Vector3Int` to `HexCoord` and vice versa.
  - [ ] Delegates world position conversions to Unity's native `Grid.CellToWorld` / `Grid.WorldToCell`.
- [ ] **4.2. `TilemapHighlightOverlay`**:
  - [ ] Renders path, reachable zone, and AOE target highlights on an overlay Tilemap.
  - [ ] Configurable highlight tiles and colors set via Inspector.
- [ ] **4.3. `TilemapPointerPicker`**:
  - [ ] Reads pointer position via Unity 6 Input System and raycasts to grid cell.
- [ ] **4.4. `GridMover`**:
  - [ ] MonoBehaviour component for smooth movement interpolation along hex waypoints.

---

## Phase 5: Inspector-Authored Greybox Demo Scene
- [ ] **5.1. Authored Scene & Tilemaps**:
  - [ ] Unity scene with painted ground tiles, obstacle tiles, and overlay highlight tilemap.
  - [ ] Designer-friendly setup in the Unity Inspector.
- [ ] **5.2. Interactive Unit & Controller**:
  - [ ] Greybox Cube moving along calculated paths on click.
  - [ ] Key controls to switch targeting shapes (Line, Cone, Blast) with real-time overlay previews.

---

## Phase 6: Automated Tests & Polish
- [ ] **6.1. Full Test Runner Validation**:
  - [ ] Run all EditMode and PlayMode tests via Unity Test Framework.
  - [ ] Verify zero warnings with `<WarningsAsErrors>CS0618</WarningsAsErrors>`.
- [ ] **6.2. Documentation & Package Validation**:
  - [ ] Ensure full XML documentation on public APIs.
  - [ ] Update `CHANGELOG.md` and package metadata.
