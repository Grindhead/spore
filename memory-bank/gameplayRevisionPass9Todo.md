# Gameplay Revision Pass 9 TODO

Scope: review the current Creature objective loop and keep improving moment-to-moment gameplay pressure.

## Review Findings

- Hold Feeding Ground used rival-defeat progress for population pressure, so killing enough rivals could stop replacement spawns before the hold timer finished.
- Hold progress text showed the timer, but it did not tell the player whether they were currently inside the ground and securing progress.
- The objective status meter could treat rival kills as completion progress during a hold objective, muddying the feedback.

## Tasks

- [x] Make hold-objective spawn pressure scale from hold time instead of rival kills.
- [x] Keep replacement spawns available during Hold Feeding Ground until the hold timer itself is complete.
- [x] Display whether the player is "Securing" the ground or needs to "Return to ground".
- [x] Make the HUD progress/status meter use hold-time pressure for hold objectives.
- [x] Add focused EditMode coverage for hold pressure progress and hold HUD text.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Hold Feeding Ground now drives encounter pressure from hold-time progress, so killing rivals early no longer dries up replacement spawns before the objective is actually complete.
- The HUD now distinguishes "Securing" from "Return to ground" for hold objectives.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
