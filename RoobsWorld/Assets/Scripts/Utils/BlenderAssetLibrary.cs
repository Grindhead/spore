using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Utils
{
    /// <summary>
    /// Runtime helper for loading Blender-generated prefabs from Resources and applying shared visual operations.
    /// </summary>
    public static class BlenderAssetLibrary
    {
        private const string CellPartsResourceRoot = "Models/CellParts";
        private const string CreaturePartsResourceRoot = "Models/CreatureParts";

        private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();
        private static readonly MaterialPropertyBlock TintPropertyBlock = new MaterialPropertyBlock();

        public static GameObject LoadCellPartPrefab(string assetName)
        {
            return LoadPrefab($"{CellPartsResourceRoot}/{assetName}");
        }

        public static GameObject LoadCreaturePartPrefab(string assetName)
        {
            return LoadPrefab($"{CreaturePartsResourceRoot}/{assetName}");
        }

        private static GameObject LoadPrefab(string resourcePath)
        {
            if (PrefabCache.TryGetValue(resourcePath, out var cached))
                return cached;

            var loaded = Resources.Load<GameObject>(resourcePath);
            PrefabCache[resourcePath] = loaded;
            return loaded;
        }

        public static void DisableColliders(GameObject root)
        {
            if (root == null) return;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                UnityObjectUtil.Destroy(colliders[i]);
        }

        public static void TintRenderers(GameObject root, Color color, float emissionMultiplier = 0f)
        {
            if (root == null) return;
            color.a = 1f;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                renderer.GetPropertyBlock(TintPropertyBlock);
                bool changed = false;

                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                {
                    TintPropertyBlock.SetColor("_Color", color);
                    changed = true;
                }

                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    TintPropertyBlock.SetColor("_BaseColor", color);
                    changed = true;
                }

                if (emissionMultiplier > 0f && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    TintPropertyBlock.SetColor("_EmissionColor", color * emissionMultiplier);
                    changed = true;
                }

                if (changed)
                    renderer.SetPropertyBlock(TintPropertyBlock);
            }
        }
    }
}
