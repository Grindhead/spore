# Cell-stage playtest checklist

Use this after **RoobsWorld → Quick Setup** and **EditMode tests** (see [setup.md](setup.md)).

> After each session, copy **[PLAYTEST_LOG_TEMPLATE.md](PLAYTEST_LOG_TEMPLATE.md)**, fill it in, and save it as `PLAYTEST_LOG_YYYY-MM-DD.md`.

## Before first play

- [ ] **Quick Setup:** Unity menu **RoobsWorld → Quick Setup (Tags + Build Settings + First Scene)**. Creates scenes, adds Bootstrap to first scene, assigns InputSystem_Actions.
- [ ] **Tests:** **Window → General → Test Runner** → EditMode → **Run All** (~330 tests). Fix any failures before playtesting.

## In-game (cell stage)

1. **Main menu:** Click **New Game** (or Load Game if you have a save).
2. **Movement:** **WASD** or **left stick** — cell should move in the arena.
3. **Eat:** Overlap green food (algae) or smaller cells — mass bar and DNA should increase.
4. **Growth:** Mass bar fills; at threshold the **evolution UI** appears — pick a part (e.g. flagella, spike) and continue.
5. **Reproduction:** When mass reaches reproduction threshold, **mutation choice** appears — pick one of three variants; offspring spawn and your genome updates.
6. **Hazards:** Avoid larger **predator cells**, **toxin clouds** (damage), and **current zones** (push).
7. **Documentary mode:** Press **]** (RightBracket) to cycle time scale **1× → 2× → 4×**. HUD shows "2×" or "4×" when sped up.
8. **Pause:** **Escape** or **Gamepad Start** — pause menu (Resume, Settings, Save, Main Menu).
9. **Optional:** **Alt+F** / **Alt+T** / **Alt+C** at player position to spawn food, toxin, or current (environmental manipulation).

## Audio (optional)

Add clips to **RoobsWorld/Assets/Resources/Music/** and **.../SFX/** with names from **AUDIO_CATALOG.md** (cell minimum: `mainmenu_theme`, `cell_exploration`, `ambient_underwater`, `ui_click`, `food_eaten`, `creature_death`). AudioClipLoader registers them at startup.

## Reference

- **memory-bank/activeContext.md** — Next steps, current focus.
- **memory-bank/progress.md** — What works, what needs Unity Editor work.
