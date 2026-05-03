# Add a new game event type

**Requirements:**

- Define a new event type implementing IGameEvent (or project convention); descriptive name (e.g. XHappenedEvent).
- Add to GameEvents or dedicated events file; keep payload minimal and serializable if needed for save/replay.
- Document who publishes and who subscribes; subscribe in OnEnable/Start, unsubscribe in OnDisable/OnDestroy.
- Do not publish the same event from inside its own handler (re-entrancy).
- Follow `.cursor/rules/unity_event_system.mdc`; update docs or GameEvents comments.
