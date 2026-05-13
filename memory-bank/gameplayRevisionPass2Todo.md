# Gameplay Revision Pass 2 TODO

Scope: deepen the existing Cell and Creature loops without adding new stages or broad systems.

## Tasks

- [x] Add a Cell varied-diet reward so players are encouraged to switch food sources instead of farming one safest type.
- [x] Add a Creature guard counterattack so well-timed defensive play creates an immediate offensive payoff.
- [x] Make Creature objective rewards reflect objective risk instead of using one flat DNA payout.
- [x] Surface rival archetype defeats in the Creature HUD so encounters feel less anonymous.
- [x] Add focused EditMode coverage for the new gameplay rules.
- [x] Run available automated validation and record any Unity editor blockers.

## Validation

- `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passes from `RoobsWorld/`.
- `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:normal` exits 0, but the .NET runner reports build status only for this Unity-style test project.
- `git diff --check` passes for the touched files, with only existing LF-to-CRLF warnings.
- Fresh Unity EditMode execution is blocked before results are written: Unity cannot connect to the Package Manager IPC stream after 30 seconds and logs `attempt to write a readonly database`.
