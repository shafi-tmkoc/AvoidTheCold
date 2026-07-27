using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Watches the freeze meter value. When it drops to/below the danger
    /// threshold, tints the freeze bar red; reverts once the value recovers
    /// above the threshold. (Frost overlay is handled separately by
    /// FrostOverlayEffect, tied to elapsed time instead of meter value.)
    /// </summary>
    public class DangerWarningEffect : MonoBehaviour
    {
        [SerializeField] private FreezeMeterValue meterValue;
        [SerializeField] private Image freezeBarImage;

        [Header("Danger Threshold")]
        [Tooltip("Danger triggers when the meter value drops to/below this fraction")]
        [SerializeField] private float dangerValueThreshold = 0.3f;

        [Header("Bar Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        private bool _isDanger;

        private void OnEnable()
        {
            if (meterValue != null) meterValue.OnValueChanged += HandleValueChanged;
        }

        private void OnDisable()
        {
            if (meterValue != null) meterValue.OnValueChanged -= HandleValueChanged;
        }

        private void HandleValueChanged(float value)
        {
            bool shouldBeDanger = value <= dangerValueThreshold;
            if (shouldBeDanger == _isDanger) return;

            _isDanger = shouldBeDanger;
            Debug.Log($"[DangerWarningEffect] Danger state: {_isDanger} (meterValue={value:0.00})");

            if (freezeBarImage != null) freezeBarImage.color = _isDanger ? dangerColor : normalColor;
        }
    }
}
