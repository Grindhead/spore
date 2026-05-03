using UnityEngine;

namespace RoobsWorld.Utils
{
    public static class TempAssetBuilder
    {
        public static Material CreateTempMaterial(Color color, string name = "TempMat")
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }

        public static GameObject CreateTempCreature(Vector3 position, Color bodyColor)
        {
            return ModelFactory.CreateCreatureModel(position, bodyColor);
        }

        public static GameObject CreateTempPlanet(float radius, Color color)
        {
            return ModelFactory.CreatePlanet(Vector3.zero, radius, color);
        }

        public static GameObject CreateTempBuilding(Vector3 position, Vector3 size, Color color)
        {
            return ModelFactory.CreateHut(position, size.x, size.y, color);
        }
    }
}
