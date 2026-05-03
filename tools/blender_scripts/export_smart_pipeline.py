"""
Smart pipeline export: export selected objects or "Export" collection to Unity as FBX.
Naming convention: *_VIS = visible mesh, *_COL = collider mesh.
Unity post-processor will add MeshCollider to _COL children and create prefabs.
Run in Blender: select objects or ensure "Export" collection exists, then run script.
"""
import bpy
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, "..", ".."))
EXPORT_FOLDER = os.path.join(REPO_ROOT, "RoobsWorld", "Assets", "Art", "Models")

COLLECTION_NAME = "Export"


def get_objects_to_export():
    """Selected mesh objects, or all mesh objects in 'Export' collection."""
    selected = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    if selected:
        return selected
    coll = bpy.data.collections.get(COLLECTION_NAME)
    if coll:
        return [o for o in coll.objects if o.type == "MESH"]
    print("No mesh objects selected and no 'Export' collection. Select meshes or add to Export collection.")
    return []


def export_smart():
    objects = get_objects_to_export()
    if not objects:
        return

    os.makedirs(EXPORT_FOLDER, exist_ok=True)
    for obj in objects:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        filepath = os.path.join(EXPORT_FOLDER, obj.name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=filepath,
            use_selection=True,
            apply_unit_scale=True,
            bake_space_transform=True,
        )
        print(f"Exported: {obj.name} -> {filepath}")

    print(f"Exported {len(objects)} object(s) to {EXPORT_FOLDER}")
    print("Unity: Models under Assets/Art/Models get prefabs; children with '_COL' in name get MeshCollider.")


if __name__ == "__main__":
    export_smart()
