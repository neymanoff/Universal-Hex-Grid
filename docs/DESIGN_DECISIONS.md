# Universal Hex Grid — Design Decisions & Rationale (docs/DESIGN_DECISIONS.md)

> Records technical design decisions, architectural trade-offs, and solutions to previous implementation issues.

---

## ADR 01: Unity Native Tilemap vs. Custom Procedural Grid

* **Status:** Accepted
* **Context:** In early iterations of the monolithic game, a custom grid was built by instantiating GameObjects/cylinders for every cell and procedural sprite generation for outlines. This created heavy scene hierarchies, high draw calls, and made visual level editing cumbersome.
* **Decision:** Rely on Unity's official built-in **`UnityEngine.Grid`** and **`UnityEngine.Tilemaps`** (Hexagonal Point Top).
* **Rationale:**
  1. *Zero Reinvention*: Unity handles geometry, batching, sorting layers, and camera rendering natively in C++.
  2. *Inspector-First Authoring*: Level designers paint terrain, obstacles, and hazards directly in the Unity Tile Palette.
  3. *Performance*: Thousands of tiles render in a single batched draw call without per-tile GameObject overhead.

---

## ADR 02: Headless Pure C# Core (`noEngineReferences: true`)

* **Status:** Accepted
* **Context:** Tactical RPGs often require server-authoritative simulation (anti-cheat, combat resolution, headless bots) or fast isolated unit testing.
* **Decision:** Keep mathematical coordinates, A* pathfinding, reachable flood-fill, and targeting geometry in an independent pure C# assembly (`Neymanoff.HexGrid.Core`) with no reference to `UnityEngine.dll`.
* **Rationale:**
  1. The domain code can be tested instantly via NUnit without starting the Unity engine.
  2. Can be compiled into external server executables or Docker containers if needed.
  3. Strict adherence to Clean Architecture / Hexagonal Architecture.

---

## ADR 03: Coordinate System & Unity Offset Alignment

* **Status:** Accepted
* **Context:** Hex grids can be Pointy-Top or Flat-Top, with various offset conventions (odd-r, even-r, odd-q, even-q).
* **Decision:** Standardize on **Pointy-Top** using axial `(q, r)` internally, with explicit two-way conversions to Unity's **Hexagon Point Top** layout (Odd-R offset: odd rows shifted right):
  * `Axial to Odd-R`: `col = q + (r - (r & 1)) / 2; row = r;`
  * `Odd-R to Axial`: `q = col - (row - (row & 1)) / 2; r = row;`
* **Rationale:** Matches Unity's default Tile Palette settings for point-top hexagonal grids, allowing 1:1 parity between `HexCoord` and Tilemap `Vector3Int`.

---

## ADR 04: Targeting Cone Geometry (Symmetrical 120°)

* **Status:** Accepted
* **Context:** The legacy code in `LegendsLegacyOfLost` implemented a cone that suffered from diagonal drift and exceeded its designated range due to asymmetric row step indexing.
* **Decision:** Standardize on a symmetrical 120° cone spreading along one of the 6 primary hex directions. At depth $d$, the arc consists of $2d + 1$ cells centered on the forward axis.
* **Rationale:** Completely symmetrical, predictable, and mathematically sound for tactical RPG area-of-effect abilities.

---

## ADR 05: Approach Pathfinding (`stopAdjacentIfBlocked`)

* **Status:** Accepted
* **Context:** In tactical RPGs, clicking an enemy unit or an interactive object (chest, NPC) targets an occupied cell. Standard A* returns "path not found" because the destination cell cannot be entered.
* **Decision:** Built-in support for `stopAdjacentIfBlocked`: if the goal cannot be entered, the pathfinder returns the shortest valid path to the best reachable neighbor adjacent to the goal.
* **Rationale:** Eliminates repeated pathfinding logic and edge cases between combat movement, melee attacking, and world map navigation.

---

## ADR 06: Highlight Rendering via Overlay Tilemap

* **Status:** Accepted
* **Context:** The legacy implementation generated procedural outline sprites and dynamic child SpriteRenderers for each highlighted cell, leading to memory leaks and complex caching.
* **Decision:** Render pathlines, reachable zones, and AOE previews by setting highlight tiles on a designated overlay Tilemap.
* **Rationale:** Zero texture allocation at runtime, clean Inspector setup (swap highlight tiles/colors in serialized fields), and instant clearing with `tilemap.ClearAllTiles()`.
