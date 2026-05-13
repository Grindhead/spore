# Gameplay Revision Pass 17 Todo

Scope: continue gameplay improvements with a multiplayer-facing pass that makes shared Cell-stage sessions easier to read during play.

- [x] Review open gameplay todo items and confirm which remaining items still require human playtests.
- [x] Add multiplayer remote-cell labels that show mass and size relationship against the local player.
- [x] Feed local player mass into remote-player snapshot application so labels update during shared play.
- [x] Send client mass through remote input packets so host-relayed labels stay meaningful for other clients.
- [x] Keep labels compact and readable above remote ghosts as they scale.
- [x] Add EditMode coverage for multiplayer mass relationship and status label formatting.
- [x] Validate the gameplay pass with build and EditMode tests.
- [ ] Run a manual multiplayer Cell-stage playtest to confirm remote-player labels improve shared-ocean readability.
