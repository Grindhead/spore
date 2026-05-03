using NUnit.Framework;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class ModelFactoryCellColliderTests
    {
        [TearDown]
        public void TearDown()
        {
            var cells = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in cells)
            {
                if (go != null && go.transform.parent == null && go.name == "Cell")
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CreateCell_SphereCollider_CoversHorizontalVisualBounds()
        {
            var go = ModelFactory.CreateCell(Vector3.zero, new Color(0.5f, 0f, 0.5f), 1.5f);
            Assert.IsNotNull(go);

            var collider = go.GetComponent<SphereCollider>();
            Assert.IsNotNull(collider, "Cell should have a SphereCollider on the root.");

            var renderers = go.GetComponentsInChildren<Renderer>();
            Assert.IsNotNull(renderers);
            Assert.IsNotEmpty(renderers, "Cell should have at least one renderer.");

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float requiredWorldRadius = ComputeRequiredHorizontalRadius(bounds, go.transform.position);
            float colliderWorldRadius = Mathf.Abs(go.transform.lossyScale.x) * collider.radius;

            Assert.GreaterOrEqual(
                colliderWorldRadius + 0.001f,
                requiredWorldRadius,
                "Cell collider should cover the rendered silhouette so contact matches visuals.");
        }

        private static float ComputeRequiredHorizontalRadius(Bounds bounds, Vector3 origin)
        {
            var centerOffset = bounds.center - origin;
            centerOffset.y = 0f;
            float centerOffsetWorld = centerOffset.magnitude;
            float boundsRadiusWorld = Mathf.Sqrt(
                bounds.extents.x * bounds.extents.x +
                bounds.extents.z * bounds.extents.z);
            return centerOffsetWorld + boundsRadiusWorld;
        }
    }
}
