using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Drives the frost overlay off the freeze meter value (which already
    /// combines time draining and pieces placed): once the meter drops
    /// below the ramp threshold, frost climbs toward 1 as the meter keeps
    /// falling, and recedes back toward 0 as placing pieces pushes the
    /// meter back up. Uses CanvasGroupFader so the change is smooth, not an
    /// instant jump. Also fades fully out the moment every puzzle slot is
    /// filled, regardless of whatever the meter was showing at that instant
    /// (the meter itself stops ticking on a win, so without this it would
    /// otherwise stay frozen at its last value).
    /// </summary>
    public class FrostOverlayEffect : MonoBehaviour
    {
        [SerializeField] private FreezeMeterValue meterValue;
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private CanvasGroupFader frostOverlay;

        [Tooltip("Meter value (0-1) at/below which frost starts appearing. 0.5 = starts once the meter has drained past half.")]
        [SerializeField] private float rampStartMeterValue = 0.5f;

        private void OnEnable()
        {
            if (meterValue != null) meterValue.OnValueChanged += HandleValueChanged;
            if (progressTracker != null) progressTracker.OnAllSlotsFilled += HandleAllSlotsFilled;
        }

        private void OnDisable()
        {
            if (meterValue != null) meterValue.OnValueChanged -= HandleValueChanged;
            if (progressTracker != null) progressTracker.OnAllSlotsFilled -= HandleAllSlotsFilled;
        }

        private void HandleValueChanged(float value)
        {
            if (frostOverlay == null) return;

            float target = Mathf.InverseLerp(rampStartMeterValue, 0f, value);
            frostOverlay.SetTarget(target);
        }

        private void HandleAllSlotsFilled()
        {
            if (frostOverlay == null) return;

            Debug.Log($"[FrostOverlayEffect] {name}: all slots filled - fading out");
            frostOverlay.SetTarget(0f);
        }
    }
}
