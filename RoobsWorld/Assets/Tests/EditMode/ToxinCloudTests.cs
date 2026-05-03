using NUnit.Framework;
using System.Collections.Generic;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class ToxinCloudTests
    {
        [TearDown]
        public void CleanupClouds()
        {
            var clouds = Object.FindObjectsByType<ToxinCloud>(FindObjectsInactive.Include);
            for (int i = 0; i < clouds.Length; i++)
            {
                if (clouds[i] != null)
                    Object.DestroyImmediate(clouds[i].gameObject);
            }
        }

        [Test]
        public void CopyActiveInstances_IncludesActive_ExcludesInactive()
        {
            var active = MakeCloud("Cloud_Active");
            var inactive = MakeCloud("Cloud_Inactive");
            inactive.gameObject.SetActive(false);

            var buffer = new List<ToxinCloud>();
            int count = ToxinCloud.CopyActiveInstances(buffer);

            Assert.AreEqual(buffer.Count, count);
            Assert.Contains(active, buffer);
            Assert.IsFalse(buffer.Contains(inactive));
        }

        [Test]
        public void CopyActiveInstances_NullBuffer_ReturnsZero()
        {
            int count = ToxinCloud.CopyActiveInstances(null);
            Assert.AreEqual(0, count);
        }

        private static ToxinCloud MakeCloud(string name)
        {
            var go = new GameObject(name);
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            return go.AddComponent<ToxinCloud>();
        }
    }
}
