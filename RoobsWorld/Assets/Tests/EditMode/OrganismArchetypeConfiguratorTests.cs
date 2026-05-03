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
