using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AvoidTheCold
{
    /// <summary>
    /// Attach to a draggable puzzle piece (world-space SpriteRenderer under
    /// Board2D). Polls the New Input System for mouse/touch press-drag-release
    /// directly on this piece's own Collider2D, snaps into a matching
    /// ShapeDropSlot on a correct drop, and bounces back to its start position
    /// otherwise. Same public API as the old UI version (ShapeId, Initialize,
    /// OnReturnedToStart, SnapToSlot, ReturnToStart) so everything else that
    /// listens to a piece (VO triggers, tutorials, progress tracker) keeps
    /// working unchanged.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class DraggableShape : MonoBehaviour
    {
        [Tooltip("Must match the ShapeDropSlot this piece belongs to")]
        [SerializeField] private string shapeId;

        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private Camera _camera;

        private Transform _startParent;
        private Vector3 _startPosition;
        private int _startSortingOrder;

        private bool _isPlaced;
        private bool _isDragging;
        private Vector3 _dragOffset;

        public string ShapeId => shapeId;

        /// <summary>Raised whenever this piece bounces back to its start (wrong slot or empty space).</summary>
        public event Action OnReturnedToStart;

        /// <summary>Raised once this piece snaps into its correct slot.</summary>
        public event Action OnPlacedSuccessfully;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _camera = Camera.main;

            if (_camera == null)
                Debug.Log("[DraggableShape] No main camera found - dragging will not work until one is tagged MainCamera");

            CacheStartTransform();
        }

        private void CacheStartTransform()
        {
            _startParent = transform.parent;
            _startPosition = transform.position;
            _startSortingOrder = _spriteRenderer.sortingOrder;
        }

        /// <summary>
        /// Configures a freshly spawned piece: sets its shape id and start
        /// (tray) world position, then re-caches that as its "start".
        /// </summary>
        public void Initialize(string id, Vector3 trayPosition)
        {
            shapeId = id;
            _isPlaced = false;
            _isDragging = false;
            if (_collider != null) _collider.enabled = true;
            transform.position = trayPosition;
            CacheStartTransform();
        }

        private void Update()
        {
            if (_isPlaced || _camera == null) return;

            if (_isDragging)
            {
                ContinueDrag();
                if (WasReleasedThisFrame()) EndDrag();
                return;
            }

            if (WasPressedThisFrame(out Vector2 screenPos))
            {
                Vector3 worldPoint = ScreenToWorld(screenPos);
                //Debug.Log($"[DraggableShape] {name} press: screenPos={screenPos} Screen={Screen.width}x{Screen.height} cam={_camera.name} camPos={_camera.transform.position} orthoSize={_camera.orthographicSize} -> worldPoint={worldPoint} pieceTransformPos={transform.position}");
                if (_collider.OverlapPoint(worldPoint))
                    BeginDrag(worldPoint);
                else
                    Debug.Log($"[DraggableShape] {name} press missed collider (OverlapPoint false) - collider bounds min={_collider.bounds.min} max={_collider.bounds.max}");
            }
        }

        private void BeginDrag(Vector3 worldPoint)
        {
            Debug.Log($"[DraggableShape] Begin drag: {name} (shapeId={shapeId})");
            _isDragging = true;
            _dragOffset = transform.position - worldPoint;
            _spriteRenderer.sortingOrder = 1000; // render above everything else while dragging
        }

        private void ContinueDrag()
        {
            Vector2 screenPos = CurrentPointerScreenPos();
            Vector3 worldPoint = ScreenToWorld(screenPos);
            Vector3 newPos = worldPoint + _dragOffset;
            newPos.z = _startPosition.z;
            transform.position = newPos;

            if (WasReleasedThisFrame())
                Debug.Log($"[DraggableShape] {name} release: screenPos={screenPos} Screen={Screen.width}x{Screen.height} cam={_camera.name} camPos={_camera.transform.position} orthoSize={_camera.orthographicSize} -> worldPoint={worldPoint} dragOffset={_dragOffset} finalPiecePos={newPos}");
        }

        private void EndDrag()
        {
            _isDragging = false;
            _spriteRenderer.sortingOrder = _startSortingOrder;

            var slot = FindOverlappingSlot();
            if (slot != null && slot.TryAccept(this))
                return; // slot already called SnapToSlot

            ReturnToStart();
        }

        /// <summary>
        /// Finds the slot whose CENTER is closest to this piece's center,
        /// among slots the piece's bounds are currently touching. Picking by
        /// nearest-center rather than largest box-overlap area matters
        /// because many pieces are triangular/non-rectangular but still use
        /// rectangular BoxCollider2D bounds - two triangles sharing a
        /// diagonal edge have bounding boxes that overlap each other
        /// heavily, so "largest rectangular overlap" can pick the wrong
        /// neighboring slot even when the piece is visually on the correct
        /// one. Center-distance is forgiving for small children's imprecise
        /// dragging without depending on collider shape precision.
        /// </summary>
        private ShapeDropSlot FindOverlappingSlot()
        {
            // Force sync so Physics2D sees the position we just set in ContinueDrag
            Physics2D.SyncTransforms();

            Bounds pieceBounds = _collider.bounds;
            Vector2 queryCenter = pieceBounds.center;
            Vector2 querySize = pieceBounds.size;

            var hits = Physics2D.OverlapBoxAll(queryCenter, querySize, 0f);

            Debug.Log($"[DraggableShape] {name} queryCenter={queryCenter} querySize={querySize} -> {hits.Length} hit(s)");

            ShapeDropSlot best = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var slot = hit.GetComponent<ShapeDropSlot>();
                if (slot == null) continue;

                // Skip disabled colliders
                if (!hit.enabled) continue;

                float distSqr = ((Vector2)hit.bounds.center - queryCenter).sqrMagnitude;
                Debug.Log($"[DraggableShape]   candidate {slot.name}: centerDist={Mathf.Sqrt(distSqr)}");

                if (distSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distSqr;
                    best = slot;
                }
            }

            Debug.Log($"[DraggableShape] Best slot: {(best != null ? best.name : "null")} (centerDist={(best != null ? Mathf.Sqrt(bestDistanceSqr) : 0f)})");
            return best;
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            float depth = _camera.transform.position.z * -1f;
            Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, depth);
            Vector3 world = _camera.ScreenToWorldPoint(screenPoint);
            world.z = 0f;
            return world;
        }

        private static bool WasPressedThisFrame(out Vector2 screenPos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
            screenPos = default;
            return false;
        }

        private static bool WasReleasedThisFrame()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            return false;
        }

        private static Vector2 CurrentPointerScreenPos()
        {
            // On the release frame itself, primaryTouch.press.isPressed has
            // already flipped to false (the touch is lifting), so it must
            // still be checked here via wasReleasedThisFrame - otherwise this
            // falls through to Mouse.current, which reports a stale (0,0) on
            // a touch-only device and teleports the piece right as we're
            // about to check which slot it's over.
            if (Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }

        /// <summary>Called by a matching ShapeDropSlot on a correct drop.</summary>
        public void SnapToSlot(Transform slot)
        {
            Debug.Log($"[DraggableShape] Snapped: {name} (shapeId={shapeId}) -> slot {slot.name}");
            _isPlaced = true;
            transform.SetParent(slot, true);
            transform.position = slot.position;
            transform.localScale = Vector3.one;
            if (_collider != null) _collider.enabled = false;
            if (AudioManager.Instance != null) AudioManager.Instance.Connect();
            OnPlacedSuccessfully?.Invoke();
        }

        /// <summary>Bounces the piece back to where it started (wrong slot or dropped in empty space).</summary>
        public void ReturnToStart()
        {
            Debug.Log($"[DraggableShape] Wrong drop, returning to start: {name} (shapeId={shapeId})");
            transform.SetParent(_startParent, true);
            transform.position = _startPosition;
            OnReturnedToStart?.Invoke();
        }
    }
}