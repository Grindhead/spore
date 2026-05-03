using System;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Cell-stage organism: genome, energy, age, position, and core actions (Die, Move, Eat).
    /// Step 1 of cell-stage evolution roadmap. Visual: ProceduralCellGenerator (body + spikes + flagella from genome).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Collider))]
    public class Organism : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<Organism> _activeInstances =
            new System.Collections.Generic.List<Organism>(64);

        private const float BaseScale = 0.5f;
        private const float InitialEnergy = 50f;
        private const float MinEnergyToSurvive = 0f;
        private const float MinimumGenomeSize = 0.1f;
        private const float MaximumGrowthSizeBonus = 3.2f;
        private const float FoodGrowthToSizeFactor = 0.0045f;
        private const float PredationNutritionPerScale = 3.8f;
        private const float PreyConsumeSizeRatio = 0.82f;
        private const float ThreatContactSizeRatio = 1.16f;
        private const float ContactPredationDamage = 0.08f;

        [Header("State")]
        [SerializeField] private Genome _genome;
        [SerializeField] private float _energy = InitialEnergy;
        [SerializeField] private float _age;
        [SerializeField] private int _speciesId;
        [SerializeField] private float _growthSizeBonus;

        private MeshRenderer _renderer;
        private bool _dead;
        private System.Action<Organism> _poolReturnCallback;
        private float _electricPulseTimer;
        private float _electricSlowTimer;
        private readonly BehaviourBiasTracker _adaptivePressure = new BehaviourBiasTracker();

        /// <summary>Optional: returns metabolism multiplier for current position (e.g. from biome). If null, 1f is used.</summary>
        public Func<float> GetMetabolismMultiplier;

        /// <summary>Optional: returns food efficiency multiplier for current position (e.g. from biome). If null, 1f is used when eating.</summary>
        public Func<float> GetFoodEfficiencyMultiplier;

        public Genome Genome
        {
            get => _genome;
            set { _genome = value; ApplyVisuals(); }
        }

        public float Energy => _energy;

        /// <summary>Deduct energy (e.g. for reproduction cost). Clamps to 0. Does nothing if dead.</summary>
        public void DeductEnergy(float amount)
        {
            if (_dead || amount <= 0f) return;
            _energy = Mathf.Max(0f, _energy - amount);
        }

        public float Age => _age;
        public int SpeciesId => _speciesId;
        public bool IsDead => _dead;

        /// <summary>
        /// Returns mutation pressure learned from this organism's own survival history.
        /// This gives NPC organisms the same natural-selection style feedback loop as the player:
        /// diet, predation, damage, and threat escapes nudge offspring genomes.
        /// </summary>
        public BehaviourBias GetAdaptiveMutationBias() => _adaptivePressure.ComputeBias();

        public void ResetAdaptiveMutationBias() => _adaptivePressure.Reset();

        public void RecordFoodExperience(FoodType type) => _adaptivePressure.RecordFoodEaten(type);

        public void RecordPredationExperience()
        {
            _adaptivePressure.RecordFoodEaten(FoodType.Meat);
            _adaptivePressure.RecordCellConsumed();
        }

        public void RecordDamageExperience() => _adaptivePressure.RecordDamageTaken();

        public void RecordThreatEscapeExperience() => _adaptivePressure.RecordPredatorEscaped();

        /// <summary>
        /// Copies currently active organisms into <paramref name="buffer"/> and returns the count.
        /// Null or destroyed references are compacted out of the registry.
        /// </summary>
        public static int CopyActiveInstances(System.Collections.Generic.List<Organism> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            CompactRegistry();
            if (_activeInstances.Count == 0)
            {
                var sceneInstances = FindObjectsByType<Organism>(FindObjectsInactive.Exclude);
                for (int i = 0; i < sceneInstances.Length; i++)
                {
                    var organism = sceneInstances[i];
                    if (organism != null && !_activeInstances.Contains(organism))
                        _activeInstances.Add(organism);
                }
            }

            for (int i = 0; i < _activeInstances.Count; i++)
            {
                var organism = _activeInstances[i];
                if (organism != null && organism.isActiveAndEnabled)
                    buffer.Add(organism);
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

        /// <summary>
        /// Ensures organism colliders are enabled triggers on spawn and pool reuse.
        /// </summary>
        private void EnsureTriggerColliders()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            bool hasSupportedTrigger = false;

            for (int i = 0; colliders != null && i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;

                if (col is MeshCollider)
                {
                    // Keep collision simple and reliable: root sphere trigger handles gameplay overlap.
                    UnityObjectUtil.Destroy(col);
                    continue;
                }

                col.enabled = true;
                col.isTrigger = true;
                hasSupportedTrigger = true;
            }

            if (!hasSupportedTrigger)
            {
                var fallback = GetComponent<SphereCollider>();
                if (fallback == null)
                    fallback = gameObject.AddComponent<SphereCollider>();

                fallback.enabled = true;
                fallback.isTrigger = true;
            }
        }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            EnsureTriggerColliders();
            if (_genome == null)
                _genome = Genome.Default();
            _electricPulseTimer = BalanceConstants.GeneticTraitElectricPulseInterval;
        }

        private void OnEnable()
        {
            EnsureTriggerColliders();
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

        /// <summary>Apply incoming damage, reduced by Armoured trait if active. Dies if energy reaches zero.</summary>
        public void TakeDamage(float amount)
        {
            if (_dead || amount <= 0f) return;
            RecordDamageExperience();
            if (_genome.HasTrait(GeneticTraits.Armoured))
                amount *= 1f - BalanceConstants.GeneticTraitArmouredDamageReduction;
            _energy -= amount;
            if (_energy <= MinEnergyToSurvive)
                Die();
        }

        /// <summary>Applies Electric-trait slow for the configured duration.</summary>
        public void ApplyElectricSlow()
        {
            _electricSlowTimer = BalanceConstants.GeneticTraitElectricSlowDuration;
        }

        private void OnTriggerStay(Collider other)
        {
            if (_dead || !_genome.HasTrait(GeneticTraits.Toxic)) return;
            var target = other.GetComponent<Organism>();
            if (target != null && !target.IsDead)
                target.TakeDamage(BalanceConstants.GeneticTraitToxicContactDrain * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_dead) return;

            if (TryResolvePredationContact(other))
                return;

            var food = other.GetComponent<FoodItem>();
            if (food == null) return;

            float nutrition = food.NutritionValue;
            int id = food.FoodManagerId;
            if (id != 0)
            {
                var fm = FindAnyObjectByType<FoodManager>();
                if (fm != null && fm.ConsumeFood(id))
                    nutrition = food.NutritionValue;
                else
                    return;
            }
            else
            {
                UnityObjectUtil.Destroy(food.gameObject);
            }

            float mult = GetFoodEfficiencyMultiplier != null ? GetFoodEfficiencyMultiplier() : 1f;
            float gained = Eat(nutrition, mult);
            if (gained > 0f)
                RecordFoodExperience(food.Type);
        }

        private void Start()
        {
            ApplyVisuals();
        }

        private void Update()
        {
            if (_dead) return;
            float metMult = GetMetabolismMultiplier != null ? GetMetabolismMultiplier() : 1f;
            Tick(Time.deltaTime, metMult);
            UpdateBioluminescence();
        }

        /// <summary>Apply metabolism drain for deltaTime. Used for fixed timestep and tests. Multiplier 1f = normal (no biome).</summary>
        public void ApplyMetabolism(float deltaTime, float multiplier = 1f)
        {
            if (_dead || deltaTime <= 0f) return;
            _energy -= _genome.MetabolismCost * deltaTime * Mathf.Max(0f, multiplier);
        }

        /// <summary>Advance one timestep: apply metabolism, trait passives, and trigger death if starved. Returns true if organism died this tick. metabolismMultiplier 1f = normal.</summary>
        public bool Tick(float deltaTime, float metabolismMultiplier = 1f)
        {
            if (_dead) return false;
            _age += deltaTime;
            ApplyMetabolism(deltaTime, metabolismMultiplier);
            ApplyTraitPassives(deltaTime);
            if (_energy <= MinEnergyToSurvive)
            {
                Die();
                return true;
            }
            return false;
        }

        private void ApplyTraitPassives(float deltaTime)
        {
            if (_genome == null) return;

            if (_genome.HasTrait(GeneticTraits.Photosynthetic))
                _energy += BalanceConstants.GeneticTraitPhotosyntheticEnergyRate
                           * CellEnvironmentEventManager.GlobalPhotosynthesisTraitMultiplier
                           * deltaTime;

            if (_genome.HasTrait(GeneticTraits.Regenerative) && _energy > 10f)
                _energy += BalanceConstants.GeneticTraitRegenerativeRate * deltaTime;

            if (_electricSlowTimer > 0f)
                _electricSlowTimer -= deltaTime;

            if (_genome.HasTrait(GeneticTraits.Electric))
            {
                _electricPulseTimer -= deltaTime;
                if (_electricPulseTimer <= 0f)
                {
                    ApplyElectricPulse();
                    _electricPulseTimer = BalanceConstants.GeneticTraitElectricPulseInterval;
                }
            }
        }

        private void ApplyElectricPulse()
        {
            var hits = Physics.OverlapSphere(transform.position,
                BalanceConstants.GeneticTraitElectricPulseRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                hit.GetComponent<Organism>()?.ApplyElectricSlow();
            }
        }

        /// <summary>Apply genome size and color to the visual (sphere scale and material color).</summary>
        public void ApplyVisuals()
        {
            if (_genome == null) return;

            float scale = BaseScale * GetEffectiveGenomeSize() * BalanceConstants.CellEntityVisualScaleMultiplier;
            transform.localScale = new Vector3(scale, scale, scale);
            ProceduralCellGenerator.ApplyGenomeVisuals(gameObject, _genome);

            if (Application.isPlaying && _renderer != null && _renderer.material != null)
            {
                var opaqueColor = ProceduralTextureFactory.Opaque(_genome.Color);
                _renderer.material.color = opaqueColor;
            }

            UpdateBioluminescence();
        }

        /// <summary>Sets emission intensity on the body material proportional to current energy (bioluminescence effect).</summary>
        private void UpdateBioluminescence()
        {
            if (!Application.isPlaying || _renderer == null || _renderer.material == null || _genome == null) return;
            float energyNorm = Mathf.Clamp01(_energy / 100f);
            float intensity = Mathf.Lerp(0f, 0.55f, energyNorm);
            Color emission = ProceduralTextureFactory.Opaque(_genome.Color) * intensity;
            emission.a = 1f;
            _renderer.material.EnableKeyword("_EMISSION");
            _renderer.material.SetColor("_EmissionColor", emission);
        }

        /// <summary>Move in a direction for this frame. Uses genome Speed.</summary>
        public void Move(Vector3 direction)
        {
            Move(direction, Time.deltaTime);
        }

        /// <summary>Move in a direction for a given delta time. Used for testing and fixed timestep. speedMultiplier 1f = normal (no biome).</summary>
        public void Move(Vector3 direction, float deltaTime, float speedMultiplier = 1f)
        {
            if (_dead) return;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f && deltaTime > 0f)
            {
                Vector3 separation = CellSeparation.GetDirection(transform);
                direction = CellSeparation.Apply(direction, separation);
                float slowMult = _electricSlowTimer > 0f ? 0.4f : 1f;
                float speed = _genome.Speed * 5f * Mathf.Max(0f, speedMultiplier) * slowMult;
                transform.position += (direction * speed + separation * (speed * CellSeparation.PushSpeedMultiplier)) * deltaTime;
            }
        }

        /// <summary>Consume food: gain energy scaled by digestion. Returns actual energy gained. foodEfficiencyMultiplier 1f = normal (e.g. from biome).</summary>
        public float Eat(float nutritionValue, float foodEfficiencyMultiplier = 1f)
        {
            if (_dead) return 0f;
            float gained = nutritionValue * _genome.FoodEfficiency * Mathf.Max(0f, foodEfficiencyMultiplier);
            _energy += gained;
            ApplyGrowthFromNutrition(gained);
            return gained;
        }

        /// <summary>
        /// Sets a callback invoked when this organism dies (instead of destroying the GameObject).
        /// Used by <see cref="OrganismPool"/> to return the instance to the pool.
        /// Pass null to restore default destroy-on-death behaviour.
        /// </summary>
        public void SetPoolCallback(System.Action<Organism> callback) => _poolReturnCallback = callback;

        /// <summary>
        /// Reinitialises this organism so it can be reused by a pool without recreating the GameObject.
        /// Resets energy, age, dead-flag, and species; updates position and visuals.
        /// </summary>
        public void Reinitialize(Vector3 position, Genome genome, float energy, int speciesId)
        {
            transform.position = position;
            _genome = genome ?? Genome.Default();
            _energy = Mathf.Max(0f, energy);
            _age = 0f;
            _speciesId = speciesId;
            _dead = false;
            _growthSizeBonus = 0f;
            _electricPulseTimer = BalanceConstants.GeneticTraitElectricPulseInterval;
            _electricSlowTimer = 0f;
            ResetAdaptiveMutationBias();
            GetMetabolismMultiplier = null;
            GetFoodEfficiencyMultiplier = null;
            gameObject.SetActive(true);
            ApplyVisuals();
            GetComponent<OrganismLOD>()?.RefreshChildren();
        }

        /// <summary>Mark organism dead. Publishes CellOrganismDiedEvent; returns to pool when one is set, otherwise destroys the GameObject.</summary>
        public void Die()
        {
            if (_dead) return;
            _dead = true;
            EventBus.Publish(new CellOrganismDiedEvent
            {
                SpeciesId = _speciesId,
                Position = transform.position,
                VisualScale = transform.localScale.x
            });
            if (_poolReturnCallback != null)
                _poolReturnCallback(this);
            else if (Application.isPlaying)
                Destroy(gameObject);
            // In edit mode (tests), skip destruction — test teardown cleans up.
        }

        /// <summary>Set species id (e.g. after speciation). Used by ReproductionSystem when spawning offspring.</summary>
        public void SetSpeciesId(int speciesId)
        {
            _speciesId = speciesId;
        }

        /// <summary>
        /// Creates a single organism in the scene with a sphere visual (scale and color from genome).
        /// Use for testing or spawning the first organism. Aligns with Step 1 "single organism in scene".
        /// </summary>
        public static Organism Create(Vector3 position, Genome genome = null)
        {
            return Create(position, genome, InitialEnergy, 0);
        }

        /// <summary>
        /// Creates an organism with a given initial energy (e.g. for offspring from reproduction).
        /// </summary>
        public static Organism Create(Vector3 position, Genome genome, float initialEnergy)
        {
            return Create(position, genome, initialEnergy, 0);
        }

        /// <summary>
        /// Creates an organism with initial energy and species id (for reproduction and speciation).
        /// </summary>
        public static Organism Create(Vector3 position, Genome genome, float initialEnergy, int speciesId)
        {
            genome = genome ?? Genome.Default();
            GameObject go = ProceduralCellGenerator.CreateCellFromGenome(position, genome);
            go.name = "Organism";
            var org = go.AddComponent<Organism>();
            org.Genome = genome;
            org._energy = Mathf.Max(0f, initialEnergy);
            org._speciesId = speciesId;
            go.AddComponent<OrganismLOD>();
            return org;
        }

        private float GetEffectiveGenomeSize()
        {
            if (_genome == null)
                return MinimumGenomeSize;

            return Mathf.Max(MinimumGenomeSize, _genome.Size + _growthSizeBonus);
        }

        private void ApplyGrowthFromNutrition(float gainedNutrition)
        {
            if (gainedNutrition <= 0f || _dead)
                return;

            _growthSizeBonus += gainedNutrition * FoodGrowthToSizeFactor;
            _growthSizeBonus = Mathf.Clamp(_growthSizeBonus, 0f, MaximumGrowthSizeBonus);
            ApplyVisuals();
        }

        private bool TryResolvePredationContact(Collider other)
        {
            if (other == null || _genome == null || _dead)
                return false;

            float myScale = transform.localScale.x;

            var otherOrganism = FindComponentOnCollider<Organism>(other);
            if (otherOrganism != null && otherOrganism != this && !otherOrganism.IsDead)
            {
                float otherScale = otherOrganism.transform.localScale.x;
                if (otherScale <= myScale * PreyConsumeSizeRatio)
                {
                    Eat(Mathf.Max(0.2f, otherScale * PredationNutritionPerScale));
                    RecordPredationExperience();
                    otherOrganism.Die();
                    return true;
                }

                if (otherScale >= myScale * ThreatContactSizeRatio)
                    TakeDamage(ContactPredationDamage);

                return false;
            }

            var otherCell = FindComponentOnCollider<CellAI>(other);
            if (otherCell != null && otherCell != GetComponent<CellAI>())
            {
                float otherScale = otherCell.transform.localScale.x;
                if (otherScale <= myScale * PreyConsumeSizeRatio)
                {
                    Eat(Mathf.Max(0.2f, otherScale * PredationNutritionPerScale * 0.85f));
                    RecordPredationExperience();
                    UnityObjectUtil.Destroy(otherCell.gameObject);
                    return true;
                }

                if (otherScale >= myScale * ThreatContactSizeRatio)
                    TakeDamage(ContactPredationDamage);

                return false;
            }

            var player = FindComponentOnCollider<PlayerCell>(other);
            if (player != null && !player.IsDead)
            {
                float playerScale = player.transform.localScale.x;
                if (myScale >= playerScale * ThreatContactSizeRatio)
                {
                    player.TakeDamage(ContactPredationDamage);
                    Eat(ContactPredationDamage * 0.9f);
                    RecordPredationExperience();
                    return true;
                }
            }

            return false;
        }

        private static T FindComponentOnCollider<T>(Collider other) where T : Component
        {
            if (other == null) return null;
            var direct = other.GetComponent<T>();
            if (direct != null) return direct;

            var attachedBody = other.attachedRigidbody;
            if (attachedBody != null)
            {
                var onBody = attachedBody.GetComponent<T>();
                if (onBody != null) return onBody;
            }

            return other.GetComponentInParent<T>();
        }
    }
}

