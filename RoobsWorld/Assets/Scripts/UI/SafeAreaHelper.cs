using UnityEngine;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Applies device safe area (notches, rounded corners, system UI) to full-screen UI roots
    /// so content stays visible on desktop (multi-monitor, taskbar) and mobile (notches, home indicator).
    /// </summary>
    public static class SafeAreaHelper
    {
        /// <summary>
        /// Applies Screen.safeArea to the given RectTransform so its layout matches the safe rectangle.
        /// Call when building UI or when resolution/orientation changes.
        /// </summary>
        public static void Apply(RectTransform rect)
        {
            if (rect == null) return;
            Rect safe = Screen.safeArea;
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0 || h <= 0) return;
            rect.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            rect.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Returns whether the current device has a non-fullscreen safe area (e.g. notch or taskbar).
        /// </summary>
        public static bool HasSafeAreaInsets()
        {
            Rect safe = Screen.safeArea;
            return safe.xMin > 0 || safe.yMin > 0 || safe.xMax < Screen.width || safe.yMax < Screen.height;
        }
    }
}
