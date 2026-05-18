using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class OrganismArchetypeConfiguratorTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
                Object.DestroyImmediate(go);
        }

        [Test]
        public void Configure_FilterFeeder_AssignsOnlyFilterFeederBehaviour()
        {
            var organism = CreateOrganism();

            OrganismArchetypeConfigurator.Configure(organism, OrganismArchetype.FilterFeeder);

            Assert.IsNotNull(organism.GetComponent<FilterFeederCell>());
            Assert.IsNull(organism.GetComponent<CellOrganismAI>());
            Assert.IsNull(organism.GetComponent<BacteriaSwarmCell>());
            Assert.IsNull(organism.GetComponent<ScavengerCell>());
            Assert.IsNull(organism.GetComponent<ParasiteCell>());
            Assert.IsNull(organism.GetComponent<PhotosyntheticCell>());
            Assert.IsNull(organism.GetComponent<ColonialCell>());
        }

        [Test]
        public void Configure_ReconfigureToGeneralist_ClearsOldArchetype()
        {
            var organism = CreateOrganism();
            OrganismArchetypeConfigurator.Configure(organism, OrganismArchetype.FilterFeeder);
            Assert.IsNotNull(organism.GetComponent<FilterFeederCell>());

            OrganismArchetypeConfigurator.Configure(organism, OrganismArchetype.Generalist);

            Assert.IsNotNull(organism.GetComponent<CellOrganismAI>());
            Assert.IsNull(organism.GetComponent<FilterFeederCell>());
            Assert.IsNull(organism.GetComponent<BacteriaSwarmCell>());
            Assert.IsNull(organism.GetComponent<ScavengerCell>());
            Assert.IsNull(organism.GetComponent<ParasiteCell>());
            Assert.IsNull(organism.GetComponent<PhotosyntheticCell>());
            Assert.IsNull(organism.GetComponent<ColonialCell>());
        }

        [Test]
        public void Profiles_CoverEveryArchetype_WithReadableGameplayMetadata()
        {
            foreach (OrganismArchetype archetype in System.Enum.GetValues(typeof(OrganismArchetype)))
            {
                var profile = OrganismArchetypeConfigurator.GetProfile(archetype);

                Assert.AreEqual(archetype, profile.Archetype);
                Assert.IsNotEmpty(profile.Silhouette);
                Assert.IsNotEmpty(profile.BehaviorTell);
                Assert.IsNotEmpty(profile.RewardLabel);
                Assert.IsTrue(System.Enum.IsDefined(typeof(CellStage.TrophicRole), profile.Diet));
                Assert.IsTrue(System.Enum.IsDefined(typeof(CellThreatLevel), profile.ThreatLevel));
            }
        }

        [Test]
        public void Configure_Attaches_ReadabilityMarker_ForHudAndTells()
        {
            var organism = CreateOrganism();

            OrganismArchetypeConfigurator.Configure(organism, OrganismArchetype.Parasite);

            var marker = organism.GetComponent<OrganismReadabilityMarker>();
            Assert.IsNotNull(marker);
            Assert.AreEqual(OrganismArchetype.Parasite, marker.Archetype);
            Assert.AreEqual(CellStage.TrophicRole.Carnivore, marker.Diet);
            Assert.AreEqual(CellThreatLevel.High, marker.ThreatLevel);
            Assert.That(marker.BehaviorTell, Does.Contain("lunging"));
        }

        private static Organism CreateOrganism()
        {
            var go = new GameObject("TestOrganism");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            return go.AddComponent<Organism>();
        }
    }
}
