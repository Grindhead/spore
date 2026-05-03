"""
Generate a starter 3D asset pack for a Spore-like Cell + Creature stage game.

This script is intended to run inside Blender (GUI or background), then export
Unity-ready FBX models and PNG textures directly into this repository.

Output:
  - RoobsWorld/Assets/Art/Models/CellParts/*.fbx
  - RoobsWorld/Assets/Art/Models/CreatureParts/*.fbx
  - RoobsWorld/Assets/Art/Textures/CellParts/*.png
  - RoobsWorld/Assets/Art/Textures/CreatureParts/*.png

Naming conventions:
  - Visible mesh object: <asset>_VIS
  - Collider mesh object: <asset>_COL
"""

from __future__ import annotations

import math
import os
import warnings
from dataclasses import dataclass

import bpy


# Resolve repo paths from this script location: tools/blender_scripts/*
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, "..", ".."))
MODELS_ROOT = os.path.join(REPO_ROOT, "RoobsWorld", "Assets", "Art", "Models")
TEXTURES_ROOT = os.path.join(REPO_ROOT, "RoobsWorld", "Assets", "Art", "Textures")

CELL_MODELS = os.path.join(MODELS_ROOT, "CellParts")
CREATURE_MODELS = os.path.join(MODELS_ROOT, "CreatureParts")
CELL_TEXTURES = os.path.join(TEXTURES_ROOT, "CellParts")
CREATURE_TEXTURES = os.path.join(TEXTURES_ROOT, "CreatureParts")


@dataclass
class AssetSpec:
    name: str
    category: str  # "cell" | "creature"


ASSET_SPECS = [
    AssetSpec("cell_body", "cell"),
    AssetSpec("cell_flagella", "cell"),
    AssetSpec("cell_cilia", "cell"),
    AssetSpec("cell_spike", "cell"),
    AssetSpec("cell_jet", "cell"),
    AssetSpec("cell_pickup", "cell"),
    AssetSpec("cell_food_algae", "cell"),
    AssetSpec("cell_food_bacteria", "cell"),
    AssetSpec("cell_food_meat", "cell"),
    AssetSpec("cell_toxin_cloud", "cell"),
    AssetSpec("cell_current_glyph", "cell"),
    AssetSpec("mouth_basic", "creature"),
    AssetSpec("eye_basic", "creature"),
    AssetSpec("leg_basic", "creature"),
    AssetSpec("arm_basic", "creature"),
    AssetSpec("wing_basic", "creature"),
    AssetSpec("tail_basic", "creature"),
    AssetSpec("horn_basic", "creature"),
    AssetSpec("fin_basic", "creature"),
    AssetSpec("shell_basic", "creature"),
    AssetSpec("spike_basic", "creature"),
]


def ensure_dirs() -> None:
    for path in [CELL_MODELS, CREATURE_MODELS, CELL_TEXTURES, CREATURE_TEXTURES]:
        os.makedirs(path, exist_ok=True)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block_collection in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.textures,
        bpy.data.collections,
    ):
        for block in list(block_collection):
            if block.users == 0:
                block_collection.remove(block)


def active_object() -> bpy.types.Object:
    obj = bpy.context.active_object
    if obj is None:
        raise RuntimeError("Expected an active object but found none.")
    return obj


def apply_transform(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def set_origin_to_geometry(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")


def triangulate(obj: bpy.types.Object) -> None:
    tri = obj.modifiers.new(name="Triangulate", type="TRIANGULATE")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=tri.name)


def smooth(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()
    if obj.data and hasattr(obj.data, "use_auto_smooth"):
        obj.data.use_auto_smooth = True
        obj.data.auto_smooth_angle = math.radians(45.0)


def create_image(
    name: str,
    width: int,
    height: int,
    out_dir: str,
    base_a: tuple[float, float, float],
    base_b: tuple[float, float, float],
    noise_strength: float = 0.08,
) -> str:
    img = bpy.data.images.new(name=name, width=width, height=height, alpha=False)
    pixels = [0.0] * (width * height * 4)

    for y in range(height):
        ty = y / max(1, height - 1)
        for x in range(width):
            tx = x / max(1, width - 1)
            idx = (y * width + x) * 4
            band = 0.5 + 0.5 * math.sin((tx * 10.0 + ty * 7.0) * math.pi)
            n = noise_strength * (math.sin((x * 0.17 + y * 0.11)) + math.cos((x * 0.07 - y * 0.19)))
            r = (base_a[0] * (1.0 - band) + base_b[0] * band) + n
            g = (base_a[1] * (1.0 - band) + base_b[1] * band) + n
            b = (base_a[2] * (1.0 - band) + base_b[2] * band) + n
            pixels[idx + 0] = max(0.0, min(1.0, r))
            pixels[idx + 1] = max(0.0, min(1.0, g))
            pixels[idx + 2] = max(0.0, min(1.0, b))
            pixels[idx + 3] = 1.0

    img.pixels = pixels
    out_path = os.path.join(out_dir, f"{name}.png")
    img.filepath_raw = out_path
    img.file_format = "PNG"
    img.save()
    return out_path


def create_material(name: str, texture_path: str) -> bpy.types.Material:
    mat = bpy.data.materials.new(name=name)
    # Blender 5.1 emits a deprecation warning when reading/writing use_nodes.
    # Keep compatibility with 5.x while avoiding warning spam.
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", DeprecationWarning)
        try:
            mat.use_nodes = True
        except Exception:
            pass
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new(type="ShaderNodeOutputMaterial")
    bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
    tex = nodes.new(type="ShaderNodeTexImage")
    tex.image = bpy.data.images.load(texture_path, check_existing=True)

    out.location = (350, 0)
    bsdf.location = (120, 0)
    tex.location = (-200, 0)

    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = 0.45
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.35
    elif "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = 0.35

    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def ensure_uv(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")


def assign_material(obj: bpy.types.Object, mat: bpy.types.Material) -> None:
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)


def duplicate_for_collider(source: bpy.types.Object, name: str, simplify: float = 0.25) -> bpy.types.Object:
    col = source.copy()
    col.data = source.data.copy()
    col.name = name
    bpy.context.collection.objects.link(col)
    # Skip decimation on already tiny meshes (e.g. wing/fin planes) to avoid
    # Blender warnings: "Modifier requires more than 3 input faces".
    if col.type == "MESH" and len(col.data.polygons) > 3:
        dec = col.modifiers.new(name="Decimate", type="DECIMATE")
        dec.ratio = simplify
        bpy.ops.object.select_all(action="DESELECT")
        col.select_set(True)
        bpy.context.view_layer.objects.active = col
        try:
            bpy.ops.object.modifier_apply(modifier=dec.name)
        except Exception:
            pass
    return col


def parent_to_root(root_name: str, vis: bpy.types.Object, col: bpy.types.Object) -> bpy.types.Object:
    root = bpy.data.objects.new(root_name, None)
    bpy.context.collection.objects.link(root)
    vis.parent = root
    col.parent = root
    vis.matrix_parent_inverse = root.matrix_world.inverted()
    col.matrix_parent_inverse = root.matrix_world.inverted()
    return root


def export_asset(root_obj: bpy.types.Object, out_path: str) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    root_obj.select_set(True)
    for child in root_obj.children:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root_obj
    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def create_cell_body() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.55, segments=24, ring_count=12)
    obj = active_object()
    obj.name = "cell_body_VIS"
    obj.scale = (1.2, 0.9, 1.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_flagella() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=1.1, vertices=14)
    obj = active_object()
    obj.name = "cell_flagella_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    obj.scale = (0.7, 0.7, 1.5)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_cilia() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(radius1=0.06, radius2=0.02, depth=0.7, vertices=12)
    obj = active_object()
    obj.name = "cell_cilia_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_spike() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(radius1=0.08, radius2=0.005, depth=0.65, vertices=14)
    obj = active_object()
    obj.name = "cell_spike_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_jet() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.13, depth=0.5, vertices=18)
    obj = active_object()
    obj.name = "cell_jet_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_pickup() -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.3)
    obj = active_object()
    obj.name = "cell_pickup_VIS"
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_food_algae() -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.22)
    obj = active_object()
    obj.name = "cell_food_algae_VIS"
    obj.scale = (1.1, 0.85, 1.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_food_bacteria() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, segments=18, ring_count=10)
    obj = active_object()
    obj.name = "cell_food_bacteria_VIS"
    obj.scale = (1.25, 0.7, 0.85)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_food_meat() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=0.36)
    obj = active_object()
    obj.name = "cell_food_meat_VIS"
    obj.scale = (1.0, 0.75, 1.2)
    obj.rotation_euler = (math.radians(13), math.radians(-9), math.radians(22))
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_toxin_cloud() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, segments=24, ring_count=14)
    obj = active_object()
    obj.name = "cell_toxin_cloud_VIS"
    obj.scale = (1.0, 0.72, 1.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_cell_current_glyph() -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.34,
        minor_radius=0.05,
        major_segments=30,
        minor_segments=12,
    )
    obj = active_object()
    obj.name = "cell_current_glyph_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_mouth_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.23,
        minor_radius=0.07,
        major_segments=24,
        minor_segments=12,
    )
    obj = active_object()
    obj.name = "mouth_basic_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    obj.scale = (1.0, 0.8, 0.65)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_eye_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.16, segments=20, ring_count=10)
    obj = active_object()
    obj.name = "eye_basic_VIS"
    smooth(obj)
    triangulate(obj)
    return obj


def create_leg_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.9, vertices=14)
    obj = active_object()
    obj.name = "leg_basic_VIS"
    obj.scale = (1.0, 0.85, 1.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_arm_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.75, vertices=14)
    obj = active_object()
    obj.name = "arm_basic_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_wing_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_plane_add(size=0.8)
    obj = active_object()
    obj.name = "wing_basic_VIS"
    obj.scale = (1.3, 1.0, 0.6)
    apply_transform(obj)
    triangulate(obj)
    return obj


def create_tail_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(radius1=0.12, radius2=0.02, depth=1.0, vertices=16)
    obj = active_object()
    obj.name = "tail_basic_VIS"
    obj.rotation_euler = (math.radians(90), 0.0, 0.0)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_horn_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(radius1=0.08, radius2=0.001, depth=0.5, vertices=14)
    obj = active_object()
    obj.name = "horn_basic_VIS"
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_fin_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_plane_add(size=0.5)
    obj = active_object()
    obj.name = "fin_basic_VIS"
    obj.scale = (0.25, 1.2, 1.0)
    obj.rotation_euler = (0.0, 0.0, math.radians(90))
    apply_transform(obj)
    triangulate(obj)
    return obj


def create_shell_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.25, segments=20, ring_count=12)
    obj = active_object()
    obj.name = "shell_basic_VIS"
    obj.scale = (1.1, 0.65, 1.2)
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


def create_spike_basic() -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(radius1=0.06, radius2=0.001, depth=0.42, vertices=12)
    obj = active_object()
    obj.name = "spike_basic_VIS"
    apply_transform(obj)
    smooth(obj)
    triangulate(obj)
    return obj


BUILDERS: dict[str, callable] = {
    "cell_body": create_cell_body,
    "cell_flagella": create_cell_flagella,
    "cell_cilia": create_cell_cilia,
    "cell_spike": create_cell_spike,
    "cell_jet": create_cell_jet,
    "cell_pickup": create_cell_pickup,
    "cell_food_algae": create_cell_food_algae,
    "cell_food_bacteria": create_cell_food_bacteria,
    "cell_food_meat": create_cell_food_meat,
    "cell_toxin_cloud": create_cell_toxin_cloud,
    "cell_current_glyph": create_cell_current_glyph,
    "mouth_basic": create_mouth_basic,
    "eye_basic": create_eye_basic,
    "leg_basic": create_leg_basic,
    "arm_basic": create_arm_basic,
    "wing_basic": create_wing_basic,
    "tail_basic": create_tail_basic,
    "horn_basic": create_horn_basic,
    "fin_basic": create_fin_basic,
    "shell_basic": create_shell_basic,
    "spike_basic": create_spike_basic,
}


def build_textures() -> dict[str, str]:
    texture_paths: dict[str, str] = {}
    texture_paths["cell_membrane"] = create_image(
        "cell_membrane",
        512,
        512,
        CELL_TEXTURES,
        (0.08, 0.40, 0.55),
        (0.22, 0.70, 0.83),
        noise_strength=0.07,
    )
    texture_paths["cell_appendage"] = create_image(
        "cell_appendage",
        512,
        512,
        CELL_TEXTURES,
        (0.20, 0.55, 0.70),
        (0.55, 0.87, 0.92),
        noise_strength=0.06,
    )
    texture_paths["cell_jet"] = create_image(
        "cell_jet",
        512,
        512,
        CELL_TEXTURES,
        (0.10, 0.18, 0.24),
        (0.40, 0.55, 0.66),
        noise_strength=0.08,
    )
    texture_paths["cell_food_algae"] = create_image(
        "cell_food_algae",
        512,
        512,
        CELL_TEXTURES,
        (0.24, 0.74, 0.24),
        (0.67, 0.92, 0.38),
        noise_strength=0.08,
    )
    texture_paths["cell_food_bacteria"] = create_image(
        "cell_food_bacteria",
        512,
        512,
        CELL_TEXTURES,
        (0.88, 0.70, 0.22),
        (0.97, 0.90, 0.52),
        noise_strength=0.07,
    )
    texture_paths["cell_food_meat"] = create_image(
        "cell_food_meat",
        512,
        512,
        CELL_TEXTURES,
        (0.48, 0.16, 0.13),
        (0.72, 0.31, 0.20),
        noise_strength=0.09,
    )
    texture_paths["cell_toxin_cloud"] = create_image(
        "cell_toxin_cloud",
        512,
        512,
        CELL_TEXTURES,
        (0.18, 0.42, 0.12),
        (0.54, 0.82, 0.24),
        noise_strength=0.08,
    )
    texture_paths["cell_current_glyph"] = create_image(
        "cell_current_glyph",
        512,
        512,
        CELL_TEXTURES,
        (0.24, 0.52, 0.78),
        (0.56, 0.84, 0.96),
        noise_strength=0.06,
    )
    texture_paths["creature_skin"] = create_image(
        "creature_skin",
        1024,
        1024,
        CREATURE_TEXTURES,
        (0.38, 0.27, 0.18),
        (0.66, 0.44, 0.30),
        noise_strength=0.10,
    )
    texture_paths["creature_shell"] = create_image(
        "creature_shell",
        1024,
        1024,
        CREATURE_TEXTURES,
        (0.33, 0.29, 0.20),
        (0.58, 0.52, 0.36),
        noise_strength=0.09,
    )
    texture_paths["creature_bone"] = create_image(
        "creature_bone",
        1024,
        1024,
        CREATURE_TEXTURES,
        (0.55, 0.54, 0.50),
        (0.86, 0.84, 0.78),
        noise_strength=0.05,
    )
    texture_paths["creature_eye"] = create_image(
        "creature_eye",
        512,
        512,
        CREATURE_TEXTURES,
        (0.80, 0.80, 0.78),
        (0.96, 0.95, 0.92),
        noise_strength=0.03,
    )
    texture_paths["creature_fin"] = create_image(
        "creature_fin",
        1024,
        1024,
        CREATURE_TEXTURES,
        (0.18, 0.45, 0.62),
        (0.45, 0.72, 0.88),
        noise_strength=0.08,
    )
    return texture_paths


def choose_texture_key(asset_name: str) -> str:
    if asset_name.startswith("cell_"):
        if asset_name in (
            "cell_food_algae",
            "cell_food_bacteria",
            "cell_food_meat",
            "cell_toxin_cloud",
            "cell_current_glyph",
        ):
            return asset_name
        if asset_name == "cell_jet":
            return "cell_jet"
        if asset_name in ("cell_flagella", "cell_cilia", "cell_spike"):
            return "cell_appendage"
        return "cell_membrane"
    if "eye" in asset_name:
        return "creature_eye"
    if "shell" in asset_name:
        return "creature_shell"
    if asset_name in ("horn_basic", "spike_basic"):
        return "creature_bone"
    if asset_name in ("fin_basic", "wing_basic"):
        return "creature_fin"
    return "creature_skin"


def build_asset(asset: AssetSpec, texture_paths: dict[str, str]) -> str:
    if asset.name not in BUILDERS:
        raise KeyError(f"No builder defined for asset '{asset.name}'.")

    vis = BUILDERS[asset.name]()
    vis.name = f"{asset.name}_VIS"
    ensure_uv(vis)
    tex_path = texture_paths[choose_texture_key(asset.name)]
    mat = create_material(f"{asset.name}_MAT", tex_path)
    assign_material(vis, mat)
    set_origin_to_geometry(vis)
    apply_transform(vis)

    col = duplicate_for_collider(vis, f"{asset.name}_COL", simplify=0.2)
    set_origin_to_geometry(col)
    apply_transform(col)

    root = parent_to_root(asset.name, vis, col)

    out_dir = CELL_MODELS if asset.category == "cell" else CREATURE_MODELS
    out_path = os.path.join(out_dir, f"{asset.name}.fbx")
    export_asset(root, out_path)
    return out_path


def clear_scene_objects() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def main() -> None:
    print("[SporeAssetGen] Preparing directories...")
    ensure_dirs()
    print("[SporeAssetGen] Resetting scene...")
    reset_scene()

    print("[SporeAssetGen] Generating textures...")
    texture_paths = build_textures()
    for key, path in texture_paths.items():
        print(f"  texture: {key} -> {path}")

    exported = []
    for spec in ASSET_SPECS:
        clear_scene_objects()
        out_path = build_asset(spec, texture_paths)
        exported.append(out_path)
        print(f"[SporeAssetGen] Exported {spec.name} -> {out_path}")

    print(f"[SporeAssetGen] Done. Exported {len(exported)} FBX assets.")


if __name__ == "__main__":
    main()
