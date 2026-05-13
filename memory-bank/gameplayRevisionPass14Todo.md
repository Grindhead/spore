# Gameplay Revision Pass 14 TODO

Scope: continue the gameplay todo stream by reducing likely "what do I do next?" playtest moments.

## Review Findings

- The open todo items now require human 8-10 minute playtests, but code can still reduce the highest-risk confusion before those sessions.
- Recent passes seed food, part, predator, toxic, bloom, and larger-prey opportunities, yet the HUD only showed the raw task and reward.
- Players could miss why a newly seeded nearby object matters, especially when a goal changes quickly.

## Tasks

- [x] Review the remaining gameplay todo items and identify which are human-only versus code-actionable.
- [x] Add compact goal-action hints to the Cell HUD goal line.
- [x] Name the seeded opportunity shape for food, toxin, larger-prey, and part goals.
- [x] Keep hints short enough to sit beside progress and reward text.
- [x] Add focused EditMode coverage for goal guidance suffixes.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-10

- Cell HUD goal text now includes a compact action hint between progress and reward, naming the next opportunity shape without needing a longer tutorial popup.
- Food, toxin, larger-prey, and part goals now line up with the opportunity seeding from passes 10-13: "Nearby food", "Toxic trail", "Orange prey", and "Nearby part".
- Other authored goals now also get short action cues such as "Break away", "Grow to upgrade", "Hold toxic zone", and "Bloom food".
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
