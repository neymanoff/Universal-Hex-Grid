# Changelog

All notable changes to the `com.neymanoff.hex-grid` package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-05

### Added
- **Tactical Squad Formations & Spawners (`Neymanoff.HexGrid.Unity` & `Neymanoff.HexGrid.Core`)**:
  - `CellOwner`: Faction categorization enum (`Neutral`, `Player`, `Enemy`, `Ally`).
  - Integer cubic hex rotation in `HexCoord` (`RotateCw`, `RotateCcw`) supporting all 6 orientation steps (0°..300°).
  - `FormationPlanner`: Anchored layout solver with `FrontRowCenter` and `Origin` alignment modes.
  - `FormationPatternSO`: ScriptableObject asset for data-driven formation patterns with presets (2-3, 3-2, 1-2-1, Line, Wedge) and Tilemap baking (`BakeFromTilemap`).
  - `HexFormationAnchor`: Tactical squad anchor calculating spatial slot positions and orientations with live Scene View gizmos.
  - `HexSpawnPoint`: Single-entity spawner for world map entities, bosses, and patrols.
  - `GridOccupant`: MonoBehaviour component binding entities to `HexOccupancyMap`.
- **Pre-Battle UI Formations & Preview**:
  - `HexUiLayoutConverter`: Converts axial coordinates and formation patterns into uGUI pixel positions with Pointy-Top staggering, gap spacing, and auto-centering.
  - `HexTilemapSampler`: Samples non-empty hex tiles from authored Tilemaps with canonical sorting.
  - `HexFormationPreviewUI`: Pointy-top hex formation preview canvas component.
  - `HexFormationSelectorUI`: Pre-battle formation selection menu with button generation and real-time linked preview updates.
- **3D Modular Hex Spawner Enhancements (`HexGrid3DSpawner`)**:
  - Multi-tilemap source scanning (`_sourceTilemaps`) combining walkable, obstacle, and custom layers.
  - Per-tile scaling (`Scale`) and Euler rotation (`RotationOffset`) in `TilePrefabMapping` preserving base prefab scales at (1, 1, 1).
  - Bottom-to-surface alignment (`_alignBottomToSurface`) placing 3D meshes flush on ground surface ($Y = 0$).
  - `_hideTilemapsOnSpawn` option preserving 2D terrain art under 3D props.
- **Automated Tests**:
  - Expanded EditMode NUnit test suite to 92 tests with 100% pass rate and zero warnings.

## [1.0.0] - 2026-10-04

### Added
- **Pure C# Headless Core (`Neymanoff.HexGrid.Core`)**:
  - `HexCoord`: Immutable axial (Q, R) and cubic (S) coordinate struct with arithmetic operators, neighbor navigation, O(1) distance calculation, and exact Odd-R conversions.
  - `HexDirection`: Enum and extensions for pointy-top directions, opposite calculations, and 60-degree rotations.
  - `ITraversalRule`: Passability and movement cost contract decoupled from engine dependencies.
  - `HexOccupancyMap`: Spatial bidirectional O(1) entity tracking container.
  - `MinBinaryHeap`: Zero-allocation binary min-heap / priority queue for .NET Standard 2.1.
  - `HexPathfinder`: Weighted A* pathfinding with approach mode (`stopAdjacentIfBlocked`), budget limits, closed set, and deterministic tie-breaking.
  - `HexFloodFill`: Dijkstra reachable zone calculator for movement boundaries and cost maps.
  - `HexTargetResolver` & `TargetShape`: Deterministic geometric targeting engine supporting SingleCell, Line, symmetrical 120° Cone, Area, and Ring.
- **Unity Presentation Layer (`Neymanoff.HexGrid.Unity`)**:
  - `HexTilemapBridge`: Connects Unity's native Hexagonal Grid and Tilemaps to pure `HexCoord` domain; implements `ITraversalRule`.
  - `TilemapHighlightOverlay`: Single-drawcall overlay Tilemap for painting path waypoints, reachable zones, targeting previews, and hover highlights.
  - `TilemapPointerPicker`: Translates mouse/touch input into hex coordinates using the Unity 6 Input System and mathematical plane raycasts.
  - `GridMover`: Smooth waypoint-based entity movement with speed control, rotation, and arrival callbacks.
  - `HexDemoController`: Coordinates input, path previews, and targeting projections.
- **Editor Tooling (`Neymanoff.HexGrid.Editor`)**:
  - `HexGridMenuCommands`: 1-click scene hierarchy creation menu (`GameObject -> Hex Grid -> Create Tactical Grid Setup`) with wired Inspector references and undo support.
- **Automated Tests**:
  - 51 NUnit EditMode tests covering math, pathfinding, traversal rules, targeting shapes, and Tilemap conversions with 100% pass rate.
