# AI Agent Collaboration & Engineering Standards (AGENTS.md)

> **MANDATORY INSTRUCTIONS FOR ALL AI AGENTS ENTERING THIS WORKSPACE**:
> You must strictly adhere to the operational agreements defined below.

---

## 1. Operational Protocol: Discussion vs. Execution
* **Questions != Directives**: Any prompt phrased as a question, hypothesis, review, or discussion (*"What do you say to this?"*, *"Could we do X?"*) is strictly an analytical inquiry. **DO NOT edit, create, or delete code or docs.** Debate, analyze trade-offs, and wait for confirmation.
* **Direct Action Triggers**: Only execute code changes or file modifications when receiving an explicit, unambiguous command (e.g. *"Implement this"*, *"Make changes"*, *"Go ahead"*).
* **Language Rules**:
  * Chat dialogue and interactive discussions with the developer: **RUSSIAN**.
  * Code, XML docstrings, comments, Git commit messages, and documentation: **ENGLISH**.

---

## 2. 3-Tier Anti-Obsolete Defense (Unity 6 Standards)
This project targets the exact Unity version recorded in `ProjectSettings/ProjectVersion.txt` and strictly prohibits obsolete Unity APIs (enforced via `Directory.Build.props` with `<WarningsAsErrors>CS0618</WarningsAsErrors>`):
1. **Never use obsolete methods**:
   * `FindObjectOfType<T>()` -> Use `FindFirstObjectByType<T>()` or `FindAnyObjectByType<T>()`.
   * `UnityEngine.UI.Text` -> Use `TMPro.TextMeshProUGUI`.
   * `WWW` -> Use `UnityEngine.Networking.UnityWebRequest`.
   * `Application.LoadLevel(...)` -> Use `UnityEngine.SceneManagement.SceneManager.LoadScene(...)`.
   * `Random.RandomRange(...)` -> Use `UnityEngine.Random.Range(...)`.
   * Legacy `UnityEngine.Input.*` -> Use Unity 6 Input System (`UnityEngine.InputSystem`).

---

## 3. Mandatory Modular & Inspector Standards
* **Inspector First**: Do not write in C# code what can and should be configured visually in the Unity Inspector, Prefabs, or ScriptableObjects.
* **Greybox Prototyping First**: Test mechanics using primitive shapes (cubes for units, flat cylinders/hexes for cells, spheres for AOE projections) in a dedicated demo scene before touching production art.
* **Do Not Reinvent the Wheel**: Leverage official Unity packages (Input System, TextMeshPro, Cinemachine) and vetted free commercial-permissive libraries.
* **Zero Duplication & Extraction**: Extract working math and pathfinding from the main game (`Legends: Legacy of the Lost`), refine it into game-agnostic interfaces, and maintain clean separation between Headless Domain (Pure C#) and Unity Presentation.
* Refer to `MODULAR_DEVELOPMENT_AGREEMENTS.md` for the full cross-project engineering standard.

---

## 4. Source-First Grounding & Architectural Truth
1. **Mandatory Source Code Inspection**:
   * Before writing, generating, or refactoring ANY code in an extracted module, the agent MUST first inspect and cite the corresponding working files in the main game (`d:\Unity\My Projects\LegendsLegacyOfLost`).
   * Never invent new abstractions or speculative architectures without first verifying how the system was originally solved in `Legends: Legacy of the Lost`.
2. **Tactical Grid Specifications (Legends Ground Truth)**:
   * **Pointy-Top Aspect Ratio**: `cellSize` in `Grid` MUST strictly equal `(0.8659766f, 1f, 1f)` ($\frac{\sqrt{3}}{2} \approx 0.8660254$), matching `Grid_Battle_Formation_HexPT.prefab`. Never use `(1, 1, 1)` which distorts horizontal spacing by 15.5%.
   * **3D Ground Plane (XZ)**: Tactical combat takes place in 3D world space on the ground plane (XZ horizontal, Y up). Units stand on top of cell surfaces with proper vertical pivot height offsets, never sinking into the ground or floating in a vertical 2D XY wall.
   * **Dual Scope Requirement**: The hex grid module MUST cover BOTH core pillars:
     1. **Battle Preparation & Formations**: Formation templates (2-3, 3-2, 1-2-1), anchor alignment, 120°/180° orientation, tilemap sampling, and UI layout conversion (`FormationPlanner`, `HexTilemapSampler`, `HexUiLayoutConverter`, `CellOwner`).
     2. **3D Tactical Combat**: Turn movement, AP budget, unit-cell occupancy (`GridOccupant`), A* pathfinding, Dijkstra flood fill, and AoE target projections (120° cone, line, ring, blast).
3. **Step-by-Step Discipline & Communication**:
   * Strictly execute one bounded phase at a time. Never bundle multiple phases or jump ahead without explicit user review.
   * If Unity batchmode CLI or background test execution is required, explicitly ask the developer to save and close the Unity Editor. Never run batchmode while the project is locked by an active Editor instance.

## 5. Task Progression & Verification Protocol ([IMPLEMENTED] vs [VERIFIED])
1. **Pre-Execution Registration**: Before writing any code, the intended task must be registered as `[IN PROGRESS]` in `TODO.md` with specific acceptance criteria.
2. **"Implemented" vs "Verified"**:
   * `[IMPLEMENTED]`: The agent has completed the code, passed 100% of automated NUnit CLI tests, and verified zero compilation errors.
   * `[VERIFIED]`: The human developer has personally tested the feature in the Unity Editor PlayMode and explicitly confirmed that it works as expected.
   * **STRICT PROHIBITION**: The agent is NEVER permitted to mark a task as `[VERIFIED]` on its own. Only the human developer grants verification status.
3. **Contour/Outline Highlighting Rule**:
   * Cell highlights must support both Solid fill and Outline/Contour modes (`HighlightStyle.Solid` and `HighlightStyle.Outline`).
   * When underlying map art or tile illustrations are present, outline mode must preserve 100% visibility of the underlying artwork while clearly framing the active cell.
4. **3D Modular Hex Spawner Requirement**:
   * The module must support 3D physical hex meshes/prefabs (`HexGrid3DSpawner`) instantiated from 2D Tilemap authoring layouts.

---

## 6. Git & Asset Safety
* **Manual Developer Commits**: The agent must NEVER execute `git commit`. The developer reviews all diffs and commits manually via GitHub Desktop. Upon completing work, the agent provides only the suggested commit `Summary` and `Description` in English.
* **Unity Meta Files**: Every asset must have a valid `.meta` file. Never delete `.meta` files without verifying source existence.
