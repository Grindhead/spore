# Gameplay Revision Pass 22 Todo

Scope: add momentum to Creature objective pacing and make multiplayer Cell remote movement reflect mass, so both solo and networked play feel more responsive to player state.

- [x] Review Creature objective pacing and multiplayer remote input movement.
- [x] Add a capped Creature objective momentum delay that shortens the wait between objectives after successful clears.
- [x] Apply the momentum delay to the real Creature objective loop.
- [x] Scale host-side remote Cell input movement by remote mass so larger cells feel heavier in multiplayer.
- [x] Add EditMode coverage for objective momentum delay and remote mass-based movement speed.
- [x] Validate the gameplay pass with build and EditMode tests.
- [ ] Run manual Creature objective and multiplayer Cell movement playtests to tune delay and mass-speed feel.
