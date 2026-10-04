# Changelog

All notable changes to the `com.neymanoff.hex-grid` package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
