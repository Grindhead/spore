using UnityEngine;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// Utility for rendering a temporary camera view of a creature into a thumbnail texture or PNG bytes.
    /// </summary>
    public static class CreatureThumbnail
    {
        private const int THUMBNAIL_SIZE = 256;
        private const float CAMERA_DISTANCE = 3f;
        private const float CAMERA_HEIGHT = 1.5f;

        public static Texture2D Capture(GameObject creatureRoot)
        {
            if (creatureRoot == null) return null;

            var camObj = new GameObject("ThumbnailCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 1f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;

            camObj.transform.position = creatureRoot.transform.position
                + new Vector3(0, CAMERA_HEIGHT, CAMERA_DISTANCE);
            camObj.transform.LookAt(creatureRoot.transform.position + Vector3.up * 0.5f);

            var rt = new RenderTexture(THUMBNAIL_SIZE, THUMBNAIL_SIZE, 24);
            cam.targetTexture = rt;
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(THUMBNAIL_SIZE, THUMBNAIL_SIZE, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, THUMBNAIL_SIZE, THUMBNAIL_SIZE), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);

            return tex;
        }

        public static byte[] CaptureAsPNG(GameObject creatureRoot)
        {
            var tex = Capture(creatureRoot);
            if (tex == null) return null;
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }
    }
}
