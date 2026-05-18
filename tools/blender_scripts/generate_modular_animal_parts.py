"""
Generate many modular animal part variants in Blender and export as FBX.

Usage (from repo root):
  "C:\\Program Files\\Blender Foundation\\Blender\\blender.exe" --background --factory-startup --python "tools/blender_scripts/generate_modular_animal_parts.py"

Optional script args (pass after --):
  --seed 42
  --count-per-type 12

Example:
  blender.exe --background --factory-startup --python tools/blender_scripts/generate_modular_animal_parts.py -- --seed 7 --count-per-type 16
"""

from __future__ import annotations

import argparse
import math
import os
import random
from dataclasses import dataclass

import bpy


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, "..", ".."))
OUT_DIR = os.path.join(REPO_ROOT, "RoobsWorld", "Assets", "Art", "Models", "CreatureParts", "ModularGenerated")


@dataclass(frozen=True)
class PartType:
    name: str


PART_TYPES = [
    PartType("head"),
    PartType("torso"),
    PartType("leg"),
    PartType("arm"),
    PartType("tail"),
    PartType("ear"),
    PartType("horn"),
    PartType("wing"),
    PartType("jaw"),
]


def parse_args() -> argparse.Namespace:
    argv = []
    if "--" in os.sys.argv:
        argv = os.sys.argv[os.sys.argv.index("--") + 1 :]

    parser = argparse.ArgumentParser(description="Generate modular animal parts")
    parser.add_argument("--seed", type=int, default=1234)
    parser.add_argument("--count-per-type", type=int, default=10)
    return parser.parse_args(argv)


def ensure_output_dir() -> None:
    os.makedirs(OUT_DIR, exist_ok=True)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def active() -> bpy.types.Object:
    obj = bpy.context.active_object
    if obj is None:
        raise RuntimeError("Expected active object")
    return obj


def select_only(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_transform(obj: bpy.types.Object) -> None:
    select_only(obj)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def triangulate(obj: bpy.types.Object) -> None:
    tri = obj.modifiers.new(name="Triangulate", type="TRIANGULATE")
    select_only(obj)
    bpy.ops.object.modifier_apply(modifier=tri.name)


def smooth(obj: bpy.types.Object) -> None:
    select_only(obj)
    bpy.ops.object.shade_smooth()


def ensure_uv(obj: bpy.types.Object) -> None:
    select_only(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")


def duplicate_collider(vis: bpy.types.Object, ratio: float = 0.25) -> bpy.types.Object:
    col = vis.copy()
    col.data = vis.data.copy()
    col.name = vis.name.replace("_VIS", "_COL")
    bpy.context.collection.objects.link(col)

    if col.type == "MESH" and len(col.data.polygons) > 16:
        dec = col.modifiers.new(name="Decimate", type="DECIMATE")
        dec.ratio = ratio
        select_only(col)
        bpy.ops.object.modifier_apply(modifier=dec.name)

    return col


def parent_root(root_name: str, vis: bpy.types.Object, col: bpy.types.Object) -> bpy.types.Object:
    root = bpy.data.objects.new(root_name, None)
    bpy.context.collection.objects.link(root)
    vis.parent = root
    col.parent = root
    vis.matrix_parent_inverse = root.matrix_world.inverted()
    col.matrix_parent_inverse = root.matrix_world.inverted()
    return root


def export_selected(root: bpy.types.Object, out_path: str) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for c in root.children:
        c.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def rand_range(rng: random.Random, a: float, b: float) -> float:
    return rng.uniform(a, b)


def create_head(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=rand_range(rng, 0.25, 0.5),
        segments=24,
        ring_count=12,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.scale = (
        rand_range(rng, 0.8, 1.4),
        rand_range(rng, 0.7, 1.2),
        rand_range(rng, 0.8, 1.3),
    )
    obj.rotation_euler = (0.0, rand_range(rng, -0.2, 0.2), 0.0)
    return obj


def create_torso(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=rand_range(rng, 0.45, 0.8))
    obj = active()
    obj.name = f"{name}_VIS"
    obj.scale = (
        rand_range(rng, 1.0, 1.8),
        rand_range(rng, 0.8, 1.3),
        rand_range(rng, 0.9, 1.6),
    )
    return obj


def create_leg(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        radius=rand_range(rng, 0.05, 0.14),
        depth=rand_range(rng, 0.6, 1.5),
        vertices=14,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.scale = (1.0, rand_range(rng, 0.75, 1.25), 1.0)
    return obj


def create_arm(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        radius=rand_range(rng, 0.04, 0.12),
        depth=rand_range(rng, 0.5, 1.2),
        vertices=14,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, rand_range(rng, -0.3, 0.3))
    return obj


def create_tail(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        radius1=rand_range(rng, 0.06, 0.2),
        radius2=rand_range(rng, 0.01, 0.05),
        depth=rand_range(rng, 0.7, 1.9),
        vertices=16,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.rotation_euler = (math.radians(90), rand_range(rng, -0.25, 0.25), 0.0)
    return obj


def create_ear(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        radius1=rand_range(rng, 0.08, 0.22),
        radius2=0.001,
        depth=rand_range(rng, 0.2, 0.6),
        vertices=12,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.rotation_euler = (rand_range(rng, -0.3, 0.3), rand_range(rng, -0.25, 0.25), 0.0)
    return obj


def create_horn(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        radius1=rand_range(rng, 0.03, 0.11),
        radius2=0.001,
        depth=rand_range(rng, 0.25, 0.85),
        vertices=14,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.rotation_euler = (rand_range(rng, -0.15, 0.15), rand_range(rng, -0.15, 0.15), 0.0)
    return obj


def create_wing(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_plane_add(size=rand_range(rng, 0.4, 1.3))
    obj = active()
    obj.name = f"{name}_VIS"
    obj.scale = (rand_range(rng, 1.2, 2.4), rand_range(rng, 0.4, 1.2), 1.0)
    obj.rotation_euler = (0.0, 0.0, rand_range(rng, -0.35, 0.35))
    return obj


def create_jaw(rng: random.Random, name: str) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=rand_range(rng, 0.15, 0.35),
        minor_radius=rand_range(rng, 0.03, 0.10),
        major_segments=24,
        minor_segments=12,
    )
    obj = active()
    obj.name = f"{name}_VIS"
    obj.scale = (1.0, rand_range(rng, 0.5, 1.0), rand_range(rng, 0.5, 1.0))
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    return obj


BUILDERS = {
    "head": create_head,
    "torso": create_torso,
    "leg": create_leg,
    "arm": create_arm,
    "tail": create_tail,
    "ear": create_ear,
    "horn": create_horn,
    "wing": create_wing,
    "jaw": create_jaw,
}


def build_variant(part_type: str, variant_idx: int, rng: random.Random) -> str:
    part_name = f"animal_{part_type}_{variant_idx:03d}"

    vis = BUILDERS[part_type](rng, part_name)
    apply_transform(vis)
    smooth(vis)
    triangulate(vis)
    ensure_uv(vis)

    col = duplicate_collider(vis, ratio=0.22)
    apply_transform(col)

    root = parent_root(part_name, vis, col)
    out_path = os.path.join(OUT_DIR, f"{part_name}.fbx")
    export_selected(root, out_path)
    return out_path


def clear_scene_objects() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def main() -> None:
    args = parse_args()
    rng = random.Random(args.seed)

    ensure_output_dir()
    reset_scene()

    exported = []

    for pt in PART_TYPES:
        for i in range(1, args.count_per_type + 1):
            clear_scene_objects()
            path = build_variant(pt.name, i, rng)
            exported.append(path)
            print(f"[ModularAnimalParts] Exported: {path}")

    print(f"[ModularAnimalParts] Done. Exported {len(exported)} files to {OUT_DIR}")


if __name__ == "__main__":
    main()
