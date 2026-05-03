# Add a new game stage (or major game mode)

**Requirements:**

- Define stage in StageManager FSM; one-way transitions from/to existing stages as per design.
- Create scene(s), camera rig, input mapping, and HUD for the stage; follow naming (e.g. XStage, XStageHUD).
- Implement IGameStage or project stage interface; register in StageManager and Build Settings.
- Use EventBus for stage entry/exit and transition events; update GameEvents if new event types.
- Update `memory-bank/systemPatterns.md` and `activeContext.md` with the new stage; follow `.cursor/rules/unity_scene_structure.mdc`, `folder_structure_rules.mdc`.
