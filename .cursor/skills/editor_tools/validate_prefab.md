# Create a prefab validation tool

**Requirements:**

- Scan prefabs (selected or folder) for missing references, naming violations, or required components.
- Report results in console or EditorWindow; optional fix actions with user confirmation.
- Do not modify prefabs unless user triggers fix; validation read-only by default.
- Follow `.cursor/rules/unity_editor_tools.mdc`; see create_asset_validator skill.
