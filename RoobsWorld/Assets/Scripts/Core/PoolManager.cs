using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Global object pool for GameObject instances, keyed by prefab, to reduce allocations and instantiation cost.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<UnityEngine.EntityId, Queue<GameObject>> _pools = new Dictionary<UnityEngine.EntityId, Queue<GameObject>>();
        private readonly Dictionary<UnityEngine.EntityId, Transform> _poolParents = new Dictionary<UnityEngine.EntityId, Transform>();

        /// <summary>
        /// Pre-populates the pool for <paramref name="prefab"/> with the given number of inactive instances.
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            UnityEngine.EntityId key = prefab.GetEntityId();
            EnsurePool(key, prefab);

            for (int i = 0; i < count; i++)
            {
                var obj = CreateNewInstance(prefab, key);
                obj.SetActive(false);
                _pools[key].Enqueue(obj);
            }
        }

        /// <summary>
        /// Retrieves an instance for <paramref name="prefab"/> from the pool (or instantiates one) and places it at the given transform.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            UnityEngine.EntityId key = prefab.GetEntityId();
            EnsurePool(key, prefab);

            GameObject obj;
            var pool = _pools[key];

            while (pool.Count > 0)
            {
                obj = pool.Dequeue();
                if (obj == null) continue;

                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                return obj;
            }

            obj = CreateNewInstance(prefab, key);
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Returns an instance to the pool and deactivates it so it can be reused later.
        /// </summary>
        public void Despawn(GameObject obj, GameObject prefab)
        {
            UnityEngine.EntityId key = prefab.GetEntityId();
            EnsurePool(key, prefab);

            obj.SetActive(false);
            obj.transform.SetParent(_poolParents[key]);
            _pools[key].Enqueue(obj);
        }

        /// <summary>
        /// Destroys all pooled instances and the pool container for the given <paramref name="prefab"/>.
        /// </summary>
        public void ClearPool(GameObject prefab)
        {
            UnityEngine.EntityId key = prefab.GetEntityId();
            if (!_pools.ContainsKey(key)) return;

            while (_pools[key].Count > 0)
            {
                var obj = _pools[key].Dequeue();
                if (obj != null)
                    Destroy(obj);
            }

            if (_poolParents.ContainsKey(key) && _poolParents[key] != null)
                Destroy(_poolParents[key].gameObject);

            _pools.Remove(key);
            _poolParents.Remove(key);
        }

        /// <summary>
        /// Destroys all pooled instances and clears every registered prefab pool.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _poolParents)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _pools.Clear();
            _poolParents.Clear();
        }

        private void EnsurePool(UnityEngine.EntityId key, GameObject prefab)
        {
            if (_pools.ContainsKey(key)) return;

            _pools[key] = new Queue<GameObject>();
            var parent = new GameObject($"Pool_{prefab.name}");
            parent.transform.SetParent(transform);
            _poolParents[key] = parent.transform;
        }

        private GameObject CreateNewInstance(GameObject prefab, UnityEngine.EntityId key)
        {
            var obj = Instantiate(prefab, _poolParents[key]);
            return obj;
        }
    }
}
