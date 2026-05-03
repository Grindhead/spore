using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Disables spike and flagella child GameObjects when the organism is beyond a threshold distance from the camera.
    /// Rechecks every <see cref="CheckInterval"/> seconds to keep overhead minimal.
    /// Attach to the same GameObject as <see cref="Organism"/> (done automatically by <see cref="OrganismPool"/> and <see cref="Organism.Create"/>).
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class OrganismLOD : MonoBehaviour
    {
        /// <summary>Distance beyond which spike/flagella children are hidden. Tunable per-instance.</summary>
        public float HideDistance = 30f;

        /// <summary>Seconds between LOD recalculation (default 0.25 s = 4 checks/sec per organism).</summary>
        public float CheckInterval = 0.25f;

        private float _nextCheck;
        private Transform[] _detailChildren;
        private bool _detailVisible = true;

        private void Start()
        {
            // Collect spike/flagella child objects by tag on creation
            _detailChildren = CollectDetailChildren();
            _nextCheck = Time.time + Random.Range(0f, CheckInterval); // stagger to spread cost
        }

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + CheckInterval;

            if (!CameraManager.HasInstance) return;
            var cam = Camera.main;
            if (cam == null) return;

            float sqrDist = (transform.position - cam.transform.position).sqrMagnitude;
            bool shouldShow = sqrDist <= HideDistance * HideDistance;

            if (shouldShow != _detailVisible)
            {
                _detailVisible = shouldShow;
                SetDetailActive(_detailVisible);
            }
        }

        private void SetDetailActive(bool active)
        {
            if (_detailChildren == null) return;
            foreach (var child in _detailChildren)
            {
                if (child != null)
                    child.gameObject.SetActive(active);
            }
        }

        /// <summary>Re-scan children after pool reinitialisation (genome may have changed spike/flagella count).</summary>
        public void RefreshChildren()
        {
            _detailChildren = CollectDetailChildren();
            _detailVisible = true; // ensure visible state is reset
        }

        private Transform[] CollectDetailChildren()
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
            {
                string n = child.name.ToLowerInvariant();
                if (n.StartsWith("spike") || n.StartsWith("flagella"))
                    list.Add(child);
            }
            return list.ToArray();
        }
    }
}
