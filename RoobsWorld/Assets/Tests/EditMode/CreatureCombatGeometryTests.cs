using NUnit.Framework;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureCombatGeometryTests
    {
        [Test]
        public void ComputeContactReach_IncludesBodyRadii()
        {
            float reach = CreatureCombatGeometry.ComputeContactReach(
                baseRange: 2f,
                attackerRadius: 1f,
                targetRadius: 1.5f,
                attackerRadiusWeight: 0.25f,
                targetRadiusWeight: 1f);

            Assert.That(reach, Is.EqualTo(3.75f).Within(0.0001f));
        }

        [Test]
        public void EstimatePlanarRadius_UsesColliderBounds()
        {
            var go = new GameObject("CombatRadius_Test");
            try
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(2f, 1f, 4f); // extents => (1, 0.5, 2)

                float radius = CreatureCombatGeometry.EstimatePlanarRadius(go.transform);
                Assert.That(radius, Is.EqualTo(Mathf.Sqrt(5f)).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EstimatePlanarRadius_UsesFallback_WhenNoGeometryExists()
        {
            var go = new GameObject("CombatRadius_Empty");
            try
            {
                float radius = CreatureCombatGeometry.EstimatePlanarRadius(go.transform, fallbackRadius: 0.9f);
                Assert.That(radius, Is.EqualTo(0.9f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
