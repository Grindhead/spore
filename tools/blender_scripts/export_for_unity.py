"""
Export Blender collection "Export" to Unity as FBX.
Run in Blender: Scripting workspace, run script.
FBX files are written to RoobsWorld/Assets/Art/Models (relative to repo root).
"""
import bpy
import os

# Resolve path: this script lives in tools/blender_scripts; repo root is two levels up
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, "..", ".."))
EXPORT_FOLDER = os.path.join(REPO_ROOT, "RoobsWorld", "Assets", "Art", "Models")

COLLECTION_NAME = "Export"


def export_collection(collection_name=COLLECTION_NAME):
    collection = bpy.data.collections.get(collection_name)
    if not collection:
        print(f"Collection '{collection_name}' not found.")
        return

    os.makedirs(EXPORT_FOLDER, exist_ok=True)
    exported = 0

    for obj in collection.objects:
        if obj.type != "MESH":
            continue

        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)

        filepath = os.path.join(EXPORT_FOLDER, obj.name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=filepath,
            use_selection=True,
            apply_unit_scale=True,
            bake_space_transform=True,
        )
        exported += 1
        print(f"Exported: {obj.name} -> {filepath}")

    print(f"Done. Exported {exported} mesh(es) to {EXPORT_FOLDER}")


if __name__ == "__main__":
    export_collection(COLLECTION_NAME)
