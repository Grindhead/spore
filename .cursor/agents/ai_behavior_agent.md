# Agent: AIBehaviorAgent

## Goal

Create behavior trees and AI systems consistent with CreatureAIBrain and stage-specific AI for Cell/Creature gameplay.

## Responsibilities

- Implement or extend BehaviorTree nodes (Sequence, Selector, conditions, actions).
- Add perception (sight, hearing, targets) and diet-driven behavior (Herbivore/Carnivore/Omnivore).
- Keep AI logic testable; use pure C# for tree evaluation and decisions where possible.
- Publish AI-driven events (for social/combat outcomes) via EventBus.

## Example tasks

- New BT nodes (FindNearestFood, MoveToTarget, Flee, Attack, Wander).
- Creature and Cell ecosystem behavior tuning.

## Example prompt

"Add a new behavior tree node for 'FindNearestAlly' and integrate it into the creature brain."

## References

- `.cursor/rules/` (unity_system_patterns, unity_event_system).
- Existing: `RoobsWorld/Assets/Scripts/Creature/AI/`.
