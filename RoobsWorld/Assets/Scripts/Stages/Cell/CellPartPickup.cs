using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// World pickup that unlocks a specific <see cref="CellPartType"/> and grants DNA when collected.
    /// </summary>
    public class CellPartPickup : MonoBehaviour
    {
        private const float MinimumWorldTriggerRadius = 0.45f;

        [SerializeField] private CellPartType _partType = CellPartType.Flagella;
        [SerializeField] private int _dnaReward = 3;

        public CellPartType PartType => _partType;
        public int DnaReward => _dnaReward;

        public void SetPartType(CellPartType partType)
        {
            _partType = partType;
        }

        private void Awake()
        {
            EnsureTriggerColliders();
        }

        private void OnEnable()
        {
            EnsureTriggerColliders();
        }

        public void EnsureTriggerColliders()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            bool hasSupportedTrigger = false;

            for (int i = 0; colliders != null && i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;

                if (col is MeshCollider)
                {
                    // Pickups use an explicit root sphere trigger; decorative mesh colliders are unreliable as triggers.
                    UnityObjectUtil.Destroy(col);
                    continue;
                }

                col.enabled = true;
                col.isTrigger = true;
                hasSupportedTrigger = true;
            }

            var rootTrigger = GetComponent<SphereCollider>();
            if (rootTrigger == null)
                rootTrigger = gameObject.AddComponent<SphereCollider>();

            rootTrigger.enabled = true;
            rootTrigger.isTrigger = true;

            float lossyScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
            float minLocalRadius = MinimumWorldTriggerRadius / lossyScale;
            rootTrigger.radius = Mathf.Max(rootTrigger.radius, minLocalRadius);

            if (!hasSupportedTrigger && rootTrigger != null)
            {
                hasSupportedTrigger = true;
            }
        }
    }
}
