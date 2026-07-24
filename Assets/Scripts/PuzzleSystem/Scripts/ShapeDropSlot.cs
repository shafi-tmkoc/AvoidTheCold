using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Puzzle
{
    /// <summary>
    /// Attach to a silhouette drop target. Accepts a DraggableShape only if its
    /// shapeId matches, then snaps it into place and locks the slot.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ShapeDropSlot : MonoBehaviour, IDropHandler
    {
        [Tooltip("Must match the DraggableShape.ShapeId that belongs here")]
        [SerializeField] private string shapeId;

        private RectTransform _rectTransform;
        private bool _isFilled;

        /// <summary>Raised after this slot accepts and snaps a matching piece.</summary>
        public event Action<ShapeDropSlot> OnFilled;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        /// <summary>Configures a freshly spawned slot: sets its shape id and position.</summary>
        public void Initialize(string id, Vector2 position)
        {
            shapeId = id;
            _isFilled = false;
            _rectTransform.anchoredPosition = position;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_isFilled || eventData.pointerDrag == null) return;

            var shape = eventData.pointerDrag.GetComponent<DraggableShape>();
            if (shape == null || shape.ShapeId != shapeId)
            {
                Debug.Log($"[ShapeDropSlot] Rejected drop on {name} (expected={shapeId})");
                return; // wrong shape: piece bounces back via its own OnEndDrag
            }

            Debug.Log($"[ShapeDropSlot] Accepted drop on {name} (shapeId={shapeId})");
            shape.SnapToSlot(_rectTransform);
            _isFilled = true;
            OnFilled?.Invoke(this);
        }
    }
}
