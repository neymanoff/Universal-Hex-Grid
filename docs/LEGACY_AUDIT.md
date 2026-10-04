# Universal Hex Grid — Legacy Codebase Audit (docs/LEGACY_AUDIT.md)

> Analyzes the original grid implementation in `LegendsLegacyOfLost` and defines what is extracted, what is refactored, and what is discarded.

---

## 1. Audit Summary by File

| Original File (`LegendsLegacyOfLost`) | What it did | Status in `Universal-Hex-Grid` | Rationale |
| :--- | :--- | :--- | :--- |
| `Features/GridModule/Runtime/HexCoord.cs` | Axial & Odd-R math, neighbor indexing | **Extracted & Refactored** | Decouple from `UnityEngine.Vector3Int`, retain rock-solid integer math, add operators. |
| `Features/GridModule/Runtime/GridPathfinder.cs` | BFS pathfinding with `stopAdjacentIfBlocked` | **Refactored into A\*** | Upgrade from unweighted BFS to weighted A* with traversal rules; preserve approach mode. |
| `Core/Targeting/GridTargetResolver.cs` | Shape targeting (Line, Cone, Area) | **Extracted Geometric Math Only** | Decouple from `UnitBase`, `TeamUtils`, and `TurnManager`. Fix cone asymmetry. |
| `Core/Grid/GridManager.cs` | Monolithic manager, instantiated cell prefabs from Tilemap | **Discarded / Replaced** | Violates DRY and Inspector-first. Replaced by `HexTilemapBridge` using native Tilemaps directly. |
| `Core/Grid/GridCell.cs` | Individual MonoBehaviour per cell with SpriteRenderer | **Discarded** | Redundant. Native Tilemap handles rendering in a single draw call without hundreds of GameObjects. |
| `Core/Grid/CellVisualFormatter.cs` | Procedural Texture2D outlines at runtime | **Discarded** | Causes memory leaks and stutter. Replaced by overlay Tilemap with designer-assigned highlight tiles. |
| `Core/World/WorldMapMover.cs` | Input raycast and tile-by-tile movement | **Refactored** | Extract movement interpolation into `GridMover`, pointer picking into `TilemapPointerPicker`. |
| `Core/Grid/HexTilemapSampler.cs` | Samples Tilemap cell bounds and colors | **Refactored** | Simplified into `HexTilemapBridge` methods. |
| `Core/Grid/FormationPlanner.cs` | Spawn layout rotations (120°, 180°) | **Moved to Combat/Roster** | Formation and spawn planning belongs to the battle orchestrator, not the generic spatial grid. |

---

## 2. Identified Bugs & Anti-Patterns Fixed During Extraction

### 2.1. Cone Shape Asymmetry & Range Drift
* **Legacy Bug:** In `GridTargetResolver.ConeCells`, rows were offset asymmetrically along a single diagonal axis (`dirDiag = (dirF + 1) % 6`), causing the cone to veer off to one side and reach cells beyond the declared range.
* **Fix:** `HexTargetResolver` implements a mathematically symmetrical 120° cone centered along the forward axis.

### 2.2. Monolithic Singletons & God Classes
* **Legacy Bug:** `GridManager.Instance` was responsible for reading tilemaps, instantiating cell prefabs, caching outlines, managing occupancy, and distance lookups.
* **Fix:** Separation of concerns:
  * Pure math & pathfinding -> `HexCoord`, `HexPathfinder` (Core).
  * Tilemap reading & world coords -> `HexTilemapBridge` (Unity Presentation).
  * Unit movement -> `GridMover` (Unity Presentation).

### 2.3. Procedural Runtime Texture Generation
* **Legacy Bug:** `CellVisualFormatter` generated custom outline sprites at runtime by cloning and iterating pixel buffers of textures, caching them statically, which risked leaks across scene unloads.
* **Fix:** Inspector-first overlay Tilemap: highlight tiles are standard Unity assets assigned in the Inspector.
