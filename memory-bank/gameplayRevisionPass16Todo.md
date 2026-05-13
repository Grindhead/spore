# Gameplay Revision Pass 16 Todo

Scope: improve Creature-stage moment-to-moment readability so objectives and streak play tell the player where to go and how long they have to keep pressure up.

- [x] Add distance-aware Creature HUD guidance for rival cull, alpha hunt, and hold-feeding-ground objectives.
- [x] Surface remaining streak-chain time in Creature objective progress text while a streak is active.
- [x] Expose nearest rival and alpha-rival distance from the encounter manager for objective guidance.
- [x] Add EditMode coverage for distance guidance, hold-ground marker distance, nearest-rival distance, and streak timer text.
- [x] Validate the gameplay pass with `dotnet build RoobsWorld.slnx --no-restore -v:minimal` and `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal`.
- [ ] Run a manual Creature-stage playtest to confirm distance and chain-time cues improve combat flow.
