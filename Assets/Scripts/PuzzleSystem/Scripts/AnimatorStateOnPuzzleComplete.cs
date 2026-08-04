using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Switches a target Animator to stateOnComplete once every puzzle slot
    /// has been filled (e.g. a shivering character calms down, curtains stop
    /// swaying), and switches it back to stateOnReset when a fresh level
    /// attempt starts. Uses Animator.Play() directly by state name, so no
    /// Animator parameters/transitions need to be wired up. Reusable for any
    /// object with this same "play state X on win, state Y on reset" need -
    /// attach one instance per object (e.g. Curtain, DayaShivering, TappuShivering).
    /// </summary>
    public class AnimatorStateOnPuzzleComplete : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private Animator targetAnimator;

        [Tooltip("Animator state name to play once every slot is filled (e.g. DayaSittingIdle, CurtainSwayToNormal)")]
        [SerializeField] private string stateOnComplete;
        [Tooltip("Animator state name to play back when a fresh attempt starts (e.g. DayaShivering, CurtainSway)")]
        [SerializeField] private string stateOnReset;

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
            if (targetAnimator == null || string.IsNullOrEmpty(stateOnComplete)) return;
            Debug.Log($"[AnimatorStateOnPuzzleComplete] {name}: all slots filled - playing '{stateOnComplete}'");
            targetAnimator.Play(stateOnComplete);
        }

        /// <summary>Call when a fresh level attempt starts to switch back to stateOnReset.</summary>
        public void ResetForNewAttempt()
        {
            if (targetAnimator == null || string.IsNullOrEmpty(stateOnReset)) return;
            Debug.Log($"[AnimatorStateOnPuzzleComplete] {name}: new attempt - playing '{stateOnReset}'");
            targetAnimator.Play(stateOnReset);
        }
    }
}
