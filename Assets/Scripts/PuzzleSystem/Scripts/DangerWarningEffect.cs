using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Watches the freeze meter value. When it drops to/below the danger
    /// threshold, fades the freeze bar to red over fadeDuration seconds;
    /// fades back to normal once the value recovers above the threshold.
    /// (Frost overlay is handled separately by FrostOverlayEffect, tied to
    /// elapsed time instead of meter value.)
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

        [Header("Transition")]
        [Tooltip("Seconds to fade between normalColor and dangerColor when the state flips")]
        [SerializeField] private float fadeDuration = 0.4f;

        private bool _isDanger;
        private Coroutine _fadeCoroutine;

        private void OnEnable()
        {
            if (meterValue != null) meterValue.OnValueChanged += HandleValueChanged;
        }

        private void OnDisable()
        {
            if (meterValue != null) meterValue.OnValueChanged -= HandleValueChanged;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        }

        private void HandleValueChanged(float value)
        {
            bool shouldBeDanger = value <= dangerValueThreshold;
            if (shouldBeDanger == _isDanger) return;

            _isDanger = shouldBeDanger;
            Debug.Log($"[DangerWarningEffect] Danger state: {_isDanger} (meterValue={value:0.00}) - starting fade");

            if (freezeBarImage == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeTo(_isDanger ? dangerColor : normalColor));
        }

        /// <summary>Lerps freezeBarImage.color from its current color to target over fadeDuration seconds.</summary>
        private IEnumerator FadeTo(Color target)
        {
            Color start = freezeBarImage.color;

            if (fadeDuration <= 0f)
            {
                freezeBarImage.color = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                freezeBarImage.color = Color.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            freezeBarImage.color = target;
            _fadeCoroutine = null;
        }
    }
}
