using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AvoidTheCold
{
    /// <summary>
    /// Attach to a draggable puzzle piece (UI Image). Handles pointer/touch drag,
    /// snapping into a matching ShapeDropSlot, and bouncing back to its start
    /// position when dropped anywhere else.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class DraggableShape : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Must match the ShapeDropSlot this piece belongs to")]
        [SerializeField] private string shapeId;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private Transform _startParent;
        private Vector2 _startAnchoredPos;
        private bool _isPlaced;

        public string ShapeId => shapeId;

        /// <summary>Raised whenever this piece bounces back to its start (wrong slot or empty space).</summary>
        public event Action OnReturnedToStart;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            CacheStartTransform();
        }

        private void CacheStartTransform()
        {
            _startParent = transform.parent;
            _startAnchoredPos = _rectTransform.anchoredPosition;
        }

        /// <summary>
        /// Configures a freshly spawned piece: sets its shape id and tray
        /// position, then re-caches that position as its "start" (Awake
        /// already ran at Instantiate time, before this position was set).
        /// </summary>
        public void Initialize(string id, Vector2 trayPosition)
        {
            shapeId = id;
            _isPlaced = false;
            _canvasGroup.blocksRaycasts = true;
            _rectTransform.anchoredPosition = trayPosition;
            CacheStartTransform();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isPlaced) return;

            Debug.Log($"[DraggableShape] Begin drag: {name} (shapeId={shapeId})");
            _canvasGroup.blocksRaycasts = false; // let the raycast pass through to the slot underneath
            if (_rootCanvas != null)
                transform.SetParent(_rootCanvas.transform, true); // render above everything else while dragging
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isPlaced || _rootCanvas == null) return;

            _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isPlaced) return;

            _canvasGroup.blocksRaycasts = true;
            ReturnToStart(); // if a ShapeDropSlot accepted this piece, SnapToSlot already set _isPlaced = true
        }

        /// <summary>Called by a matching ShapeDropSlot on a correct drop.</summary>
        public void SnapToSlot(RectTransform slot)
        {
            Debug.Log($"[DraggableShape] Snapped: {name} (shapeId={shapeId}) -> slot {slot.name}");
            _isPlaced = true;
            transform.SetParent(slot, false);
            _rectTransform.anchoredPosition = Vector2.zero;
            _canvasGroup.blocksRaycasts = false; // placed pieces no longer need to be draggable
        }

        /// <summary>Bounces the piece back to where it started (wrong slot or dropped in empty space).</summary>
        public void ReturnToStart()
        {
            Debug.Log($"[DraggableShape] Wrong drop, returning to start: {name} (shapeId={shapeId})");
            transform.SetParent(_startParent, false);
            _rectTransform.anchoredPosition = _startAnchoredPos;
            OnReturnedToStart?.Invoke();
        }
    }
}
