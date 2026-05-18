"""
Blender script: Decimate high-poly meshes, create low-poly versions, and bake normals.

Usage:
  1. Open your .blend with high-poly mesh(es).
  2. Select the mesh(es) to process.
  3. Run script via: Scripting workspace > Open this file > Run Script.
     Or: blender --background file.blend --python blender_decimate_and_bake_normals.py

Config (edit in script or via Blender text editor):
  DECIMATE_RATIO  - Target face ratio (0.0-1.0). 0.1 = 10% of original faces.
  NORMAL_MAP_SIZE - Baked normal map resolution (e.g. 2048, 4096).
  OUTPUT_SUFFIX   - Name suffix for low-poly object and baked image.
"""

import bpy

# --- Config (adjust as needed) ---
# Better default quality for character/animal parts.
DECIMATE_RATIO = 0.35  # 0.35 = 35% of original poly count
NORMAL_MAP_SIZE = 4096
OUTPUT_SUFFIX = "_low"
BAKE_MARGIN = 16
CAGE_EXTRUSION = 0.05  # Push ray origin out so it hits high-poly (tune if bake is wrong)
USE_AUTO_SMOOTH = True
AUTO_SMOOTH_ANGLE_DEGREES = 60
WEIGHTED_NORMAL_STRENGTH = 50
APPLY_WEIGHTED_NORMAL = True
KEEP_SHARP_EDGES = True


def clear_scene_selection():
    bpy.ops.object.select_all(action="DESELECT")


def get_selected_meshes():
    return [obj for obj in bpy.context.selected_objects if obj.type == "MESH"]


def ensure_uv_for_bake(obj):
    """Ensure object has a UV layer for baking; add one if missing."""
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="UVMap")
    return obj.data.uv_layers.active


def decimate_mesh(obj, ratio):
    """Add Decimate modifier with given ratio and apply it. Returns same object (modified)."""
    mod = obj.modifiers.new(name="Decimate", type="DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = max(0.01, min(1.0, ratio))
    # Keep quads where possible; triangulation during decimation can make shading noisier.
    mod.use_collapse_triangulate = False
    # Apply in edit mode would be cleaner; for script we use context override if needed
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def duplicate_as_low_poly(high_poly_obj, suffix):
    """Duplicate object, decimate it, name it with suffix. Returns (high_poly, low_poly)."""
    clear_scene_selection()
    high_poly_obj.select_set(True)
    bpy.context.view_layer.objects.active = high_poly_obj
    bpy.ops.object.duplicate()
    low_poly_obj = bpy.context.active_object
    low_poly_obj.name = high_poly_obj.name + suffix
    low_poly_obj.data = low_poly_obj.data.copy()  # Single-user copy of mesh data
    decimate_mesh(low_poly_obj, DECIMATE_RATIO)
    return high_poly_obj, low_poly_obj


def improve_low_poly_shading(obj):
    """Set shading flags and optionally add weighted normals for cleaner surface response."""
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()
    if USE_AUTO_SMOOTH and hasattr(obj.data, "use_auto_smooth"):
        obj.data.use_auto_smooth = True
        obj.data.auto_smooth_angle = AUTO_SMOOTH_ANGLE_DEGREES * (3.141592653589793 / 180.0)
    if APPLY_WEIGHTED_NORMAL:
        wn = obj.modifiers.new(name="WeightedNormal", type="WEIGHTED_NORMAL")
        wn.weight = WEIGHTED_NORMAL_STRENGTH
        wn.keep_sharp = KEEP_SHARP_EDGES
        bpy.ops.object.modifier_apply(modifier=wn.name)


def prepare_materials_for_bake(low_poly_obj, image):
    """Give low-poly a material with an image texture node for Normal bake target."""
    mat_name = low_poly_obj.name + "_BakeMat"
    mat = bpy.data.materials.get(mat_name)
    if mat is None:
        mat = bpy.data.materials.new(name=mat_name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.location = (-400, 0)
    bsdf.location = (-200, 0)
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if low_poly_obj.data.materials:
        low_poly_obj.data.materials[0] = mat
    else:
        low_poly_obj.data.materials.append(mat)
    return mat


def bake_normals_to_low_poly(high_poly_obj, low_poly_obj, image):
    """Bake normals from high-poly to low-poly's UV layout into image."""
    # Ensure UVs on low-poly
    ensure_uv_for_bake(low_poly_obj)
    prepare_materials_for_bake(low_poly_obj, image)
    # Select: high-poly selected, low-poly active (receiver)
    clear_scene_selection()
    high_poly_obj.select_set(True)
    low_poly_obj.select_set(True)
    bpy.context.view_layer.objects.active = low_poly_obj
    # Use low-poly's first material and first image node as bake target
    mat = low_poly_obj.data.materials[0]
    tex_node = None
    for n in mat.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image == image:
            tex_node = n
            break
    if not tex_node:
        return
    mat.node_tree.nodes.active = tex_node
    # Bake settings
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.bake_type = "NORMAL"
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.margin = BAKE_MARGIN
    scene.render.bake.use_clear = True
    scene.render.bake.cage_extrusion = CAGE_EXTRUSION
    scene.render.bake.normal_space = "TANGENT"
    scene.render.bake.normal_r = "POS_X"
    scene.render.bake.normal_g = "POS_Y"
    scene.render.bake.normal_b = "POS_Z"
    bpy.ops.object.bake(type="NORMAL")
    return image


def process_selected():
    """Decimate selected meshes, create low-poly copies, bake normals."""
    meshes = get_selected_meshes()
    if not meshes:
        print("No mesh objects selected. Select one or more high-poly meshes and run again.")
        return
    orig_active = bpy.context.view_layer.objects.active
    for high in meshes:
        if high.data.users > 1:
            high.data = high.data.copy()
        high_poly, low_poly = duplicate_as_low_poly(high, OUTPUT_SUFFIX)
        improve_low_poly_shading(low_poly)
        ensure_uv_for_bake(low_poly)
        img_name = low_poly.name + "_Normal"
        image = bpy.data.images.get(img_name)
        if image is None:
            image = bpy.data.images.new(
                img_name,
                width=NORMAL_MAP_SIZE,
                height=NORMAL_MAP_SIZE,
                alpha=False,
                float_buffer=False,
            )
        else:
            image.generated_width = NORMAL_MAP_SIZE
            image.generated_height = NORMAL_MAP_SIZE
        bake_normals_to_low_poly(high_poly, low_poly, image)
        print("Processed: %s -> %s, baked to %s" % (high_poly.name, low_poly.name, img_name))
    if orig_active:
        bpy.context.view_layer.objects.active = orig_active
    clear_scene_selection()
    print("Done. Low-poly objects have suffix '%s'. Normal maps are in Image Editor." % OUTPUT_SUFFIX)


if __name__ == "__main__":
    process_selected()
