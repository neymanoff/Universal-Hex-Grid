# Task Backlog & Execution Status (TODO.md)

> **Workflow Rule**:
> 1. Tasks in progress are marked `[IN PROGRESS]`.
> 2. When implementation and automated tests pass, agent marks `[IMPLEMENTED]`.
> 3. ONLY after the developer personally tests in Unity Editor PlayMode, task is marked `[VERIFIED]`.
> Agent is strictly prohibited from marking `[VERIFIED]` autonomously.

---

## Current Sprint

- [VERIFIED] **Step 1: Pointy-Top Geometry & 3D Ground Orientation**
  - Pointy-Top aspect ratio: `cellSize = (0.8659766f, 1f, 1f)` ($\sqrt{3}/2 \approx 0.8660254$).
  - 3D horizontal Ground plane (XZ) orientation: `Euler(90, 0, 0)` at `Y = 0.01`.
  - Unit height pivot offset: `HexToUnitWorld` elevates unit by $+0.3$, resting on cell surface.
  - Tactical angled camera ($50^\circ$, height $7.5$).
  - 53/53 EditMode NUnit tests passing. Verified by developer in PlayMode.

- [IMPLEMENTED] **Step 2.1: 3D Spawner Multi-Tilemap Support, Surface Alignment & Zero-Warning Obsolete Fix**
  - **Multi-Tilemap Source List**: Replaced single `_sourceTilemap` with `List<Tilemap> _sourceTilemaps`, auto-wired by default to both `Walkable Tilemap` and `Obstacle Tilemap` from `HexTilemapBridge` (supporting custom additional layers).
  - **Skip Unmapped Cells by Default**: Unmapped tiles without prefabs are strictly skipped (no unwanted 3D models over unmapped ground; 2D underlying artwork and contour outlines remain 100% visible). Optional `_generateProceduralHexForUnmapped` toggle available.
  - **Bottom-to-Surface Alignment (`_alignBottomToSurface`)**: Calculates exact local collider/renderer geometry and offsets spawned instances with `Physics.SyncTransforms()` so object base rests flush on the cell surface ($Y = 0$) rather than sinking at center.
  - **Tilemap Visibility Control**: `_hideTilemapsOnSpawn` defaults to false so 2D terrain artwork stays visible beneath 3D props/trees.
  - **SendMessage & Obsolete Warning Fixes**: Moved `ApplyGridDimensions()` in `HexTilemapBridge` from `Awake()` to `Start()`. Replaced obsolete `FindFirstObjectByType` in `GridMover` with modern `FindAnyObjectByType`. Recorded Zero-Warning Policy in `AGENTS.md`.
  - **Verification Status**: 72/72 EditMode NUnit tests passed via Unity 6 batchmode CLI (`ExitCode: 0`). Zero warnings, zero errors. Awaiting developer PlayMode testing for `[VERIFIED]`.

---

## Backlog

- [BACKLOG] **Step 3: Unit Occupancy & Faction Ownership (`GridOccupant`, `CellOwner`)**
  - Universal `HexOccupancyMap` and `GridOccupant` component (syncing unit entity to grid cell, blocking occupied cells).
  - Cell ownership zones (`Player`, `Enemy`, `Neutral`).

- [BACKLOG] **Step 4: Battle Formations & UI Layout Conversion**
  - Port `FormationPlanner` (templates 2-3, 3-2, 1-2-1, 120°/180° rotation, anchor alignment).
  - Port `HexTilemapSampler` and `HexUiLayoutConverter` for party deployment UI.
