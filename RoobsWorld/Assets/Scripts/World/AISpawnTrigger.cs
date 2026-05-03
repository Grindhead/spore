using UnityEngine;

namespace RoobsWorld.World
{
    /// <summary>
    /// When a player enters the trigger radius, spawns the configured prefabs once.
    /// Used by the AI level designer to wire enemy spawn zones. Prefabs are assigned in editor.
    /// </summary>
    public class AISpawnTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject[] _prefabsToSpawn = System.Array.Empty<GameObject>();
        [SerializeField] private float _radius = 5f;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(5f, 0f, 5f);
        [SerializeField] private bool _triggered;

        private void Update()
        {
            if (_triggered || _prefabsToSpawn == null || _prefabsToSpawn.Length == 0) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    _triggered = true;
                    SpawnPrefabs();
                    return;
                }
            }
        }

        private void SpawnPrefabs()
        {
            foreach (GameObject prefab in _prefabsToSpawn)
            {
                if (prefab == null) continue;
                Vector3 pos = transform.position + new Vector3(
                    Random.Range(-_spawnOffset.x, _spawnOffset.x),
                    _spawnOffset.y,
                    Random.Range(-_spawnOffset.z, _spawnOffset.z)
                );
                Instantiate(prefab, pos, Quaternion.identity);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
#endif
    }
}
