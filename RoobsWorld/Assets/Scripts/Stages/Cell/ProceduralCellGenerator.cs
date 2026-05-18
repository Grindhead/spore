using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Generates cell visuals from a genome: body (size, color), spikes, and flagella.
    /// Step 7 of cell-stage evolution roadmap (memory-bank/cellStageImplementationSteps.md).
    /// </summary>
    public static class ProceduralCellGenerator
    {
        private const float BaseScale = 0.5f;
        private const float CellSphereRadius = 0.5f;
        private const int MaxSpikes = 8;
        private const int MaxFlagella = 6;
        private const float SpikeLength = 0.12f;
        private const float SpikeBaseRadius = 0.025f;
        private const float FlagellaLength = 0.28f;
        private const float FlagellaBaseRadius = 0.015f;
        private const float PartSurfaceInset = 0.035f;
        private const float LoadoutPartSurfaceInset = 0.03f;
        private const float DetailSurfaceY = 0.47f;
        private const float DetailSurfaceRadius = 0.34f;
        private static Shader s_cellUnlitShader;

        // Trait signature colors — blended onto body color when a trait is active
        private static readonly Color TraitColorPhotosynthetic = new Color(0.1f, 0.9f, 0.2f);  // vivid green
        private static readonly Color TraitColorToxic          = new Color(0.7f, 0.1f, 0.9f);  // purple
        private static readonly Color TraitColorArmoured       = new Color(0.6f, 0.6f, 0.6f);  // steel gray
        private static readonly Color TraitColorRegenerative   = new Color(0.1f, 0.9f, 0.9f);  // cyan
        private static readonly Color TraitColorElectric       = new Color(1.0f, 0.9f, 0.1f);  // yellow

        /// <summary>
        /// Returns the body color blended toward trait signature colors.
        /// Each active trait nudges the color ~20% toward its tint; multiple traits stack.
        /// </summary>
        public static Color ApplyTraitTint(Color baseColor, GeneticTraits traits)
        {
            Color result = ProceduralTextureFactory.Opaque(baseColor);
            const float blend = 0.22f;
            if ((traits & GeneticTraits.Photosynthetic) != 0) result = Color.Lerp(result, TraitColorPhotosynthetic, blend);
            if ((traits & GeneticTraits.Toxic)          != 0) result = Color.Lerp(result, TraitColorToxic,          blend);
            if ((traits & GeneticTraits.Armoured)       != 0) result = Color.Lerp(result, TraitColorArmoured,       blend);
            if ((traits & GeneticTraits.Regenerative)   != 0) result = Color.Lerp(result, TraitColorRegenerative,   blend);
            if ((traits & GeneticTraits.Electric)       != 0) result = Color.Lerp(result, TraitColorElectric,       blend);
            return ProceduralTextureFactory.Opaque(result);
        }

        /// <summary>
        /// Creates a GameObject representing a cell from the given genome: body mesh (scale + color),
        /// optional spike and flagella child meshes. Caller can add Organism and other components to the returned root.
        /// </summary>
        public static GameObject CreateCellFromGenome(Vector3 position, Genome genome)
        {
            if (genome == null)
                genome = Genome.Default();

            float scale = BaseScale * Mathf.Max(0.1f, genome.Size);
            var root = new GameObject("Cell");
            root.transform.position = position;
            root.transform.localScale = Vector3.one * scale;

            Color displayColor = ApplyTraitTint(genome.Color, genome.Traits);

            EnsureProceduralBody(root, displayColor);
            var bodyCollider = root.AddComponent<SphereCollider>();
            bodyCollider.radius = CellSphereRadius;
            bodyCollider.isTrigger = true;

            int spikeCount = GetSpikeCount(genome.Spikes);
            int flagellaCount = GetFlagellaCount(genome.Flagella);

            for (int i = 0; i < spikeCount; i++)
                AddSpikeChild(root, i, spikeCount, displayColor);

            for (int i = 0; i < flagellaCount; i++)
                AddFlagellaChild(root, i, flagellaCount, displayColor);

            AddSporeLikeDetails(root, genome, displayColor);
            ApplyMenuMicrobeVisual(root, genome, displayColor);
            CellSwimVisual.Ensure(root, CellSwimVisual.SwimProfile.Npc);

            return root;
        }

        /// <summary>Updates the body color and spike/flagella visuals on an existing cell GameObject.</summary>
        public static void ApplyGenomeVisuals(GameObject root, Genome genome)
        {
            if (root == null || genome == null) return;

            Color displayColor = ApplyTraitTint(genome.Color, genome.Traits);
            EnsureProceduralBody(root, displayColor);
            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                ApplyRendererColor(mr, displayColor, 0.3f);
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child.name.StartsWith("Spike_") || child.name.StartsWith("Flagella_")
                    || child.name.StartsWith("Eye_") || child.name.StartsWith("Style_"))
                    UnityObjectUtil.Destroy(child.gameObject);
            }

            int spikeCount = GetSpikeCount(genome.Spikes);
            int flagellaCount = GetFlagellaCount(genome.Flagella);
            for (int i = 0; i < spikeCount; i++)
                AddSpikeChild(root, i, spikeCount, displayColor);

            for (int i = 0; i < flagellaCount; i++)
                AddFlagellaChild(root, i, flagellaCount, displayColor);

            AddSporeLikeDetails(root, genome, displayColor);
            ApplyMenuMicrobeVisual(root, genome, displayColor);
        }

        private static void EnsureProceduralBody(GameObject root, Color displayColor)
        {
            if (root == null)
                return;

            var mf = root.GetComponent<MeshFilter>();
            if (mf == null)
                mf = root.AddComponent<MeshFilter>();
            if (mf.sharedMesh == null)
                mf.sharedMesh = ProceduralMeshFactory.CreateCellMesh();

            var mr = root.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = root.AddComponent<MeshRenderer>();

            if (mr.sharedMaterial == null || !mr.sharedMaterial.name.StartsWith("CellStageBody"))
            {
                var mat = CreateFlatUnlitMaterial(displayColor, "CellStageBody");
                if (mat != null)
                {
                    var tex = ProceduralTextureFactory.CreateCreatureSkinTexture(displayColor, Color.Lerp(displayColor, Color.white, 0.22f));
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", tex);
                    mat.mainTexture = tex;
                }
                mr.sharedMaterial = mat;
            }

            ApplyRendererColor(mr, displayColor, 0.24f);
        }

        private static void ApplyRendererColor(Renderer renderer, Color color, float emissionMultiplier)
        {
            if (renderer == null)
                return;

            var mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (mat == null)
                return;

            SetMaterialColor(mat, color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissionMultiplier);
            }
        }

        private static Shader ResolveCellUnlitShader()
        {
            if (s_cellUnlitShader != null)
                return s_cellUnlitShader;

            s_cellUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (s_cellUnlitShader == null)
                s_cellUnlitShader = Shader.Find("Unlit/Texture");
            if (s_cellUnlitShader == null)
                s_cellUnlitShader = Shader.Find("Sprites/Default");
            if (s_cellUnlitShader == null)
                s_cellUnlitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (s_cellUnlitShader == null)
                s_cellUnlitShader = Shader.Find("Standard");
            return s_cellUnlitShader;
        }

        private static Material CreateFlatUnlitMaterial(Color color, string name)
        {
            var shader = ResolveCellUnlitShader();
            if (shader == null)
                return null;

            var mat = new Material(shader) { name = $"{name}_Mat" };
            SetMaterialColor(mat, color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0f);
            return mat;
        }

        private static void SetMaterialColor(Material mat, Color color)
        {
            if (mat == null)
                return;
            color = ProceduralTextureFactory.Opaque(color);

            mat.color = color;
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
        }

        /// <summary>
        /// Applies visual markers for unlocked player loadout parts (Flagella/Cilia/Spike/Jet).
        /// This is additive to genome-driven visuals and uses Blender assets when available.
        /// </summary>
        public static void ApplyLoadoutVisuals(GameObject root, CellPartLoadout loadout, Color bodyColor)
        {
            if (root == null) return;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child.name.StartsWith("Loadout_"))
                    UnityObjectUtil.Destroy(child.gameObject);
            }

            if (loadout == null) return;

            if (loadout.HasPart(CellPartType.Flagella))
                AddLoadoutRing(root, "Flagella", "cell_flagella", 4, CellSphereRadius * 1.02f, -0.03f, bodyColor, 0.08f);

            if (loadout.HasPart(CellPartType.Cilia))
                AddLoadoutRing(root, "Cilia", "cell_cilia", 8, CellSphereRadius * 1.05f, 0.06f, bodyColor, 0.08f);

            if (loadout.HasPart(CellPartType.Spike))
                AddLoadoutRing(root, "Spike", "cell_spike", 3, CellSphereRadius * 1.0f, 0.16f, bodyColor, 0.1f);

            if (loadout.HasPart(CellPartType.Jet))
                AddLoadoutJet(root, bodyColor);

            if (loadout.HasPart(CellPartType.HerbivoreMouth))
                AddLoadoutFrontMarker(root, "HerbivoreMouth", Color.Lerp(bodyColor, new Color(0.45f, 1f, 0.25f), 0.7f), 0.12f, 0.055f);

            if (loadout.HasPart(CellPartType.CarnivoreMouth))
                AddLoadoutFrontMarker(root, "CarnivoreMouth", Color.Lerp(bodyColor, new Color(1f, 0.28f, 0.18f), 0.7f), 0.16f, 0.045f);

            if (loadout.HasPart(CellPartType.OmnivoreMouth))
                AddLoadoutFrontMarker(root, "OmnivoreMouth", Color.Lerp(bodyColor, new Color(1f, 0.74f, 0.22f), 0.7f), 0.14f, 0.05f);

            if (loadout.HasPart(CellPartType.SuctionMouth))
                AddLoadoutFrontMarker(root, "SuctionMouth", Color.Lerp(bodyColor, new Color(0.22f, 0.9f, 1f), 0.65f), 0.18f, 0.035f);

            if (loadout.HasPart(CellPartType.PoisonVacuole))
                AddLoadoutRing(root, "PoisonVacuole", "cell_poison", 5, CellSphereRadius * 0.8f, 0.18f, new Color(0.8f, 0.2f, 1f), 0.16f);

            if (loadout.HasPart(CellPartType.ElectricNodule))
                AddLoadoutRing(root, "ElectricNodule", "cell_electric", 4, CellSphereRadius * 0.95f, 0.22f, new Color(1f, 0.95f, 0.18f), 0.2f);

            if (loadout.HasPart(CellPartType.ArmorPlate))
                AddLoadoutRing(root, "ArmorPlate", "cell_armor", 6, CellSphereRadius * 0.86f, 0.12f, new Color(0.72f, 0.78f, 0.82f), 0.08f);

            if (loadout.HasPart(CellPartType.SensorEye))
                AddLoadoutFrontMarker(root, "SensorEye", Color.Lerp(bodyColor, new Color(0.65f, 0.95f, 1f), 0.65f), 0.09f, 0.075f);

            int visualSeed = 7717 + MathUtils.StableHash(root.name) + Mathf.RoundToInt(root.transform.position.sqrMagnitude * 10f);
            MenuMicrobeVisuals.Apply(root, bodyColor, visualSeed, 5);
        }

        /// <summary>Number of spike visuals (0–MaxSpikes) from genome Spikes trait (0–10).</summary>
        public static int GetSpikeCount(float genomeSpikes)
        {
            return Mathf.Clamp(Mathf.RoundToInt(genomeSpikes * (MaxSpikes / 10f)), 0, MaxSpikes);
        }

        /// <summary>Number of flagella visuals (0–MaxFlagella) from genome Flagella trait (0–10).</summary>
        public static int GetFlagellaCount(float genomeFlagella)
        {
            return Mathf.Clamp(Mathf.RoundToInt(genomeFlagella * (MaxFlagella / 10f)), 0, MaxFlagella);
        }

        private static void AddSpikeChild(GameObject root, int index, int total, Color bodyColor)
        {
            float angle = total > 0 ? (2f * Mathf.PI * index / total) : 0f;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var child = new GameObject($"Spike_{index}");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Mathf.Max(0.01f, CellSphereRadius - PartSurfaceInset) * direction;
            child.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            child.transform.localScale = Vector3.one;
            var childMf = child.AddComponent<MeshFilter>();
            childMf.mesh = CreaturePartMeshFactory.CreateSpike(SpikeLength, SpikeBaseRadius);
            var childMr = child.AddComponent<MeshRenderer>();
            childMr.sharedMaterial = CreateFlatUnlitMaterial(
                Color.Lerp(bodyColor, Color.gray, 0.2f), $"Spike_{index}");
        }

        private static void AddFlagellaChild(GameObject root, int index, int total, Color bodyColor)
        {
            float angle = total > 0 ? (2f * Mathf.PI * index / total) : 0f;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var child = new GameObject($"Flagella_{index}");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Mathf.Max(0.01f, CellSphereRadius - PartSurfaceInset) * direction;
            child.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            child.transform.localScale = Vector3.one;
            var childMf = child.AddComponent<MeshFilter>();
            childMf.mesh = CreaturePartMeshFactory.CreateSpike(FlagellaLength, FlagellaBaseRadius);
            var childMr = child.AddComponent<MeshRenderer>();
            childMr.sharedMaterial = CreateFlatUnlitMaterial(
                Color.Lerp(bodyColor, Color.gray, 0.15f), $"Flagella_{index}");
        }

        private static void AddLoadoutRing(
            GameObject root,
            string partName,
            string prefabName,
            int count,
            float radius,
            float yOffset,
            Color bodyColor,
            float emission)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = count > 0 ? (2f * Mathf.PI * i / count) : 0f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                var holder = new GameObject($"Loadout_{partName}_{i}");
                holder.transform.SetParent(root.transform, false);
                holder.transform.localPosition = direction * Mathf.Max(0.01f, radius - LoadoutPartSurfaceInset) + Vector3.up * yOffset;
                holder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
                holder.transform.localScale = Vector3.one;
                var mf = holder.AddComponent<MeshFilter>();
                mf.mesh = CreaturePartMeshFactory.CreateSpike(0.16f, 0.02f);
                var mr = holder.AddComponent<MeshRenderer>();
                var matColor = Color.Lerp(bodyColor, Color.gray, 0.2f);
                mr.sharedMaterial = CreateFlatUnlitMaterial(matColor, $"Loadout{partName}_{i}");
                ApplyRendererColor(mr, matColor, emission);
            }
        }

        private static void AddLoadoutJet(GameObject root, Color bodyColor)
        {
            var holder = new GameObject("Loadout_Jet_0");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0f, -0.02f, -Mathf.Max(0.01f, (CellSphereRadius * 1.04f) - LoadoutPartSurfaceInset));
            holder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            holder.transform.localScale = Vector3.one;
            var mf = holder.AddComponent<MeshFilter>();
            mf.mesh = CreaturePartMeshFactory.CreateSpike(0.2f, 0.05f);
            var mr = holder.AddComponent<MeshRenderer>();
            var jetColor = Color.Lerp(bodyColor, Color.black, 0.15f);
            mr.sharedMaterial = CreateFlatUnlitMaterial(jetColor, "LoadoutJet");
            ApplyRendererColor(mr, jetColor, 0.12f);
        }

        private static void AddLoadoutFrontMarker(GameObject root, string partName, Color color, float width, float height)
        {
            var holder = new GameObject($"Loadout_{partName}_0");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0f, DetailSurfaceY + 0.04f, CellSphereRadius * 0.76f);
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = new Vector3(Mathf.Max(0.03f, width), Mathf.Max(0.02f, height), Mathf.Max(0.02f, width * 0.55f));

            var mf = holder.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(1f, 8);
            var mr = holder.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateFlatUnlitMaterial(color, $"Loadout{partName}");
            ApplyRendererColor(mr, color, 0.14f);
        }

        private static void AddSporeLikeDetails(GameObject root, Genome genome, Color displayColor)
        {
            if (root == null || genome == null)
                return;

            int seed = Mathf.Abs(Mathf.RoundToInt(
                genome.Speed * 17f + genome.Size * 31f + genome.Attack * 43f +
                genome.Digestion * 59f + genome.Flagella * 71f + genome.Spikes * 83f));

            AddBodyMarkings(root, seed, displayColor, genome);
            AddTopEyes(root, seed, displayColor, genome);
            AddPerimeterTendrils(root, seed, displayColor, genome);
            AddTraitBadges(root, displayColor, genome.Traits);
        }

        private static void ApplyMenuMicrobeVisual(GameObject root, Genome genome, Color displayColor)
        {
            if (root == null || genome == null)
                return;

            int seed = Mathf.Abs(Mathf.RoundToInt(
                genome.Speed * 97f + genome.Size * 131f + genome.Attack * 173f +
                genome.Digestion * 197f + genome.Flagella * 223f + genome.Spikes * 251f +
                ((int)genome.Traits * 283f)));
            int variant = Mathf.Clamp(
                1 + Mathf.RoundToInt(genome.Flagella * 0.35f + genome.Spikes * 0.25f + genome.TraitCount),
                1,
                7);
            MenuMicrobeVisuals.Apply(root, displayColor, 9000 + seed, variant);
        }

        private static void AddBodyMarkings(GameObject root, int seed, Color bodyColor, Genome genome)
        {
            Color markColor = Color.Lerp(bodyColor, Color.white, 0.35f);
            if (genome.Attack > genome.Digestion + 1.5f)
                markColor = Color.Lerp(bodyColor, new Color(1f, 0.72f, 0.3f), 0.55f);
            else if (genome.Digestion > genome.Attack + 1.5f)
                markColor = Color.Lerp(bodyColor, new Color(0.55f, 1f, 0.48f), 0.45f);

            bool stripeMode = (seed % 3) == 0;
            int count = stripeMode ? 4 : 5 + (seed % 5);
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                float angle = stripeMode
                    ? Mathf.Lerp(-0.85f, 0.85f, t)
                    : (seed * 0.37f + i * 2.39996f);
                float radius = stripeMode ? DetailSurfaceRadius * 0.55f : DetailSurfaceRadius * (0.28f + 0.58f * Mathf.Repeat(seed * 0.13f + i * 0.31f, 1f));
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, DetailSurfaceY + 0.012f, Mathf.Sin(angle) * radius);
                Vector3 scale = stripeMode
                    ? new Vector3(0.065f, 0.012f, 0.24f)
                    : Vector3.one * (0.045f + 0.018f * ((seed + i) % 3));

                var mark = new GameObject(stripeMode ? $"Style_Stripe_{i}" : $"Style_Spot_{i}");
                mark.transform.SetParent(root.transform, false);
                mark.transform.localPosition = pos;
                mark.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                mark.transform.localScale = scale;

                var mf = mark.AddComponent<MeshFilter>();
                mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(1f, 8);
                var mr = mark.AddComponent<MeshRenderer>();
                mr.sharedMaterial = CreateFlatUnlitMaterial(markColor, mark.name);
            }
        }

        private static void AddTopEyes(GameObject root, int seed, Color bodyColor, Genome genome)
        {
            int eyeCount = 2;
            if (genome.Speed >= 4.5f || genome.Flagella >= 6f) eyeCount = 3;
            if (genome.Attack >= 5.5f || genome.Size >= 2f) eyeCount = 4;
            if (genome.Digestion >= 6.5f && genome.Attack < 2f) eyeCount = 1;

            float spread = eyeCount == 1 ? 0f : Mathf.Min(0.28f, 0.1f + eyeCount * 0.045f);
            for (int i = 0; i < eyeCount; i++)
            {
                float offset = eyeCount == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(eyeCount - 1));
                float row = (i % 2 == 0) ? 0.18f : 0.27f;
                float radius = 0.066f + 0.01f * ((seed + i) % 2);
                Vector3 pos = new Vector3(offset, DetailSurfaceY + 0.035f, row);
                CreateTopEye(root.transform, pos, radius, $"Eye_Style_{i}", bodyColor);
            }
        }

        private static void AddPerimeterTendrils(GameObject root, int seed, Color bodyColor, Genome genome)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(genome.Flagella * 0.7f + genome.Speed * 0.25f), 3, 10);
            Color tendrilColor = Color.Lerp(bodyColor, new Color(1f, 0.63f, 0.48f), 0.55f);
            float backBias = genome.Attack > genome.Digestion ? -0.25f : 0.05f;

            for (int i = 0; i < count; i++)
            {
                float angle = (2f * Mathf.PI * i / count) + seed * 0.11f;
                if (i % 2 == 0) angle += backBias;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var tendril = new GameObject($"Style_Tendril_{i}");
                tendril.transform.SetParent(root.transform, false);
                tendril.transform.localPosition = direction * (CellSphereRadius * 0.92f) + Vector3.up * 0.02f;
                tendril.transform.localRotation = Quaternion.LookRotation(-direction, Vector3.up) * Quaternion.Euler(0f, 8f * Mathf.Sin(i), 0f);
                tendril.transform.localScale = Vector3.one;

                var mf = tendril.AddComponent<MeshFilter>();
                mf.mesh = CreaturePartMeshFactory.CreateTail(0.22f + genome.Flagella * 0.018f, 0.018f);
                var mr = tendril.AddComponent<MeshRenderer>();
                mr.sharedMaterial = CreateFlatUnlitMaterial(tendrilColor, tendril.name);
            }
        }

        private static void AddTraitBadges(GameObject root, Color bodyColor, GeneticTraits traits)
        {
            if (traits == GeneticTraits.None)
                return;

            int index = 0;
            AddTraitBadge(root, bodyColor, traits, GeneticTraits.Photosynthetic, new Color(0.55f, 1f, 0.28f), ref index);
            AddTraitBadge(root, bodyColor, traits, GeneticTraits.Toxic, new Color(0.95f, 0.32f, 1f), ref index);
            AddTraitBadge(root, bodyColor, traits, GeneticTraits.Armoured, new Color(0.75f, 0.78f, 0.82f), ref index);
            AddTraitBadge(root, bodyColor, traits, GeneticTraits.Regenerative, new Color(0.22f, 1f, 0.9f), ref index);
            AddTraitBadge(root, bodyColor, traits, GeneticTraits.Electric, new Color(1f, 0.9f, 0.18f), ref index);
        }

        private static void AddTraitBadge(GameObject root, Color bodyColor, GeneticTraits traits, GeneticTraits trait, Color color, ref int index)
        {
            if ((traits & trait) == 0)
                return;

            float angle = -0.9f + index * 0.45f;
            var badge = new GameObject($"Style_TraitBadge_{trait}");
            badge.transform.SetParent(root.transform, false);
            badge.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.22f, DetailSurfaceY + 0.026f, Mathf.Sin(angle) * 0.22f - 0.06f);
            badge.transform.localScale = Vector3.one * 0.04f;

            var mf = badge.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(1f, 6);
            var mr = badge.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateFlatUnlitMaterial(Color.Lerp(bodyColor, color, 0.8f), badge.name);
            index++;
        }

        private static void CreateTopEye(Transform parent, Vector3 localPosition, float radius, string name, Color bodyColor)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;

            var mf = eye.AddComponent<MeshFilter>();
            mf.mesh = CreaturePartMeshFactory.CreateEye(radius);
            var mr = eye.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateFlatUnlitMaterial(new Color(1f, 0.94f, 0.98f), name);

            var iris = new GameObject($"{name}_Iris");
            iris.transform.SetParent(eye.transform, false);
            iris.transform.localPosition = new Vector3(0f, radius * 0.62f, 0f);
            iris.transform.localScale = new Vector3(1f, 0.22f, 1f);
            var imf = iris.AddComponent<MeshFilter>();
            imf.mesh = CreaturePartMeshFactory.CreateEye(radius * 0.48f);
            var imr = iris.AddComponent<MeshRenderer>();
            imr.sharedMaterial = CreateFlatUnlitMaterial(Color.Lerp(bodyColor, new Color(0.05f, 0.17f, 0.35f), 0.65f), $"{name}_Iris");

            var pupil = new GameObject($"{name}_Pupil");
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, radius * 0.8f, 0f);
            pupil.transform.localScale = new Vector3(1f, 0.24f, 1f);
            var pmf = pupil.AddComponent<MeshFilter>();
            pmf.mesh = CreaturePartMeshFactory.CreatePupil(radius * 0.24f);
            var pmr = pupil.AddComponent<MeshRenderer>();
            pmr.sharedMaterial = CreateFlatUnlitMaterial(new Color(0.02f, 0.02f, 0.035f), $"{name}_Pupil");
        }

        private static void AddEyes(GameObject root, float scale)
        {
            CreateEye(root.transform, new Vector3(0.12f, 0.08f, 0.38f), 0.045f * scale, "Eye_R");
            CreateEye(root.transform, new Vector3(-0.12f, 0.08f, 0.38f), 0.045f * scale, "Eye_L");
        }

        private static void CreateEye(Transform parent, Vector3 localPosition, float radius, string name)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;

            var mf = eye.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 8);
            var mr = eye.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateFlatUnlitMaterial(new Color(1f, 0.98f, 0.9f), name);

            var pupil = new GameObject("Pupil");
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, radius * 0.58f);
            var pmf = pupil.AddComponent<MeshFilter>();
            pmf.mesh = ProceduralMeshFactory.CreateOrganicSphere(radius * 0.3f, 6);
            var pmr = pupil.AddComponent<MeshRenderer>();
            pmr.sharedMaterial = CreateFlatUnlitMaterial(new Color(0.09f, 0.09f, 0.09f), $"{name}_Pupil");
        }
    }
}
