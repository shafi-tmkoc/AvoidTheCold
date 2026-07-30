using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Converts a world-space point (e.g. a piece or slot living in Board2D)
    /// into a Screen Space - Overlay canvas's local anchored-position space,
    /// so UI-based hint/pointer elements can still track world objects.
    /// </summary>
    public static class WorldToCanvasUtility
    {
        public static Vector2 WorldToCanvasPoint(Vector3 worldPosition, RectTransform canvasRect, Camera worldCamera)
        {
            if (canvasRect == null) return Vector2.zero;

            Vector3 screenPoint = worldCamera != null
                ? worldCamera.WorldToScreenPoint(worldPosition)
                : worldPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }
    }
}
