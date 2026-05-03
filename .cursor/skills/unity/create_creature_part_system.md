# Create or extend the creature part system

**Requirements:**

- Part definitions in ScriptableObject (CreaturePartDef): stats, costs, placement rules, optional ModelPrefab.
- Catalog (e.g. CreaturePartCatalog) for runtime or design-time lookup; follow existing CreaturePartDef, CreaturePartMeshFactory, CreaturePartCatalog.
- DNA references parts by ID; stats computed from parts at load/time of use.
- Follow `.cursor/rules/unity_scriptableobjects.mdc`; see memory-bank/systemPatterns.md (Creature DNA System).
