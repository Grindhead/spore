# Create an asset validator (editor tool)

**Requirements:**

- Scan selected assets or project folders (prefabs, ScriptableObjects, scenes) for issues.
- Report missing references, naming violations, or config errors; output to console or window.
- Use MenuItem; optional EditorWindow to show results and fix actions.
- Do not modify assets unless user triggers a fix; make validation read-only by default.
- Follow `.cursor/rules/unity_editor_tools.mdc`.
