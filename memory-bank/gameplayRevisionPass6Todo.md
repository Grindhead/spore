# Gameplay Revision Pass 6 TODO

Scope: continue from all existing gameplay todo lists. Do not mark human playtests as complete without real sessions; use this pass for code-side improvements that reduce likely playtest confusion and increase build identity.

## Review Findings

- The prior pass added stamina and inherited abilities, but Creature HUD feedback still leaned on text. Players need a quick meter read during combat.
- Creature upgrades had tags and tradeoffs, but most of their permanent impact still came from health and speed. Build identity should touch attack, defense, stamina, and ability cadence.
- Cell repeatable goals now reward risk, but the current objective line did not show reward value until completion.

## Tasks

- [x] Add visual Creature stamina and inherited-ability cooldown meters to the HUD.
- [x] Expand Creature upgrade records so options can affect attack, defense, max stamina, stamina recovery, and ability cooldown.
- [x] Add a fourth "control" upgrade option focused on stamina recovery and inherited ability cadence.
- [x] Surface expanded upgrade deltas in the Creature upgrade choice UI.
- [x] Show the current Cell goal DNA reward directly in the goal HUD line, including a "Risk reward" label for high-payout repeatable routes.
- [x] Add focused EditMode coverage for upgrade identity, stamina upgrade modifiers, upgrade labels, and Cell goal reward suffixes.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Creature upgrades now carry gameplay identity beyond health/speed and persist their extra deltas through DNA upgrade records.
- Creature stamina and inherited ability readiness now have visual HUD bars.
- Cell goals now display expected DNA reward before completion, so risk routes are readable before the player commits.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style test project does not print individual NUnit cases through the .NET runner.
- Unity EditMode batch run was not repeated in this pass because the immediately previous pass already reproduced the current environment blocker: Package Manager IPC timeout plus readonly database writes before a test result XML is produced.
