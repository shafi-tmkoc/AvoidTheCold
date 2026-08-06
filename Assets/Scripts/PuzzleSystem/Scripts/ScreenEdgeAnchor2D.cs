using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Positions this object a fixed world-unit margin from a chosen screen
    /// edge, independently on X (Left/Right) and Y (Bottom/Top), recomputed
    /// from the camera's live visible world-space rect - so it stays pinned
    /// to that edge on any device/aspect ratio instead of a fixed world
    /// position. Reusable anywhere an object needs "N units from the left"
    /// or "N units from the top-right corner" type placement (e.g. Cupboard,
    /// HUD world props) without one-off margin math per script.
    /// </summary>
    [ExecuteAlways]
    public class ScreenEdgeAnchor2D : MonoBehaviour
    {
        public enum HorizontalEdge { Left, Right }
        public enum VerticalEdge { Bottom, Top }

        [SerializeField] private Camera targetCamera;

        [Header("Horizontal (X)")]
        [Tooltip("Uncheck to leave this object's X position untouched")]
        [SerializeField] private bool anchorX = true;
        [SerializeField] private HorizontalEdge horizontalEdge = HorizontalEdge.Left;
        [Tooltip("World units from the chosen horizontal edge")]
        [SerializeField] private float marginX = 0f;

        [Header("Vertical (Y)")]
        [Tooltip("Uncheck to leave this object's Y position untouched")]
        [SerializeField] private bool anchorY = true;
        [SerializeField] private VerticalEdge verticalEdge = VerticalEdge.Bottom;
        [Tooltip("World units from the chosen vertical edge")]
        [SerializeField] private float marginY = 0f;

        private int _lastScreenWidth, _lastScreenHeight;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            Apply();
        }

        private void Update()
        {
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;
            Apply();
        }

        /// <summary>Recomputes and applies this object's position from the current camera/margins - call again after changing values at runtime or from an Editor tool.</summary>
        [ContextMenu("Apply")]
        public void Apply()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                Debug.Log("[ScreenEdgeAnchor2D] No camera available - skipping");
                return;
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            Rect rect = GetCameraVisibleWorldRect(cam);
            Vector3 pos = transform.position;

            if (anchorX)
                pos.x = horizontalEdge == HorizontalEdge.Left ? rect.xMin + marginX : rect.xMax - marginX;

            if (anchorY)
                pos.y = verticalEdge == VerticalEdge.Bottom ? rect.yMin + marginY : rect.yMax - marginY;

            transform.position = pos;
            Debug.Log($"[ScreenEdgeAnchor2D] {name} -> {pos} (rect={rect})");
        }

        private static Rect GetCameraVisibleWorldRect(Camera cam)
        {
            if (!cam.orthographic)
            {
                Debug.Log("[ScreenEdgeAnchor2D] Camera is not orthographic - cannot compute a reliable world rect");
                return new Rect(-8f, -8f, 16f, 16f);
            }

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 center = cam.transform.position;
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
