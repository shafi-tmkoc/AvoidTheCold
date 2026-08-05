using System;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Attach to a world-space silhouette drop target (SpriteRenderer under
    /// Board2D). Accepts a DraggableShape only if its shapeId matches, then
    /// snaps it into place and locks the slot. Called directly by
    /// DraggableShape.EndDrag via TryAccept - no Unity UI event system
    /// involved.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShapeDropSlot : MonoBehaviour
    {
        [Tooltip("Must match the DraggableShape.ShapeId that belongs here")]
        [SerializeField] private string shapeId;

        private bool _isFilled;

        /// <summary>Raised after this slot accepts and snaps a matching piece.</summary>
        public event Action<ShapeDropSlot> OnFilled;

        /// <summary>Configures a freshly spawned slot: sets its shape id and world position.</summary>
        public void Initialize(string id, Vector3 position)
        {
            shapeId = id;
            _isFilled = false;
            transform.position = position;
        }

        /// <summary>
        /// Attempts to accept a dropped piece. Returns true (and snaps the
        /// piece into place) only if the slot is empty and the shapeId
        /// matches; otherwise returns false and the piece bounces back.
        /// </summary>
        public bool TryAccept(DraggableShape shape)
        {
            if (_isFilled || shape == null || shape.ShapeId != shapeId)
            {
                Debug.Log($"[ShapeDropSlot] Rejected drop on {name} (expected={shapeId})");
                return false;
            }

            Debug.Log($"[ShapeDropSlot] Accepted drop on {name} (shapeId={shapeId})");
            shape.SnapToSlot(transform);
            shape.outlineRenderer.enabled = false;
            _isFilled = true;
            OnFilled?.Invoke(this);
            return true;
        }
    }
}
