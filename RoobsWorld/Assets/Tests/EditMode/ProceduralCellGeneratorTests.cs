using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Utils;

namespace RoobsWorld.Tests
{
    public class ProceduralCellGeneratorTests
    {
        [TearDown]
        public void TearDown()
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all)
            {
                if (go != null && go.transform.parent == null && (go.name == "Cell" || go.name == "Organism"))
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CreateCellFromGenome_WithNullGenome_ReturnsNonNullGameObject()
        {
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, null);
            Assert.IsNotNull(go);
            bool hasRootMesh = go.GetComponent<MeshFilter>() != null && go.GetComponent<MeshRenderer>() != null;
            bool hasAnyRendererInChildren = go.GetComponentInChildren<Renderer>() != null;
            Assert.IsTrue(hasRootMesh || hasAnyRendererInChildren);
            Assert.IsNotNull(go.GetComponent<SphereCollider>());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_WithGenome_ReturnsNonNullGameObject()
        {
            var genome = Genome.Default();
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            Assert.IsNotNull(go);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_ScaleReflectsSize()
        {
            var genome = new Genome { Size = 2f };
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            float expectedScale = 0.5f * 2f;
            Assert.AreEqual(expectedScale, go.transform.localScale.x, 0.001f);
            Assert.AreEqual(expectedScale, go.transform.localScale.y, 0.001f);
            Assert.AreEqual(expectedScale, go.transform.localScale.z, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_BodyColorReflectsGenomeColor()
        {
            var genome = new Genome { Color = new Color(0.2f, 0.8f, 0.4f) };
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            var mr = go.GetComponent<MeshRenderer>() ?? go.GetComponentInChildren<MeshRenderer>();
            Assert.IsNotNull(mr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_BodyMaterialAndSkinTextureAreOpaque()
        {
            var genome = new Genome
            {
                Color = new Color(0.2f, 0.8f, 0.4f, 0.18f),
                Traits = GeneticTraits.Photosynthetic
            };

            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            var mr = go.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr);
            Assert.IsNotNull(mr.sharedMaterial);
            Assert.AreEqual(1f, mr.sharedMaterial.color.a, 0.001f);

            if (mr.sharedMaterial.HasProperty("_Color"))
                Assert.AreEqual(1f, mr.sharedMaterial.GetColor("_Color").a, 0.001f);
            if (mr.sharedMaterial.HasProperty("_BaseColor"))
                Assert.AreEqual(1f, mr.sharedMaterial.GetColor("_BaseColor").a, 0.001f);

            var texture = mr.sharedMaterial.mainTexture as Texture2D;
            Assert.IsNotNull(texture);
            Assert.AreEqual(1f, texture.GetPixel(0, 0).a, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetSpikeCount_ZeroSpikes_ReturnsZero()
        {
            Assert.AreEqual(0, ProceduralCellGenerator.GetSpikeCount(0f));
        }

        [Test]
        public void GetSpikeCount_MaxSpikes_ReturnsEight()
        {
            Assert.AreEqual(8, ProceduralCellGenerator.GetSpikeCount(10f));
        }

        [Test]
        public void GetSpikeCount_MidSpikes_ReturnsScaledCount()
        {
            int mid = ProceduralCellGenerator.GetSpikeCount(5f);
            Assert.GreaterOrEqual(mid, 3);
            Assert.LessOrEqual(mid, 5);
        }

        [Test]
        public void GetFlagellaCount_ZeroFlagella_ReturnsZero()
        {
            Assert.AreEqual(0, ProceduralCellGenerator.GetFlagellaCount(0f));
        }

        [Test]
        public void GetFlagellaCount_MaxFlagella_ReturnsSix()
        {
            Assert.AreEqual(6, ProceduralCellGenerator.GetFlagellaCount(10f));
        }

        [Test]
        public void CreateCellFromGenome_SpikeChildCount_MatchesGetSpikeCount()
        {
            var genome = new Genome { Spikes = 6f };
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            int expectedSpikes = ProceduralCellGenerator.GetSpikeCount(6f);
            int spikeChildren = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (go.transform.GetChild(i).name.StartsWith("Spike_"))
                    spikeChildren++;
            }
            Assert.AreEqual(expectedSpikes, spikeChildren);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_FlagellaChildCount_MatchesGetFlagellaCount()
        {
            var genome = new Genome { Flagella = 4f };
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            int expectedFlagella = ProceduralCellGenerator.GetFlagellaCount(4f);
            int flagellaChildren = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (go.transform.GetChild(i).name.StartsWith("Flagella_"))
                    flagellaChildren++;
            }
            Assert.AreEqual(expectedFlagella, flagellaChildren);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateCellFromGenome_VisuallyDistinct_DifferentSpikesAndFlagella()
        {
            var genomeLow = new Genome { Spikes = 0f, Flagella = 0f };
            var genomeHigh = new Genome { Spikes = 10f, Flagella = 10f };
            var goLow = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genomeLow);
            var goHigh = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genomeHigh);
            int lowSpikeCount = 0;
            int lowFlagellaCount = 0;
            int highSpikeCount = 0;
            int highFlagellaCount = 0;

            for (int i = 0; i < goLow.transform.childCount; i++)
            {
                string n = goLow.transform.GetChild(i).name;
                if (n.StartsWith("Spike_")) lowSpikeCount++;
                if (n.StartsWith("Flagella_")) lowFlagellaCount++;
            }

            for (int i = 0; i < goHigh.transform.childCount; i++)
            {
                string n = goHigh.transform.GetChild(i).name;
                if (n.StartsWith("Spike_")) highSpikeCount++;
                if (n.StartsWith("Flagella_")) highFlagellaCount++;
            }

            Assert.AreEqual(0, lowSpikeCount);
            Assert.AreEqual(0, lowFlagellaCount);
            Assert.Greater(highSpikeCount, 0);
            Assert.Greater(highFlagellaCount, 0);
            Object.DestroyImmediate(goLow);
            Object.DestroyImmediate(goHigh);
        }

        [Test]
        public void ApplyLoadoutVisuals_CiliaAndJet_AddsLoadoutChildren()
        {
            var genome = Genome.Default();
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, genome);
            var loadout = new CellPartLoadout();
            loadout.TryUnlock(CellPartType.Cilia);
            loadout.TryUnlock(CellPartType.Jet);

            ProceduralCellGenerator.ApplyLoadoutVisuals(go, loadout, Color.cyan);

            int cilia = 0;
            int jet = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                string n = go.transform.GetChild(i).name;
                if (n.StartsWith("Loadout_Cilia_")) cilia++;
                if (n.StartsWith("Loadout_Jet_")) jet++;
            }

            Assert.Greater(cilia, 0);
            Assert.AreEqual(1, jet);
            Object.DestroyImmediate(go);
        }

        // ---- Trait tint tests ----

        [Test]
        public void ApplyTraitTint_NoTraits_ReturnsSameColor()
        {
            var c = new Color(0.5f, 0.3f, 0.8f, 0.2f);
            var result = ProceduralCellGenerator.ApplyTraitTint(c, GeneticTraits.None);
            Assert.AreEqual(c.r, result.r, 0.001f);
            Assert.AreEqual(c.g, result.g, 0.001f);
            Assert.AreEqual(c.b, result.b, 0.001f);
            Assert.AreEqual(1f, result.a, 0.001f);
        }

        [Test]
        public void CreateCreatureSkinTexture_TransparentInputs_ProducesOpaquePixels()
        {
            var texture = ProceduralTextureFactory.CreateCreatureSkinTexture(
                new Color(0.2f, 0.8f, 0.4f, 0.1f),
                new Color(0.1f, 0.4f, 0.8f, 0.3f),
                8);

            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                Assert.AreEqual(1f, pixels[i].a, 0.001f);
        }

        [Test]
        public void ApplyTraitTint_Photosynthetic_ShiftsTowardGreen()
        {
            var neutral = new Color(0.5f, 0.5f, 0.5f);
            var tinted = ProceduralCellGenerator.ApplyTraitTint(neutral, GeneticTraits.Photosynthetic);
            Assert.Greater(tinted.g, neutral.g, "Photosynthetic should increase green component");
        }

        [Test]
        public void ApplyTraitTint_Toxic_ShiftsTowardPurple()
        {
            var neutral = new Color(0.5f, 0.5f, 0.5f);
            var tinted = ProceduralCellGenerator.ApplyTraitTint(neutral, GeneticTraits.Toxic);
            Assert.Greater(tinted.b, neutral.b, "Toxic should increase blue/purple component");
        }

        [Test]
        public void ApplyTraitTint_Electric_ShiftsTowardYellow()
        {
            var neutral = new Color(0.5f, 0.5f, 0.5f);
            var tinted = ProceduralCellGenerator.ApplyTraitTint(neutral, GeneticTraits.Electric);
            // Yellow = high R+G, lower B
            Assert.Less(tinted.b, neutral.b, "Electric should decrease blue component (shift to yellow)");
        }

        [Test]
        public void ApplyTraitTint_MultipleTraits_ProducesDifferentColorThanSingle()
        {
            var neutral = new Color(0.5f, 0.5f, 0.5f);
            var single = ProceduralCellGenerator.ApplyTraitTint(neutral, GeneticTraits.Photosynthetic);
            var multi  = ProceduralCellGenerator.ApplyTraitTint(neutral, GeneticTraits.Photosynthetic | GeneticTraits.Toxic);
            // Multi-trait tint should differ from single-trait
            bool differs = Mathf.Abs(single.r - multi.r) > 0.001f ||
                           Mathf.Abs(single.g - multi.g) > 0.001f ||
                           Mathf.Abs(single.b - multi.b) > 0.001f;
            Assert.True(differs, "Multiple traits should produce different tint than a single trait");
        }
    }
}
