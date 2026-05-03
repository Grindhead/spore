"""
Generate LOD meshes for selected objects: duplicate, add Decimate modifier, name _LOD1, _LOD2, _LOD3.
Run in Blender on selected mesh objects. Optionally export all (including base as LOD0) to Unity.
Unity CreateLODGroups tool can then build LODGroup from _LOD0/_LOD1 naming.
"""
import bpy

LOD_RATIOS = [0.5, 0.25, 0.1]  # LOD1, LOD2, LOD3


def create_lods_for_object(obj, ratios=None):
    if obj.type != "MESH":
        return []
    ratios = ratios or LOD_RATIOS
    lods_created = []

    for i, ratio in enumerate(ratios):
        lod = obj.copy()
        lod.data = obj.data.copy()
        bpy.context.collection.objects.link(lod)
        lod.name = f"{obj.name}_LOD{i + 1}"

        decimate = lod.modifiers.new("Decimate", type="DECIMATE")
        decimate.ratio = ratio
        # Apply modifier so export has reduced mesh (optional; comment out to keep modifier for manual tweak)
        try:
            bpy.context.view_layer.objects.active = lod
            bpy.ops.object.modifier_apply(modifier=decimate.name)
        except Exception:
            pass
        lods_created.append(lod)

    return lods_created


def create_lods_for_selected():
    selected = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    if not selected:
        print("No mesh objects selected. Select one or more meshes and run again.")
        return

    total = 0
    for obj in selected:
        lods = create_lods_for_object(obj)
        total += len(lods)
        print(f"{obj.name}: created {len(lods)} LOD(s)")

    print(f"Created {total} LOD mesh(es). Export via export_for_unity.py (use collection 'Export') or export_smart_pipeline.py.")


if __name__ == "__main__":
    create_lods_for_selected()
