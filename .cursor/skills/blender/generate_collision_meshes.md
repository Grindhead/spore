# Generate Blender collision meshes for Unity

**Requirements:**

- Create simplified meshes for collision; name with _COL suffix.
- Keep _COL meshes separate or as sibling to _VIS; consistent hierarchy for Unity importer.
- Apply transforms; export with _VIS mesh so Unity post-process can add MeshCollider to _COL.
- Follow `.cursor/rules/collider_rules.mdc`, `blender_asset_rules.mdc`, `import_processor_rules.mdc`.
