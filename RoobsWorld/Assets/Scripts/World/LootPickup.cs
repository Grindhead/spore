using UnityEngine;

namespace RoobsWorld.World
{
    /// <summary>
    /// When a player enters the trigger, the object is destroyed (picked up).
    /// Optionally can be extended to publish events (e.g. EventBus) for UI/inventory.
    /// Used by the AI level designer for loot placement.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LootPickup : MonoBehaviour
    {
        [SerializeField] private bool _requireTrigger = true;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (_requireTrigger && !col.isTrigger)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            OnPickedUp();
            Destroy(gameObject);
        }

        /// <summary>Override or hook for UI/inventory; default just destroys.</summary>
        protected virtual void OnPickedUp()
        {
            // Extend: e.g. EventBus.Publish(new LootPickedUpEvent { LootId = ... });
        }
    }
}
