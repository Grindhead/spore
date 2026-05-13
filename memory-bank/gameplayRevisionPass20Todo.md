# Gameplay Revision Pass 20 Todo

Scope: add fresh gameplay improvements that make Cell-stage goals more rewarding when players vary their actions, and make multiplayer remote movement more stable under out-of-order input packets.

- [x] Review current Cell goal rewards and multiplayer input handling for low-risk improvement points.
- [x] Add an adaptive Cell goal variety chain with a capped DNA bonus.
- [x] Include the variety bonus in goal completion events so HUD/toast rewards show the true payout.
- [x] Track host-side remote input ticks and reject stale multiplayer input packets.
- [x] Add EditMode coverage for goal variety reward math and stale input ordering.
- [x] Validate the gameplay pass with build and EditMode tests.
- [ ] Run manual Cell-stage and multiplayer movement playtests to tune the bonus pacing and packet-order feel.
