using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Expiring hazard volume that drains mass from the player and damages NPC cells over time.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class ToxinCloud : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<ToxinCloud> _activeInstances =
            new System.Collections.Generic.List<ToxinCloud>(24);

        [SerializeField] private float _damagePerSecond = 0.2f;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _lifetime = 15f;

        private float _age;

        /// <summary>
        /// Copies currently active toxin clouds into <paramref name="buffer"/> and returns the count.
        /// Null or destroyed references are compacted out of the registry.
        /// </summary>
        public static int CopyActiveInstances(System.Collections.Generic.List<ToxinCloud> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            CompactRegistry();
            var sceneInstances = FindObjectsByType<ToxinCloud>(FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneInstances.Length; i++)
            {
                var cloud = sceneInstances[i];
                if (cloud != null && !_activeInstances.Contains(cloud))
                    _activeInstances.Add(cloud);
            }

            for (int i = 0; i < _activeInstances.Count; i++)
            {
                var cloud = _activeInstances[i];
                if (cloud != null && cloud.isActiveAndEnabled)
                    buffer.Add(cloud);
            }

            return buffer.Count;
        }

        private static void CompactRegistry()
        {
            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                if (_activeInstances[i] == null)
                    _activeInstances.RemoveAt(i);
            }
        }

        public void Initialize(float radius, float damagePerSecond, float lifetime)
        {
            _radius = radius;
            _damagePerSecond = damagePerSecond;
            _lifetime = lifetime;
            ApplyRadius();
        }

        private void Start()
        {
            ApplyRadius();
        }

        private void OnEnable()
        {
            if (!_activeInstances.Contains(this))
                _activeInstances.Add(this);
        }

        private void OnDisable()
        {
            _activeInstances.Remove(this);
        }

        private void OnDestroy()
        {
            _activeInstances.Remove(this);
        }

        private void ApplyRadius()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius;
            transform.localScale = Vector3.one * (_radius * 2f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                UnityObjectUtil.Destroy(gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            float tick = _damagePerSecond * Time.deltaTime;

            var player = other.GetComponent<PlayerCell>();
            if (player != null)
            {
                player.TakeDamage(tick);
                return;
            }

            var npc = other.GetComponent<CellAI>();
            if (npc != null)
            {
                npc.ApplyDamage(tick * 0.5f);
            }
        }
    }
}
