# Gameplay Revision Pass 4 TODO

Scope: keep the repeatable Creature objective loop from stalling, and make objective completion rewards feel clearer in play.

## Tasks

- [x] Make Creature replacement-spawn caps objective-local so repeatable objectives keep enough rivals available.
- [x] Track objective-local spawned rival progress alongside objective-local defeated rival progress.
- [x] Show a short Creature HUD reward message when an objective completes.
- [x] Keep first-time achievement unlock independent from every-repeat objective reward messaging.
- [x] Add focused EditMode coverage for objective-local spawn caps and objective completion reward text.
- [x] Run available automated validation and record any Unity editor blockers.

## Validation

- `dotnet restore RoobsWorld.slnx -v:minimal` passed from `RoobsWorld/`.
- `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed from `RoobsWorld/`.
- `.\\tools\\run-editmode-tests.ps1 -NoGraphics -ResultsPath TestResults\\EditMode-20260509-pass4.xml -LogPath TestResults\\EditMode-20260509-pass4.log` did not produce a fresh XML result because Unity Package Manager could not connect to its IPC stream after 30 seconds and logged `attempt to write a readonly database`.
