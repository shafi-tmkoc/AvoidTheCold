using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Keeps this SpriteRenderer scaled and centered so it always fully
    /// covers the camera's current visible area (cover-fit: uniformly
    /// scaled so it never stretches/distorts, may crop a little off one
    /// edge on extreme aspect ratios instead of ever leaving a gap).
    /// CameraFit2D does the opposite for the gameplay board - contain-fit,
    /// so the board stays fully visible but the camera can show MORE than
    /// the reference board on some aspects; this is what covers that
    /// extra area for background art like Board2D/BG.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundCoverFit2D : MonoBehaviour
    {
        [Tooltip("Camera whose visible area this background must always cover - defaults to Camera.main")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Extra margin added to the covered area on each side, in world units, so the background clears the edge with room to spare")]
        [SerializeField] private float safetyMargin = 0.25f;

        private SpriteRenderer _spriteRenderer;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;
            Apply();
        }

        private void Update()
        {
            // Cheap guard: only recompute when the screen actually changes
            // size (device rotation, resizable window) instead of every frame -
            // same pattern as CameraFit2D.
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;
            Apply();
        }

        /// <summary>Recomputes scale/position so this sprite fully covers the camera's current visible area.</summary>
        public void Apply()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;

            if (_spriteRenderer == null || _spriteRenderer.sprite == null || targetCamera == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            // Camera's current visible world-space size, plus a small safety margin.
            float camHalfHeight = targetCamera.orthographicSize;
            float camHalfWidth = camHalfHeight * targetCamera.aspect;
            float visibleWidth = camHalfWidth * 2f + safetyMargin * 2f;
            float visibleHeight = camHalfHeight * 2f + safetyMargin * 2f;

            // Sprite's native (unscaled) world size.
            var sprite = _spriteRenderer.sprite;
            float nativeWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float nativeHeight = sprite.rect.height / sprite.pixelsPerUnit;
            if (nativeWidth <= 0f || nativeHeight <= 0f) return;

            // Compensate for the parent's own scale so the sprite renders at
            // the requested WORLD size regardless of how Board2D is scaled.
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float safeParentX = Mathf.Max(0.0001f, parentScale.x);
            float safeParentY = Mathf.Max(0.0001f, parentScale.y);

            // Cover-fit: uniform scale so BOTH dimensions meet/exceed the
            // camera's visible size - the larger of the two required scales
            // wins, so neither axis ever falls short (the other may crop).
            float scaleForWidth = (visibleWidth / nativeWidth) / safeParentX;
            float scaleForHeight = (visibleHeight / nativeHeight) / safeParentY;
            float scale = Mathf.Max(scaleForWidth, scaleForHeight);

            transform.localScale = new Vector3(scale, scale, transform.localScale.z);

            // Recenter on the camera so it stays perfectly aligned as the
            // visible area grows/shrinks with the aspect ratio.
            Vector3 pos = transform.position;
            pos.x = targetCamera.transform.position.x;
            pos.y = targetCamera.transform.position.y;
            transform.position = pos;

            Debug.Log($"[BackgroundCoverFit2D] {name}: Screen {Screen.width}x{Screen.height} camVisible={visibleWidth:0.00}x{visibleHeight:0.00} native={nativeWidth:0.00}x{nativeHeight:0.00} -> scale={scale:0.000}");
        }
    }
}
