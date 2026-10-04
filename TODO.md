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

- [IMPLEMENTED] **Step 2: Visual Highlighting Flexibility & Inspector Dimensions**
  - **TileFlags.LockColor Bugfix**: `TilemapHighlightOverlay` clears `TileFlags.LockColor` before calling `SetColor` so Inspector tint colors (blue, yellow, green, red) actually render on the screen.
  - **Cell Scale & Spacing Inspector Controls**: Exposed `_cellScale` and `_cellSpacing` in `HexTilemapBridge` Inspector matching `Legends: Legacy of the Lost` (`CellVisualFormatter.ApplyCellScaling` and `ApplyCellSpacing`).
  - **Outline / Contour Highlighting**: Generated `HexagonPointTop_Outline.png` sprite and `HexTile_Highlight_Outline.asset`. Implemented `HighlightRenderMode` (Solid vs Outline) allowing transparent-center edge framing that leaves background terrain artwork 100% visible.
  - **3D Modular Hex Spawner Foundation (`HexGrid3DSpawner`)**: Created component mapping 2D painted Tilemap cells to physical 3D hex prefabs/meshes with clean spawn and cleanup routines.
  - **Menu Item Sync**: `HexGridMenuCommands` creates grid with outline tile, solid tile, bridge controls, and 3D spawner wired out-of-the-box.
  - **Verification Status**: 58/58 EditMode NUnit tests passed via Unity batchmode CLI (`ExitCode: 0`). Awaiting developer PlayMode testing for `[VERIFIED]`.

---

## Backlog

- [BACKLOG] **Step 3: Unit Occupancy & Faction Ownership (`GridOccupant`, `CellOwner`)**
  - Universal `HexOccupancyMap` and `GridOccupant` component (syncing unit entity to grid cell, blocking occupied cells).
  - Cell ownership zones (`Player`, `Enemy`, `Neutral`).

- [BACKLOG] **Step 4: Battle Formations & UI Layout Conversion**
  - Port `FormationPlanner` (templates 2-3, 3-2, 1-2-1, 120°/180° rotation, anchor alignment).
  - Port `HexTilemapSampler` and `HexUiLayoutConverter` for party deployment UI.
