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
            {
                if (cached != null)
                    return cached;

                // Retry if a previous attempt stored null (e.g. before assets became available).
                PrefabCache.Remove(resourcePath);
            }

            var loaded = Resources.Load<GameObject>(resourcePath);
            if (loaded != null)
                PrefabCache[resourcePath] = loaded;
            return loaded;
        }

        public static void DisableColliders(GameObject root)
        {
            if (root == null) return;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                UnityObjectUtil.Destroy(colliders[i]);
            HideGeneratedColliderMeshes(root);
        }

        public static void HideGeneratedColliderMeshes(GameObject root)
        {
            if (root == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (IsGeneratedColliderMesh(renderer.transform))
                    renderer.enabled = false;
            }
        }

        private static bool IsGeneratedColliderMesh(Transform transform)
        {
            if (transform == null)
                return false;

            if (HasColliderMeshName(transform.name))
                return true;

            var meshFilter = transform.GetComponent<MeshFilter>();
            return meshFilter != null &&
                   meshFilter.sharedMesh != null &&
                   HasColliderMeshName(meshFilter.sharedMesh.name);
        }

        private static bool HasColliderMeshName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.EndsWith("_COL", System.StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("_COLLIDER", System.StringComparison.OrdinalIgnoreCase) ||
                   name.IndexOf("_COL_", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
