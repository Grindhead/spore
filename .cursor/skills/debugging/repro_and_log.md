# Add repro steps and targeted logging for a bug

**Requirements:**

- Document minimal steps to reproduce (scene, inputs, order of actions).
- Add logging only at decision points or state changes relevant to the bug; use conditional compilation or log level to avoid ship build impact.
- Do not add logs in Update or other hot paths unless necessary for diagnosis.
- Suggest assertions for invariants that, if violated, would explain the bug.
- Follow `.cursor/rules/debugging_rules.mdc`.
