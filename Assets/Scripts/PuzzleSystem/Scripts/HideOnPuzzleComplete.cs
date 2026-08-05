using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Disables this GameObject once every puzzle slot has been filled, and
    /// re-enables it via ResetForNewAttempt() when a fresh level attempt
    /// starts. Generic and reusable - attach to any object that should
    /// disappear on a win and come back for the next attempt (e.g. the
    /// stormy wind lines, the piece tray).
    /// </summary>
    public class HideOnPuzzleComplete : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;

        private void OnEnable()
        {
            if (progressTracker != null) progressTracker.OnAllSlotsFilled += HandleAllSlotsFilled;
        }

        private void OnDisable()
        {
            if (progressTracker != null) progressTracker.OnAllSlotsFilled -= HandleAllSlotsFilled;
        }

        private void HandleAllSlotsFilled()
        {
            Debug.Log($"[HideOnPuzzleComplete] All slots filled - disabling {name}");
            gameObject.SetActive(false);
        }

        /// <summary>Call when a fresh level attempt starts (next level or restart) to re-enable this GameObject.</summary>
        public void ResetForNewAttempt()
        {
            Debug.Log($"[HideOnPuzzleComplete] New attempt - re-enabling {name}");
            gameObject.SetActive(true);
        }
    }
}
