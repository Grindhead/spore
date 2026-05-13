# Gameplay Revision Pass 8 TODO

Scope: continue improving Creature-stage objective variety by making Alpha Hunt require real alpha kills instead of generic rival defeats.

## Review Findings

- Alpha Hunt had an alpha label and first alpha target, but completion still counted any rival defeat.
- The first alpha could be killed early, then the objective could stall behind normal rivals before another alpha appeared.
- The HUD did not distinguish alpha objective progress from generic rival defeat progress.

## Tasks

- [x] Track total alpha rival defeats separately from total rival defeats.
- [x] Give Alpha Hunt its own objective progress baseline and completion counter.
- [x] Publish whether a defeated rival was an alpha for HUD/telemetry consumers.
- [x] Keep Alpha Hunt supplied with alpha targets by promoting a successor and recurring alpha spawns.
- [x] Add alpha-specific objective progress text in localization.
- [x] Add telemetry for alpha rival kills.
- [x] Add focused EditMode coverage for alpha progress and recurring alpha spawn rules.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Alpha Hunt now completes from alpha rival kills rather than generic rival kills.
- The encounter manager tracks alpha defeats, publishes alpha defeat metadata, promotes successor alpha targets during the objective, and alternates alpha replacement spawns.
- The Creature HUD now labels Alpha Hunt progress as "Alpha Rivals" so the objective is clearer in play.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
