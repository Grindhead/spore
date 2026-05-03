# Create a Unity Editor window

**Requirements:**

- Place script under `RoobsWorld/Assets/Editor/` or in an Editor-only assembly.
- Use EditorWindow for the UI; register with MenuItem for menu access.
- Support selection or project-wide context where relevant (e.g. selected objects, asset folder).
- Do not modify scenes or assets unless the user explicitly triggers an action.
- Follow `.cursor/rules/unity_editor_tools.mdc`.
