using System.Collections.Generic;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Creature
{
    public readonly struct CreatureModelBuildResult
    {
        public readonly Transform BodyRoot;
        public readonly Transform PartsRoot;
        public readonly Transform[] SpineAnchors;
        public readonly Transform[] PartRoots;
        public readonly Bounds VisualBounds;

        public CreatureModelBuildResult(
            Transform bodyRoot,
            Transform partsRoot,
            Transform[] spineAnchors,
            Transform[] partRoots,
            Bounds visualBounds)
        {
            BodyRoot = bodyRoot;
            PartsRoot = partsRoot;
            SpineAnchors = spineAnchors;
            PartRoots = partRoots;
            VisualBounds = visualBounds;
        }

        public Transform GetSpineAnchor(int attachBone)
        {
            if (SpineAnchors == null || SpineAnchors.Length == 0)
                return null;
            return SpineAnchors[Mathf.Clamp(attachBone, 0, SpineAnchors.Length - 1)];
        }
    }

    /// <summary>
    /// Builds the shared modular creature visual used by gameplay, previews, and editor tooling.
    /// </summary>
    public static class CreatureModelBuilder
    {
        public const string BodyRootName = "Body";
        public const string PartsRootName = "Parts";
        public const string SpineAnchorPrefix = "SpineAnchor_";

        private const int DefaultSpineSegments = 3;
        private const int MaxSpineSegments = 8;
        private const float SpineSpacing = 0.42f;

        public static CreatureModelBuildResult Build(
            GameObject root,
            CreatureDNA dna,
            IReadOnlyDictionary<string, CreaturePartDef> partCatalog = null)
        {
            if (root == null)
                throw new System.ArgumentNullException(nameof(root));

            var safeDna = dna ?? CreatureDNA.CreateDefault();
            var colors = safeDna.Colors ?? new CreatureColors();
            int segmentCount = GetSafeSpineSegmentCount(safeDna);
            Vector3 bodyScale = GetSafeBodyScale(safeDna);
            float frontPull = GetSafeFrontPull(safeDna);

            var bodyRoot = new GameObject(BodyRootName).transform;
            bodyRoot.SetParent(root.transform, false);

            Color primary = ProceduralTextureFactory.Opaque(colors.PrimaryColor);
            Color secondary = ProceduralTextureFactory.Opaque(colors.SecondaryColor);
            Color accent = ProceduralTextureFactory.Opaque(colors.AccentColor);
            var skinTexture = ProceduralTextureFactory.CreateCreatureSkinTexture(primary, secondary);
            var accentMaterial = TempAssetBuilder.CreateTempMaterial(
                Color.Lerp(accent, Color.white, 0.18f),
                "CreatureAccentMat");
            var anchors = BuildSpine(bodyRoot, segmentCount, bodyScale, frontPull, skinTexture, primary, secondary, accentMaterial);

            var partsRoot = new GameObject(PartsRootName).transform;
            partsRoot.SetParent(root.transform, false);

            var partRoots = BuildParts(partsRoot, safeDna, anchors, colors, partCatalog);
            var bounds = CalculateVisualBounds(root);
            return new CreatureModelBuildResult(bodyRoot, partsRoot, anchors, partRoots, bounds);
        }

        public static Vector3 GetSpineAnchorLocalPosition(int attachBone, int spineSegments)
        {
            int count = Mathf.Clamp(spineSegments, 1, MaxSpineSegments);
            int index = Mathf.Clamp(attachBone, 0, count - 1);
            float centeredFrontToBack = (count - 1) * 0.5f - index;
            return new Vector3(0f, 0.55f, centeredFrontToBack * SpineSpacing);
        }

        private static int GetSafeSpineSegmentCount(CreatureDNA dna)
        {
            if (dna?.Body == null)
                return DefaultSpineSegments;
            return Mathf.Clamp(dna.Body.SpineSegments, 1, MaxSpineSegments);
        }

        private static Vector3 GetSafeBodyScale(CreatureDNA dna)
        {
            Vector3 scale = dna?.Body?.BodyScale ?? Vector3.one;
            return new Vector3(
                Mathf.Clamp(scale.x, 0.5f, 2.5f),
                Mathf.Clamp(scale.y, 0.5f, 2.5f),
                Mathf.Clamp(scale.z, 0.5f, 2.5f));
        }

        private static float GetSafeFrontPull(CreatureDNA dna)
        {
            return Mathf.Clamp(dna?.Body?.FrontPull ?? 0f, -0.3f, 0.8f);
        }

        private static Transform[] BuildSpine(
            Transform bodyRoot,
            int segmentCount,
            Vector3 bodyScale,
            float frontPull,
            Texture2D skinTexture,
            Color primary,
            Color secondary,
            Material accentMaterial)
        {
            var anchors = new Transform[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                float t = segmentCount <= 1 ? 0.5f : i / (float)(segmentCount - 1);
                var anchor = new GameObject($"{SpineAnchorPrefix}{i}").transform;
                anchor.SetParent(bodyRoot, false);
                anchor.localPosition = GetSpineAnchorLocalPosition(i, segmentCount);
                if (i == 0)
                    anchor.localPosition += Vector3.forward * frontPull;
                anchors[i] = anchor;

                var segment = new GameObject($"Spine_{i}");
                segment.transform.SetParent(anchor, false);
                segment.transform.localScale = GetSegmentScale(t, i, segmentCount, bodyScale, frontPull);

                var mf = segment.AddComponent<MeshFilter>();
                mf.mesh = CreaturePartMeshFactory.CreateSpineSegment(0.5f);

                var mr = segment.AddComponent<MeshRenderer>();
                Color segmentColor = ProceduralTextureFactory.Opaque(Color.Lerp(primary, secondary, t * 0.75f));
                mr.sharedMaterial = ProceduralTextureFactory.CreateMaterialWithTexture(skinTexture, segmentColor);

                AddBodyRidge(anchor, i, segmentCount, accentMaterial);
            }

            return anchors;
        }

        private static Vector3 GetSegmentScale(float t, int index, int count, Vector3 bodyScale, float frontPull)
        {
            float fullness = Mathf.Clamp01(1f - Mathf.Abs(t - 0.42f) * 1.65f);
            float width = Mathf.Lerp(0.48f, 0.82f, fullness);
            float height = Mathf.Lerp(0.42f, 0.66f, fullness);
            float length = Mathf.Lerp(0.64f, 0.76f, fullness);

            if (index == 0)
            {
                width *= 0.82f;
                height *= 1.04f;
                length *= 0.86f;
            }
            else if (index == count - 1)
            {
                width *= 0.72f;
                height *= 0.9f;
                length *= 0.96f;
            }

            if (index == 0)
                length *= 1f + Mathf.Max(0f, frontPull * 0.9f);

            width *= bodyScale.x;
            height *= bodyScale.y;
            length *= bodyScale.z;

            return new Vector3(width, height, length);
        }

        private static void AddBodyRidge(Transform anchor, int index, int count, Material accentMaterial)
        {
            if (count < 2 || index == count - 1)
                return;

            float t = count <= 1 ? 0.5f : index / (float)(count - 1);
            float ridgeScale = Mathf.Lerp(0.055f, 0.035f, t);
            var ridge = CreateMeshObject(
                $"DorsalRidge_{index}",
                CreaturePartMeshFactory.CreateSpike(0.1f, 0.028f),
                accentMaterial);
            ridge.transform.SetParent(anchor, false);
            ridge.transform.localPosition = new Vector3(0f, 0.34f, -0.03f);
            ridge.transform.localScale = Vector3.one * ridgeScale / 0.05f;
        }

        private static Transform[] BuildParts(
            Transform partsRoot,
            CreatureDNA dna,
            Transform[] anchors,
            CreatureColors colors,
            IReadOnlyDictionary<string, CreaturePartDef> partCatalog)
        {
            if (dna.Parts == null || dna.Parts.Count == 0)
                return new Transform[0];

            var partRoots = new List<Transform>(dna.Parts.Count);
            for (int i = 0; i < dna.Parts.Count; i++)
            {
                var placement = dna.Parts[i];
                if (placement == null || string.IsNullOrWhiteSpace(placement.PartId))
                    continue;

                CreaturePartDef partDef = null;
                if (partCatalog != null)
                    partCatalog.TryGetValue(placement.PartId, out partDef);

                GameObject partObj = CreatePartObject(placement, partDef, colors);
                Transform anchor = GetAnchor(anchors, placement.AttachBone);
                Transform parent = anchor != null ? anchor : partsRoot;
                partObj.name = $"Part_{placement.PartId}_{i:00}";
                partObj.transform.SetParent(parent, false);
                partObj.transform.localPosition = placement.Offset;
                partObj.transform.localRotation = GetAutoRotation(placement) * Quaternion.Euler(placement.Rotation);

                Vector3 defaultScale = partDef != null ? partDef.DefaultScale : GetDefaultScale(placement.PartId);
                partObj.transform.localScale = defaultScale * Mathf.Max(0.01f, placement.Scale);

                var mount = partObj.GetComponent<CreaturePartMount>() ?? partObj.AddComponent<CreaturePartMount>();
                mount.Initialize(placement, i);
                partRoots.Add(partObj.transform);
            }

            return partRoots.ToArray();
        }

        private static Transform GetAnchor(Transform[] anchors, int attachBone)
        {
            if (anchors == null || anchors.Length == 0)
                return null;
            return anchors[Mathf.Clamp(attachBone, 0, anchors.Length - 1)];
        }

        private static GameObject CreatePartObject(PartPlacement placement, CreaturePartDef partDef, CreatureColors colors)
        {
            if (partDef != null && partDef.ModelPrefab != null)
            {
                var prefabPart = Object.Instantiate(partDef.ModelPrefab);
                BlenderAssetLibrary.DisableColliders(prefabPart);
                BlenderAssetLibrary.TintRenderers(prefabPart, GetPartTint(placement.PartId, partDef.Category, colors), 0.04f);
                DecoratePart(prefabPart, placement.PartId, partDef.Category, colors);
                return prefabPart;
            }

            PartCategory category = ResolveCategory(placement.PartId, partDef);
            var partObj = CreateMeshObject(
                $"Part_{placement.PartId}",
                CreaturePartMeshFactory.GetPartMesh(placement.PartId),
                CreatePartMaterial(placement.PartId, category, colors));
            DecoratePart(partObj, placement.PartId, category, colors);
            return partObj;
        }

        private static Material CreatePartMaterial(string partId, PartCategory category, CreatureColors colors)
        {
            Color tint = GetPartTint(partId, category, colors);
            var mat = TempAssetBuilder.CreateTempMaterial(tint, $"{partId}_Mat");
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", category == PartCategory.Eye ? 0.72f : 0.38f);
            return mat;
        }

        private static Color GetPartTint(string partId, PartCategory category, CreatureColors colors)
        {
            Color primary = ProceduralTextureFactory.Opaque(colors.PrimaryColor);
            Color secondary = ProceduralTextureFactory.Opaque(colors.SecondaryColor);
            Color accent = ProceduralTextureFactory.Opaque(colors.AccentColor);

            switch (category)
            {
                case PartCategory.Eye:
                    return Color.Lerp(Color.white, accent, 0.18f);
                case PartCategory.Mouth:
                    return Color.Lerp(new Color(0.76f, 0.18f, 0.16f), secondary, 0.18f);
                case PartCategory.Horn:
                case PartCategory.Spike:
                    return Color.Lerp(new Color(0.84f, 0.8f, 0.68f), accent, 0.25f);
                case PartCategory.Shell:
                    return Color.Lerp(secondary, new Color(0.46f, 0.36f, 0.2f), 0.42f);
                case PartCategory.Wing:
                case PartCategory.Fin:
                    return Color.Lerp(primary, accent, 0.5f);
                default:
                    return Color.Lerp(primary, secondary, 0.38f);
            }
        }

        private static PartCategory ResolveCategory(string partId, CreaturePartDef partDef)
        {
            if (partDef != null)
                return partDef.Category;
            if (partId.StartsWith("mouth")) return PartCategory.Mouth;
            if (partId.StartsWith("eye")) return PartCategory.Eye;
            if (partId.StartsWith("leg")) return PartCategory.Leg;
            if (partId.StartsWith("arm")) return PartCategory.Arm;
            if (partId.StartsWith("wing")) return PartCategory.Wing;
            if (partId.StartsWith("tail")) return PartCategory.Tail;
            if (partId.StartsWith("horn")) return PartCategory.Horn;
            if (partId.StartsWith("fin")) return PartCategory.Fin;
            if (partId.StartsWith("shell")) return PartCategory.Shell;
            return PartCategory.Spike;
        }

        private static Vector3 GetDefaultScale(string partId)
        {
            if (partId.StartsWith("eye")) return Vector3.one * 0.82f;
            if (partId.StartsWith("mouth")) return Vector3.one * 0.9f;
            if (partId.StartsWith("wing")) return Vector3.one * 1.05f;
            if (partId.StartsWith("shell")) return Vector3.one * 1.12f;
            return Vector3.one;
        }

        private static Quaternion GetAutoRotation(PartPlacement placement)
        {
            string partId = placement.PartId ?? string.Empty;
            bool leftSide = placement.Offset.x < -0.001f;

            if ((partId.StartsWith("wing") || partId.StartsWith("fin")) && leftSide)
                return Quaternion.Euler(0f, 180f, 0f);

            if (partId.StartsWith("arm"))
                return Quaternion.Euler(0f, 0f, leftSide ? -18f : 18f);

            return Quaternion.identity;
        }

        private static void DecoratePart(GameObject partObj, string partId, PartCategory category, CreatureColors colors)
        {
            if (partObj == null)
                return;

            var accentMaterial = TempAssetBuilder.CreateTempMaterial(
                Color.Lerp(ProceduralTextureFactory.Opaque(colors.AccentColor), Color.white, 0.16f),
                $"{partId}_AccentMat");
            var darkMaterial = TempAssetBuilder.CreateTempMaterial(new Color(0.04f, 0.035f, 0.03f), $"{partId}_DarkMat");
            var boneMaterial = TempAssetBuilder.CreateTempMaterial(
                Color.Lerp(new Color(0.88f, 0.84f, 0.72f), ProceduralTextureFactory.Opaque(colors.AccentColor), 0.12f),
                $"{partId}_BoneMat");

            switch (category)
            {
                case PartCategory.Eye:
                    AddEyeDetails(partObj.transform, accentMaterial, darkMaterial);
                    break;
                case PartCategory.Mouth:
                    AddTeeth(partObj.transform, boneMaterial);
                    break;
                case PartCategory.Leg:
                case PartCategory.Arm:
                    AddLimbDetails(partObj.transform, category, boneMaterial);
                    break;
                case PartCategory.Wing:
                case PartCategory.Fin:
                    AddMembraneRibs(partObj.transform, accentMaterial);
                    break;
                case PartCategory.Tail:
                    AddTailTip(partObj.transform, accentMaterial);
                    break;
                case PartCategory.Shell:
                    AddShellRidges(partObj.transform, accentMaterial);
                    break;
                case PartCategory.Horn:
                case PartCategory.Spike:
                    AddBaseSocket(partObj.transform, boneMaterial);
                    break;
            }
        }

        private static void AddEyeDetails(Transform parent, Material irisMaterial, Material pupilMaterial)
        {
            var iris = CreateMeshObject("Iris", CreaturePartMeshFactory.CreateEye(0.048f), irisMaterial);
            iris.transform.SetParent(parent, false);
            iris.transform.localPosition = Vector3.forward * 0.105f;
            iris.transform.localScale = new Vector3(1f, 0.78f, 0.22f);

            var pupil = CreateMeshObject("Pupil", CreaturePartMeshFactory.CreatePupil(0.024f), pupilMaterial);
            pupil.transform.SetParent(parent, false);
            pupil.transform.localPosition = Vector3.forward * 0.135f;
            pupil.transform.localScale = new Vector3(1f, 0.82f, 0.24f);
        }

        private static void AddTeeth(Transform parent, Material toothMaterial)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.07f;
                var upper = CreateMeshObject($"UpperTooth_{i}", CreaturePartMeshFactory.CreateSpike(0.055f, 0.014f), toothMaterial);
                upper.transform.SetParent(parent, false);
                upper.transform.localPosition = new Vector3(x, 0.045f, 0.09f);
                upper.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

                var lower = CreateMeshObject($"LowerTooth_{i}", CreaturePartMeshFactory.CreateSpike(0.045f, 0.012f), toothMaterial);
                lower.transform.SetParent(parent, false);
                lower.transform.localPosition = new Vector3(x * 0.86f, -0.042f, 0.08f);
            }
        }

        private static void AddLimbDetails(Transform parent, PartCategory category, Material detailMaterial)
        {
            var joint = CreateMeshObject("JointPad", CreaturePartMeshFactory.CreateShell(0.055f), detailMaterial);
            joint.transform.SetParent(parent, false);
            joint.transform.localPosition = Vector3.down * 0.27f;
            joint.transform.localScale = new Vector3(0.82f, 0.55f, 0.82f);

            var tip = CreateMeshObject(category == PartCategory.Leg ? "FootPad" : "HandPad", CreaturePartMeshFactory.CreateShell(0.075f), detailMaterial);
            tip.transform.SetParent(parent, false);
            tip.transform.localPosition = Vector3.down * (category == PartCategory.Leg ? 0.5f : 0.38f);
            tip.transform.localScale = new Vector3(1.35f, 0.42f, 0.86f);
        }

        private static void AddMembraneRibs(Transform parent, Material ribMaterial)
        {
            for (int i = 0; i < 3; i++)
            {
                var rib = CreateMeshObject($"MembraneRib_{i}", CreaturePartMeshFactory.CreateSpike(0.22f, 0.008f), ribMaterial);
                rib.transform.SetParent(parent, false);
                rib.transform.localPosition = new Vector3(0.12f + i * 0.14f, 0.014f, -0.09f + i * 0.09f);
                rib.transform.localRotation = Quaternion.Euler(0f, 0f, -78f + i * 10f);
                rib.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            }
        }

        private static void AddTailTip(Transform parent, Material tipMaterial)
        {
            var fin = CreateMeshObject("TailFin", CreaturePartMeshFactory.CreateFin(0.18f, 0.22f), tipMaterial);
            fin.transform.SetParent(parent, false);
            fin.transform.localPosition = new Vector3(0f, 0.01f, -0.45f);
            fin.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            fin.transform.localScale = new Vector3(0.82f, 0.72f, 0.82f);
        }

        private static void AddShellRidges(Transform parent, Material ridgeMaterial)
        {
            for (int i = 0; i < 4; i++)
            {
                var ridge = CreateMeshObject($"ShellRidge_{i}", CreaturePartMeshFactory.CreateSpike(0.08f, 0.018f), ridgeMaterial);
                ridge.transform.SetParent(parent, false);
                ridge.transform.localPosition = new Vector3(0f, 0.11f, -0.12f + i * 0.08f);
                ridge.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                ridge.transform.localScale = Vector3.one * (0.78f - i * 0.06f);
            }
        }

        private static void AddBaseSocket(Transform parent, Material socketMaterial)
        {
            var socket = CreateMeshObject("BaseSocket", CreaturePartMeshFactory.CreateShell(0.062f), socketMaterial);
            socket.transform.SetParent(parent, false);
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localScale = new Vector3(1.45f, 0.35f, 1.45f);
        }

        private static GameObject CreateMeshObject(string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            return obj;
        }

        private static Bounds CalculateVisualBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
