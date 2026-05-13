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

        private readonly Collider[] _overlapBuffer = new Collider[16];

        private void Update()
        {
            if (_triggered || _prefabsToSpawn == null || _prefabsToSpawn.Length == 0) return;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit.CompareTag("Player"))
                {
                    _triggered = true;
                    SpawnPrefabs(hit.transform);
                    return;
                }
            }

            if (hitCount == _overlapBuffer.Length)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
                for (int i = _overlapBuffer.Length; i < hits.Length; i++)
                {
                    if (hits[i].CompareTag("Player"))
                    {
                        _triggered = true;
                        SpawnPrefabs(hits[i].transform);
                        return;
                    }
                }
            }
        }

        private void SpawnPrefabs(Transform player)
        {
            foreach (GameObject prefab in _prefabsToSpawn)
            {
                if (prefab == null) continue;
                Vector3 candidate = transform.position + new Vector3(
                    Random.Range(-_spawnOffset.x, _spawnOffset.x),
                    _spawnOffset.y,
                    Random.Range(-_spawnOffset.z, _spawnOffset.z)
                );
                Vector3 pos = GetSpawnPositionBehindPlayer(candidate, player);
                Instantiate(prefab, pos, Quaternion.identity);
            }
        }

        private static Vector3 GetSpawnPositionBehindPlayer(Vector3 candidate, Transform player)
        {
            if (player == null)
                return candidate;

            return RoobsWorld.Stages.Cell.CellStage.MovePointBehindPlayer(
                player.position,
                player.forward,
                candidate,
                1f);
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
