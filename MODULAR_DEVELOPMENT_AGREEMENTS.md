# Modular Project Development & Collaboration Agreements (MODULAR_DEVELOPMENT_AGREEMENTS.md)

> **MANDATORY STANDARDS FOR ALL AI AGENTS & CONTRIBUTORS ACROSS ALL CONNECTED GAME MODULES AND PROJECTS**
> This agreement governs how systems, assets, and code are engineered across the main game (`Legends: Legacy of the Lost`), all extracted standalone modules (`Universal-Humanoid-Wardrobe`, `Universal-Hex-Grid`, `Universal-RPG-Stats`), and any derivative projects.

---

## 1. Core Principles: Respect for Maintainability & Time

### 1.1. Inspector & Visual Authoring First (No Runtime Procedural Bloat)
* **Rule**: **NEVER write in C# code what can and should be configured visually in the Unity Inspector, Prefabs, or Scenes.**
* Do not programmatically construct UI hierarchies, instantiate visual layouts, hardcode coordinates/offsets, configure cameras, or wire up references at runtime if it can be authored via serialized fields, ScriptableObjects, and the Unity Editor.
* Keep C# code lean and focused strictly on:
  1. Pure domain rules, mathematical calculations, and business logic.
  2. Data state transitions and event dispatching.
  3. Clean view bindings and adapter wrappers.
* Every component intended for designer or developer interaction must expose clean, self-explanatory `[SerializeField]` fields with tooltips and sensible defaults, allowing tweaking directly inside the Inspector without diving into code.

### 1.2. Agent Scene & Hierarchy Placement
* When an AI agent manipulates scenes, test setups, or prototypes, it must configure declarative assets, scene files, and prefabs directly rather than injecting procedural boilerplate code (e.g., dynamically creating GameObjects, adding components via endless `AddComponent` calls, or setting transforms in Awake/Start).
* Keep scene hierarchies clean, structured, and easy for the human developer to inspect, select, and adjust in the Unity Editor.

---

## 2. Leverage Existing Solutions & Package Ecosystem

### 2.1. "Never Reinvent the Wheel" Rule
* Before writing any new subsystem, check for existing official Unity packages or established, free, commercially-licensed third-party solutions (e.g., MIT, Apache 2.0, BSD, Unity Companion License).
* If an official or well-tested free package exists that can be legally used in a commercial game (e.g., Unity Input System, TextMeshPro, Cinemachine, Addressables, UniTask, A* Pathfinding / NavMesh, etc.), **use and integrate that package** instead of authoring thousands of lines of custom wheel-reinventing code.
* Custom implementation is only warranted when:
  1. No suitable, commercially-permissive package exists.
  2. The external dependency introduces unacceptable bloat or violates platform constraints.
  3. The core gameplay mechanic requires bespoke, tailored domain logic.

---

## 3. Modular Architecture & Strangler Fig Migration

### 3.1. Greybox Prototyping (Cubes & Spheres Verification)
* When extracting systems from the existing game into isolated modules, verify them using minimal greybox setups (cubes, spheres, simple canvas layouts, or unit tests) before assembling them with production art.
* Each module must prove its autonomous functionality in an isolated sample/demo scene before being linked to the main game or other modules.

### 3.2. Extraction Without Duplication (Source-First Rule)
* **Inspect Source First**: Before writing, refactoring, or generating code in any extracted module, the agent MUST first inspect and cite the existing implementation in `Legends: Legacy of the Lost` (`d:\Unity\My Projects\LegendsLegacyOfLost`). Prohibit inventing speculative architectures from scratch when working code already exists.
* Do not rewrite existing working game logic from scratch if it is already implemented in `LegendsLegacyOfLost`. Extract, isolate, and refactor the existing code into the dedicated module package.
* Avoid duplicating logic across different modular projects. Shared foundational contracts, utilities, or data models must reside in shared packages (e.g., UPM local/git dependencies or shared domain libraries) rather than copy-pasted across repositories.

### 3.3. Dual-Purpose Architecture (Asset Store / Fab & Headless Service)
Every extracted module must be structured to support two usage modes:
1. **Unity Asset / UPM Package (Client & Visuals)**:
   - Contains ScriptableObjects, Prefabs, UI, and visual adapters for Unity Asset Store, Fab, or in-engine game usage.
2. **Headless / Service Domain (Pure C# / .NET)**:
   - The core domain logic (combat math, grid algorithms, inventory management, stat calculations) must remain decoupled from `UnityEngine.dll`.
   - Allows running the exact same logic on a local machine or remote server (single server or distributed microservices) without running graphics or engine overhead.

### 3.4. Grounding in Real Game Dimensions & Conventions
* Do not make naive 2D assumptions for 3D game mechanics.
* Check exact math constants from source assets (e.g. Hex Pointy-Top ratio $\sqrt{3}/2 \approx 0.8659766$, 3D ground plane XZ, character pivot offsets).
* Support the full scope of existing game features (e.g. pre-battle formation layouts, tilemap samplers, UI preview converters, in-combat movement and abilities).

### 3.5. Step-by-Step Discipline & Tool Communication
* Work strictly step-by-step. Do not jump across multiple phases or rush to conclusions without human developer confirmation.
* If Unity batchmode CLI is required, explicitly request the developer to save and close the Editor. Never guess or run silent CLI tasks when the project is locked.

### 3.6. Two-Stage Task Completion Protocol ([IMPLEMENTED] vs [VERIFIED])
* **Pre-Work Logging**: Register tasks in `TODO.md` as `[IN PROGRESS]` before touching code.
* **[IMPLEMENTED]**: Set when code is written, compiles without errors, and passes 100% automated CLI tests.
* **[VERIFIED]**: Set ONLY after the human developer personally verifies the behavior in the Unity Editor and explicitly approves it. An agent NEVER sets `[VERIFIED]` autonomously.
* **Contour/Outline Highlights**: Cell highlighting systems must support outline/contour framing to preserve visibility of underlying map textures, illustrations, and art.

### 3.7. Non-Destructive In-Place Evolution (No Delete-And-Recreate)
* **Rule**: **NEVER instruct the developer to delete existing scene hierarchies, root GameObjects, or scenes to adopt or test new components.**
* In a production environment with dozens or hundreds of authored maps, deleting the root object destroys hand-crafted level design, lighting, references, and scene-specific configurations.
* All components must support **in-place non-destructive evolution**:
  1. Adding a new component via `Add Component` on an existing object must gracefully self-heal and auto-wire dependencies via `Reset()` and `OnValidate()`.
  2. Modifications to existing components must preserve existing serialized data, painted tilemaps, and designer overrides.

---

## 4. Summary of Developer Agreements Checklist

| Principle | Requirement | Violation Example |
| :--- | :--- | :--- |
| **Source-First** | Inspect existing game code before writing | Writing a brand-new grid or pathfinder from scratch without reading `LegendsLegacyOfLost` |
| **Inspector-First** | Configure in Inspector/Prefabs/SO | Writing 200 lines of procedural C# code to create UI buttons or build a camera rig |
| **Non-Destructive** | In-place evolution via `Add Component` / auto-wiring | Telling the developer: "Delete Hex Grid and re-run menu item to test the changes" |
| **No Reinventing** | Use official & free commercial packages | Authoring a custom tween engine or custom input polling instead of existing standard tools |
| **Lean Code** | Clean, searchable, modular C# | Monolithic scripts that handle data, logic, UI, and visuals in one place |
| **Domain Reality** | Respect 3D planes, exact ratios, full scope | Flat 2D XY wall assumptions, `(1,1,1)` hex sizing, ignoring formation placement |
| **Outline Art** | Support contour highlight mode | Solid fill completely obscuring beautiful underlying terrain artwork |
| **Two-Stage Verification** | `[IMPLEMENTED]` (agent) vs `[VERIFIED]` (human) | Agent declaring task finished without developer PlayMode sign-off |
| **Step-by-Step** | One phase at a time with developer review | Completing 4 phases in one response and claiming "ready for the next module" |
| **No Duplication** | Reuse extracted code via shared packages | Copy-pasting stat math or grid classes across 3 different projects |

