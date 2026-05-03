using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for OrganismPool and the Organism.Reinitialize / SetPoolCallback hooks.
    /// </summary>
    public class OrganismPoolTests
    {
        [Test]
        public void Organism_SetPoolCallback_CallbackInvokedOnDie()
        {
            // Arrange
            var go = new GameObject("TestOrganism");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var org = go.AddComponent<Organism>();

            bool callbackFired = false;
            org.SetPoolCallback(o => { callbackFired = true; });

            // Act
            org.Die();

            // Assert
            Assert.IsTrue(callbackFired, "Pool callback should fire on Die()");
            // GameObject should NOT have been destroyed (pool owns it)
            Assert.IsTrue(go != null, "GameObject should not be destroyed when pool callback is set");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Organism_NullCallback_DestroyCalledNormally()
        {
            // When no pool callback is set, Die() destroys the GameObject (normal behaviour).
            var go = new GameObject("TestOrganism");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var org = go.AddComponent<Organism>();
            org.SetPoolCallback(null);

            org.Die();

            // In EditMode Destroy is deferred; IsDead should be true immediately.
            Assert.IsTrue(org.IsDead);
        }

        [Test]
        public void Organism_Reinitialize_ResetsStateAndPosition()
        {
            var go = new GameObject("TestOrganism");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var org = go.AddComponent<Organism>();

            // Kill it first via callback path
            org.SetPoolCallback(o => { /* pool returns it */ });
            org.Die();
            Assert.IsTrue(org.IsDead);

            // Reinitialise
            var genome = new Genome { Speed = 3f, Size = 2f };
            var targetPos = new Vector3(5f, 0f, 10f);
            org.Reinitialize(targetPos, genome, 80f, 42);

            Assert.IsFalse(org.IsDead, "IsDead should be false after Reinitialize");
            Assert.AreEqual(42, org.SpeciesId);
            Assert.AreEqual(80f, org.Energy, 0.001f);
            Assert.AreEqual(targetPos, org.transform.position);
            Assert.IsTrue(go.activeSelf, "GameObject should be active after Reinitialize");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Organism_CopyActiveInstances_IncludesActive_ExcludesInactive()
        {
            var active = MakeOrganism("Org_Active");
            var inactive = MakeOrganism("Org_Inactive");
            inactive.gameObject.SetActive(false);

            var buffer = new List<Organism>();
            int count = Organism.CopyActiveInstances(buffer);

            Assert.AreEqual(buffer.Count, count);
            Assert.Contains(active, buffer);
            Assert.IsFalse(buffer.Contains(inactive));

            Object.DestroyImmediate(active.gameObject);
            Object.DestroyImmediate(inactive.gameObject);
        }

        [Test]
        public void Organism_CopyActiveInstances_NullBuffer_ReturnsZero()
        {
            int count = Organism.CopyActiveInstances(null);
            Assert.AreEqual(0, count);
        }

        private static Organism MakeOrganism(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            return go.AddComponent<Organism>();
        }
    }
}
