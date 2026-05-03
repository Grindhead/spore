# Create an AI system for Unity

**Requirements:**

- Use behavior trees or a clear decision pipeline (e.g. Sequence/Selector nodes).
- Keep evaluation logic testable (pure C# where possible); add EditMode tests.
- Publish AI-driven events via EventBus (e.g. target acquired, action completed).
- Use ScriptableObject or data for AI parameters (ranges, weights) so designers can tune.
- Follow `.cursor/rules/unity_system_patterns.mdc` and existing AI in `Scripts/Creature/AI/` and stage NPCs.
