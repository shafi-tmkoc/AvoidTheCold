using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Pure helper for fitting a native-sized rect inside a bounding box while
    /// preserving its aspect ratio (letterbox-style), so a piece's shape
    /// never gets stretched/distorted when its slot size differs from its
    /// original prefab size.
    /// </summary>
    public static class AspectFitUtility
    {
        /// <summary>Returns the largest size with nativeSize's aspect ratio that fits entirely inside box.</summary>
        public static Vector2 FitWithinBox(Vector2 nativeSize, Vector2 box)
        {
            if (box.x <= 0f || box.y <= 0f) return nativeSize;
            if (nativeSize.x <= 0f || nativeSize.y <= 0f) return box;

            float scale = Mathf.Min(box.x / nativeSize.x, box.y / nativeSize.y);
            return new Vector2(nativeSize.x * scale, nativeSize.y * scale);
        }
    }
}
