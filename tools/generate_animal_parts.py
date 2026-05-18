from __future__ import annotations

import math
import os
import random

import bpy


OUT_DIR = r"C:\Users\craig\work\spores\RoobsWorld\Assets\Art\Models\CreatureParts\ModularGenerated"
SEED = 1234
COUNT_PER_TYPE = 10


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
    for child in root.children:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def rr(rng: random.Random, a: float, b: float) -> float:
    return rng.uniform(a, b)


def create_part(rng: random.Random, part_type: str, part_name: str) -> bpy.types.Object:
    if part_type == "head":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=rr(rng, 0.25, 0.5), segments=24, ring_count=12)
        obj = active()
        obj.scale = (rr(rng, 0.8, 1.4), rr(rng, 0.7, 1.2), rr(rng, 0.8, 1.3))
    elif part_type == "torso":
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=rr(rng, 0.45, 0.8))
        obj = active()
        obj.scale = (rr(rng, 1.0, 1.8), rr(rng, 0.8, 1.3), rr(rng, 0.9, 1.6))
    elif part_type == "leg":
        bpy.ops.mesh.primitive_cylinder_add(radius=rr(rng, 0.05, 0.14), depth=rr(rng, 0.6, 1.5), vertices=14)
        obj = active()
    elif part_type == "arm":
        bpy.ops.mesh.primitive_cylinder_add(radius=rr(rng, 0.04, 0.12), depth=rr(rng, 0.5, 1.2), vertices=14)
        obj = active()
        obj.rotation_euler = (math.radians(90), 0.0, rr(rng, -0.3, 0.3))
    elif part_type == "tail":
        bpy.ops.mesh.primitive_cone_add(radius1=rr(rng, 0.06, 0.2), radius2=rr(rng, 0.01, 0.05), depth=rr(rng, 0.7, 1.9), vertices=16)
        obj = active()
        obj.rotation_euler = (math.radians(90), rr(rng, -0.25, 0.25), 0.0)
    elif part_type == "ear":
        bpy.ops.mesh.primitive_cone_add(radius1=rr(rng, 0.08, 0.22), radius2=0.001, depth=rr(rng, 0.2, 0.6), vertices=12)
        obj = active()
    elif part_type == "horn":
        bpy.ops.mesh.primitive_cone_add(radius1=rr(rng, 0.03, 0.11), radius2=0.001, depth=rr(rng, 0.25, 0.85), vertices=14)
        obj = active()
    elif part_type == "wing":
        bpy.ops.mesh.primitive_plane_add(size=rr(rng, 0.4, 1.3))
        obj = active()
        obj.scale = (rr(rng, 1.2, 2.4), rr(rng, 0.4, 1.2), 1.0)
    elif part_type == "jaw":
        bpy.ops.mesh.primitive_torus_add(
            major_radius=rr(rng, 0.15, 0.35),
            minor_radius=rr(rng, 0.03, 0.10),
            major_segments=24,
            minor_segments=12,
        )
        obj = active()
        obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    else:
        raise ValueError(f"Unknown part type: {part_type}")

    obj.name = f"{part_name}_VIS"
    return obj


def clear_scene_objects() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def main() -> None:
    ensure_output_dir()
    rng = random.Random(SEED)
    part_types = ["head", "torso", "leg", "arm", "tail", "ear", "horn", "wing", "jaw"]

    reset_scene()
    exported = []

    print(f"[AnimalParts] Output directory: {OUT_DIR}")

    for part_type in part_types:
        for i in range(1, COUNT_PER_TYPE + 1):
            clear_scene_objects()
            part_name = f"animal_{part_type}_{i:03d}"

            vis = create_part(rng, part_type, part_name)
            apply_transform(vis)
            smooth(vis)
            triangulate(vis)
            ensure_uv(vis)

            col = duplicate_collider(vis, ratio=0.22)
            apply_transform(col)

            root = parent_root(part_name, vis, col)
            out_path = os.path.join(OUT_DIR, f"{part_name}.fbx")
            export_selected(root, out_path)
            exported.append(out_path)

            print(f"[AnimalParts] Exported: {out_path}")

    print(f"[AnimalParts] Done. Exported {len(exported)} files.")


if __name__ == "__main__":
    main()
