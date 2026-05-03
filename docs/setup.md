# Setup — Cursor + Unity + Blender

## First-time setup (order of operations)

Do these in order when opening the project for the first time or after a fresh clone:

1. **Open Unity** — Open the `RoobsWorld` folder as the project (Unity Hub or `File → Open Project`).
2. **Quick Setup** — Menu **RoobsWorld → Quick Setup (Tags + Build Settings + First Scene)**. This creates scenes, sets build order, adds Bootstrap to the first scene, and assigns InputSystem_Actions (place the `.inputactions` asset in `Assets/` or `Resources/` if missing).
3. **Run tests** — **Window → General → Test Runner** → EditMode tab → **Run All** (~330 tests). Fix any failures before continuing.
4. **CI (optional)** — In your GitHub repo: **Settings → Secrets and variables → Actions** → add secret **`UNITY_LICENSE`** so the EditMode workflow can run on push/PR (see [techContext](../memory-bank/techContext.md) § CI).
5. **Audio (optional for playtest)** — Add clips to `RoobsWorld/Assets/Resources/Music/` and `SFX/` with filenames from [AUDIO_CATALOG.md](../RoobsWorld/Assets/Resources/AUDIO_CATALOG.md) (cell minimum: mainmenu_theme, cell_exploration, ambient_underwater, ui_click, food_eaten, creature_death).
6. **Playtest** — Press Play; use **New Game** from the main menu, then play the cell stage. See [PLAYTEST_CHECKLIST.md](PLAYTEST_CHECKLIST.md) for what to verify (movement, eat, evolution, hazards, **RightBracket** for documentary time scale).

---

## First-time scene setup (Bootstrap + Input)

**One click:** **RoobsWorld → Quick Setup (Tags + Build Settings + First Scene)**. This runs in order: ensure tags (Food, AI_*), create scenes and add to Build Settings, open first scene, add Bootstrap, assign **InputSystem_Actions**.

Or run steps manually:
1. **RoobsWorld → Setup Build Settings (Create Scenes & Add to Build)**.
2. **RoobsWorld → Setup First Scene (Add Bootstrap & Assign Input)**.

To run the same setup from the command line (close Unity first):

```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" -batchmode -nographics -projectPath "path\to\RoobsWorld" -executeMethod RoobsWorld.Editor.RoobsWorldFirstSceneSetup.SetupFirstScene -quit
```

## EditMode tests

- **In Unity:** Window → General → Test Runner → EditMode tab → Run All.
- **CI:** Add the `UNITY_LICENSE` secret to your GitHub repo; the workflow runs EditMode tests on push/PR.
- **Command line (close Unity first):**  
  `Unity.exe -runTests -batchmode -projectPath "path\to\RoobsWorld" -testPlatform EditMode -testResults TestResults/EditMode.xml -quit`
- **Command line (script):** From the repo root:  
  `powershell -ExecutionPolicy Bypass -File .\tools\run-editmode-tests.ps1 -NoGraphics`

## Unity external script editor

To open Unity scripts in Cursor from the Unity Editor:

1. In Unity: **Edit → Preferences → External Tools**
2. Set **External Script Editor** to **Cursor** (or browse to the Cursor executable).
3. When you double-click a script in Unity, it opens in Cursor so the AI has full project context.

## Workspace layout

- **RoobsWorld/** — Unity project (Assets, Packages, ProjectSettings).
- **blender/** — Blender source assets (characters, environments, props).
- **tools/blender_scripts/** — Python scripts run inside Blender (export to Unity, LOD, etc.).
- **docs/** — Design and rules for Cursor and humans.
- **memory-bank/** — Project brief, patterns, progress (single source of truth).

Blender export scripts write FBX into `RoobsWorld/Assets/Art/Models`. Unity imports and optional prefab/collider pipeline run on import.

## Audio

Place music and SFX in **RoobsWorld/Assets/Resources/Music/** and **RoobsWorld/Assets/Resources/SFX/** with filenames matching the IDs in **RoobsWorld/Assets/Resources/AUDIO_CATALOG.md**. `AudioClipLoader` loads them at startup.

## Cell-stage playtest

See **[PLAYTEST_CHECKLIST.md](PLAYTEST_CHECKLIST.md)** for a step-by-step checklist (Quick Setup → tests → in-game: movement, eat, evolution, hazards, ] key, pause).

## Balance & performance (Phase 8.3)

See **[BALANCE_AND_PERFORMANCE.md](BALANCE_AND_PERFORMANCE.md)** for cell-stage tuning levers (BalanceConstants, Inspector spawn counts, AI tick, spatial grid) and playtest order.
