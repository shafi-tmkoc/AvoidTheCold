using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Maps in-game situations (piece progress, danger threshold, mission
    /// outcome, wrong drops) to the matching voiceover_title from the VO
    /// sheet and plays them through VoiceOverPlayer. Pure listener - doesn't
    /// own or change any gameplay state, just reacts to it.
    /// </summary>
    public class GameplayVoiceOverTrigger : MonoBehaviour
    {
        [SerializeField] private VoiceOverPlayer voicePlayer;
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private FreezeMeterValue freezeMeterValue;
        [SerializeField] private MissionResolver missionResolver;

        [Tooltip("Meter value at/below which TimerWarning1 plays (once per attempt)")]
        [SerializeField, Range(0f, 1f)] private float dangerThreshold = 0.3f;

        private DraggableShape[] _trackedPieces = System.Array.Empty<DraggableShape>();
        private bool _dangerWarningPlayed;
        private int _lastFilledCount;

        private void OnEnable()
        {
            if (progressTracker != null) progressTracker.OnProgressChanged += HandleProgress;
            if (freezeMeterValue != null) freezeMeterValue.OnValueChanged += HandleMeterValue;
            if (missionResolver != null)
            {
                missionResolver.OnMissionSuccess += HandleSuccess;
                missionResolver.OnMissionFailed += HandleFailed;
            }
        }

        private void OnDisable()
        {
            UnsubscribePieces();

            if (progressTracker != null) progressTracker.OnProgressChanged -= HandleProgress;
            if (freezeMeterValue != null) freezeMeterValue.OnValueChanged -= HandleMeterValue;
            if (missionResolver != null)
            {
                missionResolver.OnMissionSuccess -= HandleSuccess;
                missionResolver.OnMissionFailed -= HandleFailed;
            }
        }

        /// <summary>
        /// Call after spawning a fresh set of pieces for a new level attempt,
        /// so wrong-drop VO tracks the current pieces and the per-attempt
        /// guards (danger warning, progress) reset.
        /// </summary>
        public void ResetForNewAttempt(DraggableShape[] pieces)
        {
            UnsubscribePieces();

            _trackedPieces = pieces ?? System.Array.Empty<DraggableShape>();
            foreach (var piece in _trackedPieces)
            {
                if (piece != null) piece.OnReturnedToStart += HandleWrongPlacement;
            }

            _dangerWarningPlayed = false;
            _lastFilledCount = 0;
        }

        private void UnsubscribePieces()
        {
            foreach (var piece in _trackedPieces)
            {
                if (piece != null) piece.OnReturnedToStart -= HandleWrongPlacement;
            }
        }

        private void HandleProgress(float normalized01)
        {
            if (progressTracker == null || voicePlayer == null || progressTracker.TotalCount <= 0) return;

            int filled = Mathf.RoundToInt(normalized01 * progressTracker.TotalCount);
            if (filled <= _lastFilledCount) return; // only react to forward progress
            _lastFilledCount = filled;

            bool isLast = filled >= progressTracker.TotalCount;
            bool isFirst = filled == Random.Range(0, progressTracker.TotalCount);

            Debug.Log("Filled Value: " + filled);
            if (isLast) voicePlayer.Play(VoiceOverTitles.Encouragement2);
            else if (isFirst) voicePlayer.Play(VoiceOverTitles.Encouragement1);
        }

        private void HandleMeterValue(float value)
        {
            if (voicePlayer == null || _dangerWarningPlayed) return;
            if (value > dangerThreshold) return;

            _dangerWarningPlayed = true;
            voicePlayer.Play(VoiceOverTitles.TimerWarning1);
        }

        private void HandleWrongPlacement()
        {
            if (voicePlayer != null) voicePlayer.Play(VoiceOverTitles.WrongPlacement1);
        }

        private void HandleSuccess()
        {
            if (voicePlayer != null) voicePlayer.Play(VoiceOverTitles.Success1);
        }

        private void HandleFailed()
        {
            if (voicePlayer != null) voicePlayer.Play(VoiceOverTitles.Fail3);
        }
    }
}
