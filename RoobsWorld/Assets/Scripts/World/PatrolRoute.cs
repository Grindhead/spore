using UnityEngine;

namespace RoobsWorld.World
{
    /// <summary>
    /// Holds waypoints for AI patrol. Can be filled by MCP/editor or at runtime.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        [SerializeField] private Vector3[] _points = System.Array.Empty<Vector3>();

        /// <summary>Waypoints for patrol (world space).</summary>
        public Vector3[] Points => _points;

        /// <summary>Generate a random route around a center. Used by AI level designer.</summary>
        public void GenerateRandomRoute(Vector3 center, float radius, int pointCount)
        {
            pointCount = Mathf.Max(1, pointCount);
            _points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                _points[i] = center + new Vector3(
                    Random.Range(-radius, radius),
                    0f,
                    Random.Range(-radius, radius)
                );
            }
        }

        /// <summary>Set points directly (e.g. from editor/MCP).</summary>
        public void SetPoints(Vector3[] points)
        {
            _points = points ?? System.Array.Empty<Vector3>();
        }
    }
}
