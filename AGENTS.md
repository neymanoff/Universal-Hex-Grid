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

## 4. Git & Asset Safety
* **Manual Developer Commits**: The agent must NEVER execute `git commit`. The developer reviews all diffs and commits manually via GitHub Desktop. Upon completing work, the agent provides only the suggested commit `Summary` and `Description` in English.
* **Unity Meta Files**: Every asset must have a valid `.meta` file. Never delete `.meta` files without verifying source existence.
