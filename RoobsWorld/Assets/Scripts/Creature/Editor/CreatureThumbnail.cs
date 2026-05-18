using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// Utility for rendering a temporary camera view of a creature into a thumbnail texture or PNG bytes.
    /// </summary>
    public static class CreatureThumbnail
    {
        private const int THUMBNAIL_SIZE = 256;
        private const int PART_THUMBNAIL_SIZE = 128;
        private const float CAMERA_DISTANCE = 3f;
        private const float CAMERA_HEIGHT = 1.5f;

        public static Texture2D Capture(GameObject creatureRoot)
        {
            if (creatureRoot == null) return null;

            var camObj = new GameObject("ThumbnailCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 1f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;

            camObj.transform.position = creatureRoot.transform.position
                + new Vector3(0, CAMERA_HEIGHT, CAMERA_DISTANCE);
            camObj.transform.LookAt(creatureRoot.transform.position + Vector3.up * 0.5f);

            var rt = new RenderTexture(THUMBNAIL_SIZE, THUMBNAIL_SIZE, 24);
            cam.targetTexture = rt;
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(THUMBNAIL_SIZE, THUMBNAIL_SIZE, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, THUMBNAIL_SIZE, THUMBNAIL_SIZE), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);

            return tex;
        }

        public static byte[] CaptureAsPNG(GameObject creatureRoot)
        {
            var tex = Capture(creatureRoot);
            if (tex == null) return null;
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        public static Sprite CapturePartPreview(CreaturePartDef def, string partId, int size = PART_THUMBNAIL_SIZE)
        {
            var instance = CreatePartPreviewInstance(def, partId);
            if (instance == null)
                return null;

            size = Mathf.Clamp(size, 64, 512);
            var renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            var root = new GameObject($"PartPreviewRoot_{partId}");
            root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = new Vector3(5000f, 5000f, 5000f);

            RenderTexture previousActive = RenderTexture.active;
            try
            {
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(18f, -26f, 0f);
                SetHideFlagsRecursive(instance, HideFlags.HideAndDontSave);

                if (!TryCalculateEnabledRendererBounds(instance, out var bounds))
                    return null;

                var cameraObj = new GameObject("PartPreviewCamera");
                cameraObj.hideFlags = HideFlags.HideAndDontSave;
                cameraObj.transform.SetParent(root.transform, false);
                var cam = cameraObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 0f);
                cam.targetTexture = renderTexture;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 80f;
                cam.fieldOfView = 30f;
                cam.allowHDR = false;

                AddPreviewLight(root.transform, "PartPreviewKeyLight", Quaternion.Euler(38f, -28f, 0f), 1.25f);
                AddPreviewLight(root.transform, "PartPreviewFillLight", Quaternion.Euler(-18f, 142f, 0f), 0.42f);

                float radius = Mathf.Max(0.12f, bounds.extents.magnitude);
                Vector3 center = bounds.center;
                float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float distance = radius / Mathf.Max(0.1f, Mathf.Sin(halfFovRad)) * 1.35f;
                Vector3 camOffset = new Vector3(0.95f, 0.72f, -1.35f).normalized * distance;
                cam.transform.position = center + camOffset;
                cam.transform.LookAt(center);
                cam.Render();

                RenderTexture.active = renderTexture;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();

                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            finally
            {
                RenderTexture.active = previousActive;
                UnityObjectUtil.Destroy(renderTexture);
                UnityObjectUtil.Destroy(root);
            }
        }

        public static GameObject CreatePartPreviewInstance(CreaturePartDef def, string partId)
        {
            if (def == null && string.IsNullOrWhiteSpace(partId))
                return null;

            string safePartId = !string.IsNullOrWhiteSpace(partId) ? partId : def.PartId;
            GameObject prefab = def != null ? def.ModelPrefab : null;
            if (prefab == null && !string.IsNullOrWhiteSpace(safePartId))
                prefab = BlenderAssetLibrary.LoadCreaturePartPrefab(safePartId);
            if (prefab == null && !string.IsNullOrWhiteSpace(safePartId))
                prefab = Resources.Load<GameObject>($"Models/CreatureParts/{safePartId}");
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab);
            instance.name = $"PartPreview_{safePartId}";
            BlenderAssetLibrary.DisableColliders(instance);
            BlenderAssetLibrary.HideGeneratedColliderMeshes(instance);

            Vector3 defaultScale = def != null && def.DefaultScale != Vector3.zero
                ? def.DefaultScale
                : Vector3.one;
            instance.transform.localScale = defaultScale;
            return instance;
        }

        private static void AddPreviewLight(Transform parent, string name, Quaternion rotation, float intensity)
        {
            var lightObj = new GameObject(name);
            lightObj.hideFlags = HideFlags.HideAndDontSave;
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.rotation = rotation;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
        }

        private static bool TryCalculateEnabledRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds(root != null ? root.transform.position : Vector3.zero, Vector3.zero);
            if (root == null)
                return false;

            bool hasBounds = false;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void SetHideFlagsRecursive(GameObject obj, HideFlags hideFlags)
        {
            if (obj == null)
                return;

            obj.hideFlags = hideFlags;
            var transforms = obj.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                    transforms[i].gameObject.hideFlags = hideFlags;
            }
        }
    }
}
