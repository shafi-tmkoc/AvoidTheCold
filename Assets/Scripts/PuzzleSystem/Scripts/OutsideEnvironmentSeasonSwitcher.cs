using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AvoidTheCold
{
    /// <summary>
    /// Once every puzzle slot is filled, crossfades the window's outside
    /// view from the wintery scene to the normal scene: fades winterEnvironment
    /// out and normalEnvironment in together, then disables winterEnvironment
    /// so it stops rendering/updating underneath.
    /// </summary>
    public class OutsideEnvironmentSeasonSwitcher : MonoBehaviour
    {
        [SerializeField] private PuzzleProgressTracker progressTracker;
        [SerializeField] private SpriteRenderer winterEnvironment;
        [SerializeField] private SpriteRenderer normalEnvironment;

        [SerializeField] UnityEvent OnRevealComplete = new UnityEvent();

        [Tooltip("Seconds to crossfade from winter to normal")]
        [SerializeField] private float fadeDuration = 0.6f;

        private Coroutine _fadeCoroutine;

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
            Debug.Log("[OutsideEnvironmentSeasonSwitcher] All slots filled - crossfading winter -> normal");
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeToNormal());
        }

        /// <summary>
        /// Call when a fresh level attempt starts to undo the crossfade:
        /// stops any in-flight fade, re-activates winterEnvironment at full
        /// alpha, and deactivates normalEnvironment - so the reveal can play
        /// again next time this level's slots are all filled.
        /// </summary>
        public void ResetForNewAttempt()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (winterEnvironment != null)
            {
                winterEnvironment.gameObject.SetActive(true);
                SetAlpha(winterEnvironment, 1f);
            }

            if (normalEnvironment != null)
            {
                SetAlpha(normalEnvironment, 0f);
                normalEnvironment.gameObject.SetActive(false);
            }

            Debug.Log("[OutsideEnvironmentSeasonSwitcher] Reset for new attempt - winter restored");
        }

        private IEnumerator CrossfadeToNormal()
        {
            if (normalEnvironment == null || winterEnvironment == null)
            {
                Debug.Log("[OutsideEnvironmentSeasonSwitcher] Missing winter/normal reference - skipping crossfade");
                yield break;
            }

            normalEnvironment.gameObject.SetActive(true);
            SetAlpha(normalEnvironment, 0f);

            float startWinterAlpha = winterEnvironment.color.a;

            if (fadeDuration <= 0f)
            {
                SetAlpha(winterEnvironment, 0f);
                SetAlpha(normalEnvironment, 1f);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    SetAlpha(winterEnvironment, Mathf.Lerp(startWinterAlpha, 0f, t));
                    SetAlpha(normalEnvironment, Mathf.Lerp(0f, 1f, t));
                    yield return null;
                }

                SetAlpha(winterEnvironment, 0f);
                SetAlpha(normalEnvironment, 1f);
            }

            winterEnvironment.gameObject.SetActive(false);
            Debug.Log("[OutsideEnvironmentSeasonSwitcher] Crossfade complete - winter disabled");

            // Invoke the event when reveal is complete
            OnRevealComplete?.Invoke();
        }

        private static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}