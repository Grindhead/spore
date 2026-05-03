#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Applies consistent import settings to FBX models (e.g. from Blender).
    /// Ensures unit scale, materials, and no animation by default.
    /// </summary>
    public class ModelImportSettings : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importAnimation = false;
        }
    }
}
#endif
