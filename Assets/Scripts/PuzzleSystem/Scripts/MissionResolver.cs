using System;
using UnityEngine;

namespace Puzzle
{
    /// <summary>
    /// Decides the mission outcome by racing puzzle completion against
    /// either the freeze meter fully depleting or the timer running out
    /// (the authoritative "time's up" signal - placing pieces boosts the
    /// meter, so it may not hit exactly 0 even on a genuine timeout).
    /// Whichever happens first wins; stops the timer and raises the
    /// matching instance event (not static) once.
    /// </summary>
    public class MissionResolver : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private FreezeMeterValue meterValue;
        [SerializeField] private CountdownTimer timer;

        public event Action OnMissionSuccess;
        public event Action OnMissionFailed;

        private bool _isResolved;

        /// <summary>Call before starting a new level attempt so the race can fire again.</summary>
        public void ResetForNewAttempt()
        {
            _isResolved = false;
        }

        private void OnEnable()
        {
            if (progressTracker != null) progressTracker.OnAllSlotsFilled += HandleAllSlotsFilled;
            if (meterValue != null) meterValue.OnDepleted += HandleFailureTrigger;
            if (timer != null) timer.OnExpired += HandleFailureTrigger;
        }

        private void OnDisable()
        {
            if (progressTracker != null) progressTracker.OnAllSlotsFilled -= HandleAllSlotsFilled;
            if (meterValue != null) meterValue.OnDepleted -= HandleFailureTrigger;
            if (timer != null) timer.OnExpired -= HandleFailureTrigger;
        }

        private void HandleAllSlotsFilled()
        {
            if (_isResolved) return;
            _isResolved = true;

            Debug.Log("[MissionResolver] Mission Success - window fixed in time");
            if (timer != null) timer.StopTimer();
            OnMissionSuccess?.Invoke();
        }

        private void HandleFailureTrigger()
        {
            if (_isResolved) return;
            _isResolved = true;

            Debug.Log("[MissionResolver] Mission Failed - froze before window was fixed");
            if (timer != null) timer.StopTimer();
            OnMissionFailed?.Invoke();
        }
    }
}
