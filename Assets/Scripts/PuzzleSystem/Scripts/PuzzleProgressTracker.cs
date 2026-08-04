using System;
using UnityEngine;
using UnityEngine.Events;

namespace AvoidTheCold
{
    /// <summary>
    /// Tracks how many of the assigned ShapeDropSlots have been filled and
    /// reports normalized progress and completion. Does not know about time.
    /// </summary>
    public class PuzzleProgressTracker : MonoBehaviour
    {
        [SerializeField] private ShapeDropSlot[] slots;

        [Tooltip("Fires once every slot has been filled - wire Inspector-only responses here (no code needed). Same moment as OnAllSlotsFilled below.")]
        [SerializeField] private UnityEvent onAllShapesPlaced;

        public event Action<float> OnProgressChanged; // 0..1
        public event Action OnAllSlotsFilled;

        private int _filledCount;

        public int FilledCount => _filledCount;
        public int TotalCount => slots != null ? slots.Length : 0;

        private void Awake()
        {
            SubscribeAll();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        /// <summary>
        /// Replaces the tracked slots with a freshly spawned set (e.g. from
        /// LevelLoader) and resets the filled count for a new attempt.
        /// </summary>
        public void SetSlots(ShapeDropSlot[] newSlots)
        {
            UnsubscribeAll();
            slots = newSlots;
            _filledCount = 0;
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            if (slots == null) return;

            foreach (var slot in slots)
            {
                if (slot != null) slot.OnFilled += HandleSlotFilled;
            }
        }

        private void UnsubscribeAll()
        {
            if (slots == null) return;

            foreach (var slot in slots)
            {
                if (slot != null) slot.OnFilled -= HandleSlotFilled;
            }
        }

        private void HandleSlotFilled(ShapeDropSlot slot)
        {
            _filledCount++;
            float progress = TotalCount > 0 ? (float)_filledCount / TotalCount : 0f;

            Debug.Log($"[PuzzleProgressTracker] Progress: {_filledCount}/{TotalCount}");
            OnProgressChanged?.Invoke(progress);

            if (_filledCount >= TotalCount)
            {
                Debug.Log("[PuzzleProgressTracker] All slots filled");
                OnAllSlotsFilled?.Invoke();
                onAllShapesPlaced?.Invoke();
            }
        }
    }
}
