using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Disables this GameObject (the stormy wind-lines effect) once every
    /// puzzle slot has been filled - the wind shouldn't keep blowing once
    /// the window is fully patched up.
    /// </summary>
    public class WindlinesStormyHider : MonoBehaviour
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
            Debug.Log($"[WindlinesStormyHider] All slots filled - disabling {name}");
            gameObject.SetActive(false);
        }

        /// <summary>Call when a fresh level attempt starts (next level or restart) to re-enable the wind lines effect.</summary>
        public void ResetForNewAttempt()
        {
            Debug.Log($"[WindlinesStormyHider] New attempt - re-enabling {name}");
            gameObject.SetActive(true);
        }
    }
}
