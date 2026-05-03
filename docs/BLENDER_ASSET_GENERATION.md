# Blender Asset Generation (Cell + Creature Stages)

This repo now includes a Blender asset builder script:

- `tools/blender_scripts/generate_spore_stage_assets.py`

It generates starter meshes + textures for a Spore-like **Cell** and **Creature** stage pipeline, then exports Unity-ready FBX files.

## What It Generates

### Models (FBX)

Export path:
- `RoobsWorld/Assets/Art/Models/CellParts`
- `RoobsWorld/Assets/Art/Models/CreatureParts`

Assets:
- `cell_body`
- `cell_flagella`
- `cell_cilia`
- `cell_spike`
- `cell_jet`
- `cell_pickup`
- `cell_food_algae`
- `cell_food_bacteria`
- `cell_food_meat`
- `cell_toxin_cloud`
- `cell_current_glyph`
- `mouth_basic`
- `eye_basic`
- `leg_basic`
- `arm_basic`
- `wing_basic`
- `tail_basic`
- `horn_basic`
- `fin_basic`
- `shell_basic`
- `spike_basic`

Each FBX contains:
- `<asset>_VIS` (render mesh)
- `<asset>_COL` (simplified collider mesh)

### Textures (PNG)

Export path:
- `RoobsWorld/Assets/Art/Textures/CellParts`
- `RoobsWorld/Assets/Art/Textures/CreatureParts`

Files include:
- `cell_membrane.png`
- `cell_appendage.png`
- `cell_jet.png`
- `cell_food_algae.png`
- `cell_food_bacteria.png`
- `cell_food_meat.png`
- `cell_toxin_cloud.png`
- `cell_current_glyph.png`
- `creature_skin.png`
- `creature_shell.png`
- `creature_bone.png`
- `creature_eye.png`
- `creature_fin.png`

## Run With Blender

From repo root:

```powershell
"C:\Program Files\Blender Foundation\Blender\blender.exe" --background --factory-startup --python "tools/blender_scripts/generate_spore_stage_assets.py"
```

## Sync To Runtime Resources (Unity)

After Blender export, open Unity and run:

- `RoobsWorld > Tools > Blender Assets > Sync Models To Resources`

This creates runtime-loadable prefabs under:

- `Assets/Resources/Models/CellParts/*.prefab`
- `Assets/Resources/Models/CreatureParts/*.prefab`

Validation command:

- `RoobsWorld > Tools > Blender Assets > Validate Wiring`

Or run both in one click:

- `RoobsWorld > Tools > Blender Assets > Sync And Validate`

## Blender MCP Notes

Your MCP bridge setup is documented in:
- `.cursor/README-mcp-blender.md`

For MCP tool-based workflows, Blender must be running with the **Blender MCP Bridge** addon enabled and listening on `127.0.0.1:9876`.

If MCP is unavailable, the direct Blender command above still produces the same outputs used by Unity.
