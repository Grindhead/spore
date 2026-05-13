# Gameplay Revision Pass 7 TODO

Scope: continue improving the existing Cell and Creature loops after pass 6. Keep changes focused on moment-to-moment feedback and decision pressure.

## Review Findings

- Creature stamina is now visible, but the loop needed a positive "earn it back" path so the meter feels like momentum rather than only a cost.
- Guard counters were strong defensively, but they did not restore enough tempo to clearly reward skillful timing.
- Creature streak DNA bonuses existed, but the HUD only showed a generic tutorial tip, making the reward easy to miss.

## Tasks

- [x] Restore Creature stamina on rival kills so aggressive finishes help the player keep tempo.
- [x] Restore Creature stamina when a guard counter is successfully primed.
- [x] Publish Creature stamina restore events so HUD/telemetry can explain why the meter refilled.
- [x] Publish Creature streak bonus events with streak count and DNA payout.
- [x] Show stamina restore and streak bonus messages in the Creature HUD.
- [x] Add telemetry counters for stamina restores and streak bonuses.
- [x] Add focused EditMode coverage for stamina restoration and HUD labels.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Creature combat now has a positive stamina momentum loop: kills and well-timed counters refill stamina.
- Streak DNA bonuses are now visible as explicit HUD feedback instead of only a generic tip.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
