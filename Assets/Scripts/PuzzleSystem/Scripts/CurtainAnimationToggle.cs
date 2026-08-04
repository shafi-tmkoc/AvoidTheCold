using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Stops the curtain's Animator once every puzzle slot has been filled
    /// (the curtain settles once the window is patched up), and re-enables
    /// it via ResetForNewAttempt() when a fresh level attempt starts.
    /// </summary>
    public class CurtainAnimationToggle : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private Animator curtainAnimator;

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
            if (curtainAnimator == null) return;
            Debug.Log("[CurtainAnimationToggle] All slots filled - stopping curtain animation");
            curtainAnimator.enabled = false;
        }

        /// <summary>Call when a fresh level attempt starts to resume the curtain animation.</summary>
        public void ResetForNewAttempt()
        {
            if (curtainAnimator == null) return;
            Debug.Log("[CurtainAnimationToggle] New attempt - resuming curtain animation");
            curtainAnimator.enabled = true;
        }
    }
}
