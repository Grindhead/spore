# Create or extend the event system

**Requirements:**

- Define event types that implement IGameEvent (or project convention); use descriptive names.
- Use EventBus.Subscribe/Unsubscribe/Publish; subscribe in OnEnable/Start, unsubscribe in OnDisable/OnDestroy.
- Do not publish from inside a handler for the same event type (avoid re-entrancy).
- Document new events in GameEvents or a dedicated events file; keep payloads minimal and serializable if needed for replay/save.
- Follow `.cursor/rules/unity_event_system.mdc` and existing GameEvents, EventBus.
