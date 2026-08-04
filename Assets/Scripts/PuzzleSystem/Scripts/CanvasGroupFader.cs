using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Smoothly fades a CanvasGroup's alpha toward a target value over time
    /// instead of snapping instantly. Reusable for any fade-in/fade-out UI.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFader : MonoBehaviour
    {
        [Tooltip("Alpha change per second")]
        [SerializeField] private float fadeSpeed = 1f;

        private CanvasGroup _canvasGroup;
        [SerializeField] private float _targetAlpha = 1;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _targetAlpha = _canvasGroup.alpha;
        }

        /// <summary>Sets the alpha this fader should smoothly move toward.</summary>
        public void SetTarget(float targetAlpha)
        {
            _targetAlpha = Mathf.Clamp01(targetAlpha);
        }

        private void Update()
        {
            if (Mathf.Approximately(_canvasGroup.alpha, _targetAlpha)) return;

            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
}
