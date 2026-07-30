using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Keeps a fixed-size gameplay board fully visible on any aspect ratio by
    /// adjusting the orthographic camera's size (contain-fit: never crops the
    /// board, may show a little extra background on very different aspect
    /// ratios). Reference width/height are in world units and should match
    /// the board's actual footprint once it's laid out in world space.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFit2D : MonoBehaviour
    {
        [Tooltip("Board width in world units that must always stay fully visible")]
        [SerializeField] private float referenceWidth = 11.3066f;

        [Tooltip("Board height in world units that must always stay fully visible")]
        [SerializeField] private float referenceHeight = 8.819f;

        private Camera _camera;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            if (!TryGetComponent(out _camera))
            {
                Debug.Log("[CameraFit2D] No Camera component found - disabling");
                enabled = false;
                return;
            }

            if (!_camera.orthographic)
                Debug.Log("[CameraFit2D] Camera is not orthographic - this script expects an orthographic 2D camera");

            Apply();
        }

        private void Update()
        {
            // Cheap guard: only recompute when the screen actually changes size
            // (device rotation, resizable window) instead of every frame.
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;
            Apply();
        }

        /// <summary>Recomputes orthographic size so the reference board fits fully on screen.</summary>
        public void Apply()
        {
            if (_camera == null || Screen.width <= 0 || Screen.height <= 0) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            float screenAspect = (float)Screen.width / Screen.height;
            float referenceAspect = referenceWidth / referenceHeight;

            float size;
            if (screenAspect >= referenceAspect)
            {
                // Screen is wider than the board - height is the limiting dimension.
                size = referenceHeight / 2f;
            }
            else
            {
                // Screen is narrower/taller than the board (typical portrait phone) -
                // width is the limiting dimension, derive size from it.
                size = (referenceWidth / screenAspect) / 2f;
            }

            _camera.orthographicSize = size;
            Debug.Log($"[CameraFit2D] Screen {Screen.width}x{Screen.height} (aspect={screenAspect:0.00}) -> orthographicSize={size:0.00}");
        }
    }
}
