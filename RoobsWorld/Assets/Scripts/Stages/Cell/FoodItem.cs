using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Identifies a pickup as consumable food in the Cell stage, with type and nutrition value.
    /// </summary>
    public enum FoodType
    {
        Plant,
        Meat,
        Toxin
    }

    /// <summary>
    /// Component marking a cell-stage food object and configuring its nutrition.
    /// </summary>
    public class FoodItem : MonoBehaviour
    {
        private const float MinimumWorldTriggerRadius = 0.4f;

        [SerializeField] private FoodType _foodType = FoodType.Plant;
        [SerializeField] private float _nutritionValue = 0.1f;
        [SerializeField] private int _foodManagerId;

        public FoodType Type => _foodType;
        public float NutritionValue => _nutritionValue;
        /// <summary>Id used by FoodManager to remove/destroy when consumed. 0 = not managed.</summary>
        public int FoodManagerId => _foodManagerId;

        /// <summary>Set type and nutrition from code (e.g. FoodManager spawn).</summary>
        public void Initialize(FoodType foodType, float nutritionValue)
        {
            _foodType = foodType;
            _nutritionValue = nutritionValue;
        }

        /// <summary>Set FoodManager id so manager can remove when consumed.</summary>
        public void SetFoodManagerId(int id)
        {
            _foodManagerId = id;
        }

        private void Awake()
        {
            EnsureTriggerColliders();
        }

        private void OnEnable()
        {
            EnsureTriggerColliders();
        }

        /// <summary>
        /// Ensures every food collider is an enabled trigger.
        /// This runs before first physics step so collisions are reliable on spawn.
        /// </summary>
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
                    // Decorative Blender mesh colliders can be too tiny/non-manifold for trigger hulls.
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
