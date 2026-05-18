# Modular Animal Parts Generator (Blender)

Script:
- `tools/blender_scripts/generate_modular_animal_parts.py`

Output folder:
- `RoobsWorld/Assets/Art/Models/CreatureParts/ModularGenerated`

Each exported FBX includes:
- `<part>_VIS` visible mesh
- `<part>_COL` simplified collider mesh

Generated part families:
- `head`
- `torso`
- `leg`
- `arm`
- `tail`
- `ear`
- `horn`
- `wing`
- `jaw`

## Run (default)

```powershell
"C:\Program Files\Blender Foundation\Blender\blender.exe" --background --factory-startup --python "tools/blender_scripts/generate_modular_animal_parts.py"
```

Defaults:
- `--seed 1234`
- `--count-per-type 10`

Total files with defaults:
- `9 part types x 10 variants = 90 FBX`

## Run a larger batch

```powershell
"C:\Program Files\Blender Foundation\Blender\blender.exe" --background --factory-startup --python "tools/blender_scripts/generate_modular_animal_parts.py" -- --seed 7 --count-per-type 24
```

This example generates:
- `9 x 24 = 216 FBX`
