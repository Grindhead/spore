using UnityEngine;

namespace RoobsWorld.Utils
{
    public static class MathUtils
    {
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        public static Vector3 RandomPointOnCircle(float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }

        public static Vector3 RandomPointInCircle(float radius)
        {
            float r = Mathf.Sqrt(Random.value) * radius;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
        }

        public static float SmoothDamp01(float current, float target, ref float velocity, float smoothTime)
        {
            return Mathf.SmoothDamp(current, target, ref velocity, smoothTime);
        }

        public static int StableHash(string input)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in input)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }

    /// <summary>
    /// Small Unity lifecycle helpers shared by runtime code and EditMode tests.
    /// </summary>
    public static class UnityObjectUtil
    {
        public static void Destroy(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
