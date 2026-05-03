using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Trigger volume that creates a whirlpool effect: pulls rigidbodies toward the zone center and applies
    /// a tangential spin force, creating an inward spiral. Works alongside <see cref="ArenaCurrent"/> for
    /// more hazardous environmental variety.
    /// Attach to a GameObject with a SphereCollider set to isTrigger = true.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class WhirlpoolZone : MonoBehaviour
    {
        [Tooltip("Maximum inward pull force at the zone edge. Scales linearly to the centre.")]
        [SerializeField] private float _pullForce = 6f;

        [Tooltip("Tangential spin force applied perpendicular to the pull direction. Positive = counterclockwise.")]
        [SerializeField] private float _spinForce = 4f;

        [Tooltip("Radius of the whirlpool. Used to normalise distance for force calculation.")]
        [SerializeField] private float _radius = 10f;

        /// <summary>Initialises whirlpool parameters from code (e.g. CellStage procedural spawn).</summary>
        public void Initialize(float radius, float pullForce, float spinForce)
        {
            _radius = Mathf.Max(0.1f, radius);
            _pullForce = pullForce;
            _spinForce = spinForce;

            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius;
        }

        private void Start()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius;
        }

        private void OnTriggerStay(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            Vector3 toCenter = transform.position - other.transform.position;
            float dist = toCenter.magnitude;
            if (dist < 0.01f) return;

            // Pull force scales with proximity: stronger near the edge, consistent near the core
            float normalised = Mathf.Clamp01(dist / _radius);
            Vector3 inward = toCenter.normalized;
            rb.AddForce(inward * _pullForce * normalised, ForceMode.Acceleration);

            // Tangential force perpendicular to inward vector in the XZ plane (creates spin)
            Vector3 tangent = Vector3.Cross(Vector3.up, inward).normalized;
            rb.AddForce(tangent * _spinForce, ForceMode.Acceleration);
        }
    }
}
