using UnityEngine;

namespace RoobsWorld.Creature.Animation
{
    /// <summary>
    /// Simple FABRIK inverse-kinematics solver operating on world-space joint chains.
    /// </summary>
    public static class FABRIKSolver
    {
        public static Vector3[] Solve(Vector3[] chain, Vector3 target, float tolerance = 0.01f, int maxIterations = 10)
        {
            int n = chain.Length;
            if (n < 2)
                return (Vector3[])chain.Clone();

            var result = (Vector3[])chain.Clone();
            var lengths = new float[n - 1];
            float totalLength = 0f;

            for (int i = 0; i < n - 1; i++)
            {
                lengths[i] = Vector3.Distance(result[i], result[i + 1]);
                totalLength += lengths[i];
            }

            Vector3 root = result[0];
            float distToTarget = Vector3.Distance(root, target);

            if (distToTarget > totalLength)
            {
                Vector3 dir = (target - root).normalized;
                for (int i = 1; i < n; i++)
                    result[i] = result[i - 1] + dir * lengths[i - 1];
                return result;
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                float endDist = Vector3.Distance(result[n - 1], target);
                if (endDist < tolerance)
                    break;

                // Forward pass: end effector → root
                result[n - 1] = target;
                for (int i = n - 2; i >= 0; i--)
                {
                    Vector3 dir = (result[i] - result[i + 1]).normalized;
                    result[i] = result[i + 1] + dir * lengths[i];
                }

                // Backward pass: root → end effector
                result[0] = root;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 dir = (result[i + 1] - result[i]).normalized;
                    result[i + 1] = result[i] + dir * lengths[i];
                }
            }

            return result;
        }
    }
}
