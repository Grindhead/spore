using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tiny pathogen organism spawned during a <see cref="CellEnvironmentEventType.VirusOutbreak"/>
    /// event. Drifts toward the nearest cell (player or NPC), delivers a mass-drain burst on contact,
    /// then self-destructs.
    ///
    /// Viral particles are not pooled organisms; they are spawned by
    /// <see cref="CellEnvironmentEventManager"/> and exist only for the duration of the event.
    ///
    /// Visual: very small (0.25 scale), bright magenta with emission glow.
    /// The player can reduce viral drain by 50 % if they carry the <see cref="GeneticTraits.Armoured"/>
    /// trait. When the drain hits the player a <see cref="CellPlayerInfectedEvent"/> is published
    /// so the HUD can flash a brief warning.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class ViralParticle : MonoBehaviour
    {
        private Transform    _target;
        private PlayerCell   _player;
        private Rigidbody    _rb;
        private float        _lifetime;
        private bool         _consumed;
        private float        _wobbleSeed;

        // ---- Public for tests ----
        public bool IsConsumed => _consumed;

        // ---- Lifecycle ----

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody>();
            if (_rb == null)
            {
                Debug.LogWarning("ViralParticle missing Rigidbody and could not add one. Destroying particle.");
                Destroy(gameObject);
                return;
            }

            _rb.useGravity  = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            var col = GetComponent<SphereCollider>();
            if (col == null)
                col = gameObject.AddComponent<SphereCollider>();
            if (col == null)
            {
                Debug.LogWarning("ViralParticle missing SphereCollider and could not add one. Destroying particle.");
                Destroy(gameObject);
                return;
            }
            col.isTrigger = true;
            col.radius = 0.5f;
            _wobbleSeed = Random.value * 1000f;
        }

        /// <summary>
        /// Call after adding the component. Supplies the player reference for drain calculation
        /// and sets the initial seek target.
        /// </summary>
        public void Initialize(PlayerCell player, Transform initialTarget)
        {
            _player  = player;
            _target  = initialTarget;
            _lifetime = BalanceConstants.ViralParticleLifetimeSeconds;

            transform.localScale = Vector3.one
                * BalanceConstants.ViralParticleScale
                * BalanceConstants.CellEntityVisualScaleMultiplier;
            CellSwimVisual.Ensure(gameObject, CellSwimVisual.SwimProfile.Viral);

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.9f, 0.1f, 0.85f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor",
                    new Color(0.7f, 0f, 0.65f) * 2f);
            }
        }

        private void Update()
        {
            if (_consumed) return;
            if (_rb == null)
            {
                Destroy(gameObject);
                return;
            }

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Drift with a gentle sinusoidal wobble to feel organic
            Vector3 dir = Vector3.zero;
            if (_target != null)
            {
                dir = (_target.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) dir.Normalize();
            }

            float wobble = Mathf.Sin(Time.time * 3f + _wobbleSeed) * 0.3f;
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * wobble;
            _rb.linearVelocity = (dir + perp) * BalanceConstants.ViralParticleSpeed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed) return;

            // ---- Contact with player ----
            var pc = other.GetComponent<PlayerCell>() ?? other.GetComponentInParent<PlayerCell>();
            if (pc != null)
            {
                float drain = BalanceConstants.ViralParticleDrainOnPlayer;
                if (pc.PlayerGenome != null && pc.PlayerGenome.HasTrait(GeneticTraits.Armoured))
                    drain *= BalanceConstants.ViralParticleArmouredDrainReduction;

                pc.TakeDamage(drain);
                EventBus.Publish(new CellPlayerInfectedEvent { Drain = drain });
                Consume();
                return;
            }

            // ---- Contact with NPC organism ----
            var org = other.GetComponent<Organism>() ?? other.GetComponentInParent<Organism>();
            if (org != null && !org.IsDead)
            {
                org.TakeDamage(BalanceConstants.ViralParticleDrainOnOrganism);
                Consume();
            }
        }

        private void Consume()
        {
            _consumed = true;
            // Tiny pop VFX at contact point
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DamageHit, transform.position);
            Destroy(gameObject, 0.05f);
        }
    }
}
