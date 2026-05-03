using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Spawns drifting background debris particles (organic fragments, micro-plankton) in the cell arena
    /// to make the ocean feel alive. Purely aesthetic — no gameplay impact.
    /// Each particle wanders slowly with a random heading that gradually turns, wrapping at the arena edge.
    /// </summary>
    public class CellArenaDebris : MonoBehaviour
    {
        // ---- tuneable ----
        public const int DefaultCount = 90;
        public const float DefaultArenaRadius = 50f;
        public const float MinSpeed = 0.3f;
        public const float MaxSpeed = 1.2f;
        public const float MaxTurnRateRad = 0.8f;   // radians/s
        public const float MinScale = 0.06f;
        public const float MaxScale = 0.22f;
        public const float WrapMargin = 2f;          // re-spawn this far inside the edge

        private struct Particle
        {
            public GameObject Go;
            public float WanderAngle;   // current heading in radians
            public float Speed;
            public float TurnRate;      // radians/s (random per particle)
        }

        private Particle[] _particles;
        private Vector3 _center;
        private float _arenaRadius;

        // Colour palette — muted greens, teals, faint amber, pale cyan (microscopic organics)
        private static readonly Color[] Palette =
        {
            new Color(0.62f, 0.86f, 0.78f, 0.17f),
            new Color(0.52f, 0.84f, 0.90f, 0.15f),
            new Color(0.78f, 0.86f, 0.62f, 0.14f),
            new Color(0.64f, 0.78f, 0.92f, 0.13f),
            new Color(0.85f, 0.79f, 0.64f, 0.12f),
            new Color(0.74f, 0.90f, 0.92f, 0.1f),
        };

        /// <summary>
        /// Creates and activates a debris layer for the given arena.
        /// Call from <c>CellStage.SpawnHazards()</c> (or equivalent) after the arena floor is ready.
        /// </summary>
        public static CellArenaDebris Spawn(Vector3 center, float arenaRadius, int count = DefaultCount)
        {
            var go = new GameObject("CellArenaDebris");
            var comp = go.AddComponent<CellArenaDebris>();
            comp.Initialise(center, arenaRadius, count);
            return comp;
        }

        private void Initialise(Vector3 center, float arenaRadius, int count)
        {
            _center = center;
            _arenaRadius = arenaRadius;
            _particles = new Particle[count];

            for (int i = 0; i < count; i++)
                _particles[i] = CreateParticle(i, randomPos: true);
        }

        private Particle CreateParticle(int index, bool randomPos)
        {
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pGo.name = $"Debris_{index}";
            pGo.transform.SetParent(transform, false);
            var col = pGo.GetComponent<Collider>();
            if (col == null)
                col = pGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.enabled = true;

            float scale = Random.Range(MinScale, MaxScale);
            pGo.transform.localScale = new Vector3(
                scale * Random.Range(0.65f, 0.95f),
                scale * Random.Range(0.55f, 0.9f),
                scale * Random.Range(1.1f, 1.7f));
            pGo.transform.localRotation = Quaternion.Euler(
                Random.Range(-25f, 25f),
                Random.Range(0f, 360f),
                Random.Range(-25f, 25f));

            if (randomPos)
            {
                float r = Random.Range(0f, _arenaRadius * 0.95f);
                float a = Random.Range(0f, 2f * Mathf.PI);
                pGo.transform.position = _center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }

            // Colour + emission
            var mr = pGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var palColor = Palette[index % Palette.Length];
                var mat = new Material(mr.sharedMaterial ?? new Material(Shader.Find("Standard")));
                mat.color = palColor;
                mat.SetFloat("_Mode", 3); // Transparent
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                mr.sharedMaterial = mat;
            }

            // Make debris behave like low-value plant food so "white circles"
            // are consumable the same way as green food.
            var food = pGo.AddComponent<FoodItem>();
            food.Initialize(FoodType.Plant, 1.2f);
            food.EnsureTriggerColliders();

            float heading = Random.Range(0f, 2f * Mathf.PI);
            return new Particle
            {
                Go          = pGo,
                WanderAngle = heading,
                Speed       = Random.Range(MinSpeed, MaxSpeed),
                TurnRate    = Random.Range(-MaxTurnRateRad, MaxTurnRateRad),
            };
        }

        private void Update()
        {
            if (_particles == null || _particles.Length == 0)
                return;

            float dt = Time.deltaTime;
            float wrapRadius = _arenaRadius - WrapMargin;

            for (int i = 0; i < _particles.Length; i++)
            {
                ref var p = ref _particles[i];
                if (!p.Go) continue;

                // Gradually drift heading
                p.WanderAngle += p.TurnRate * dt;

                var dir = new Vector3(Mathf.Cos(p.WanderAngle), 0f, Mathf.Sin(p.WanderAngle));
                p.Go.transform.position += dir * (p.Speed * dt);

                // Wrap at arena edge — teleport to a random position on the opposite arc
                var pos = p.Go.transform.position;
                var flat = new Vector3(pos.x - _center.x, 0f, pos.z - _center.z);
                if (flat.magnitude > wrapRadius)
                {
                    // Re-enter from the diametrically opposite side
                    float oppositeAngle = Mathf.Atan2(-flat.z, -flat.x);
                    float r = wrapRadius * Random.Range(0.05f, 0.4f);
                    p.Go.transform.position = _center + new Vector3(
                        Mathf.Cos(oppositeAngle) * r, 0f, Mathf.Sin(oppositeAngle) * r);
                    // Randomise heading slightly so it doesn't retrace the same path
                    p.WanderAngle = oppositeAngle + Random.Range(-0.5f, 0.5f);
                }
            }
        }

        private void OnDisable()
        {
            if (_particles == null) return;
            foreach (var p in _particles)
            {
                if (p.Go != null)
                {
                    if (Application.isPlaying) Destroy(p.Go);
                    else DestroyImmediate(p.Go);
                }
            }
        }

        // ---- testable properties ----
        /// <summary>Number of active debris particles.</summary>
        public int Count => _particles?.Length ?? 0;
        /// <summary>Arena radius this debris layer was initialised with.</summary>
        public float ArenaRadius => _arenaRadius;
    }
}
