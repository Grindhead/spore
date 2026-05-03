using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Trigger volume that applies a directional flow force to rigidbodies, simulating underwater currents.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ArenaCurrent : MonoBehaviour
    {
        [SerializeField] private Vector3 _flowDirection = Vector3.right;
        [SerializeField] private float _force = 4f;

        /// <summary>World-space flow direction (normalized). Used by HUD current indicator.</summary>
        public Vector3 FlowDirection => _flowDirection;
        /// <summary>Force magnitude applied to rigidbodies. Used by HUD current indicator.</summary>
        public float Force => _force;

        public void Initialize(Vector3 direction, float force)
        {
            _flowDirection = direction.normalized;
            _force = force;
        }

        private void Start()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                rb.AddForce(_flowDirection * _force, ForceMode.Acceleration);
            }
        }
    }
}
