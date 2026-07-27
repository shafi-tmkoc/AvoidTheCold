using System.Collections;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Shows the matching win/lose banner when the mission resolves (after a
    /// short delay), fading it in via CanvasGroupFader. Fades it back out (no
    /// abrupt SetActive) when a new level starts loading, so it crossfades
    /// with the next level's puzzle appearing underneath.
    /// </summary>
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private GameObject winBanner;
        [SerializeField] private GameObject loseBanner;

        [Tooltip("Seconds to wait after the mission resolves before the banner fades in")]
        [SerializeField] private float showDelaySeconds = 2f;

        private CanvasGroupFader _winFader;
        private CanvasGroupFader _loseFader;
        private CanvasGroup _winGroup;
        private CanvasGroup _loseGroup;
        private Coroutine _pendingShowRoutine;

        private void Awake()
        {
            if (winBanner != null)
            {
                _winFader = winBanner.GetComponent<CanvasGroupFader>();
                _winGroup = winBanner.GetComponent<CanvasGroup>();
            }

            if (loseBanner != null)
            {
                _loseFader = loseBanner.GetComponent<CanvasGroupFader>();
                _loseGroup = loseBanner.GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            if (missionResolver != null)
            {
                missionResolver.OnMissionSuccess += HandleSuccess;
                missionResolver.OnMissionFailed += HandleFailed;
            }
        }

        private void OnDisable()
        {
            if (missionResolver != null)
            {
                missionResolver.OnMissionSuccess -= HandleSuccess;
                missionResolver.OnMissionFailed -= HandleFailed;
            }
        }

        /// <summary>Fades both banners out and cancels any pending delayed show - call before starting a new level attempt.</summary>
        public void HideAll()
        {
            CancelPendingShow();
            FadeOut(_loseFader, _loseGroup);
            FadeOut(_winFader, _winGroup);
        }

        private void HandleSuccess()
        {
            Debug.Log($"[ResultScreenUI] Win - showing banner in {showDelaySeconds}s");
            FadeOut(_loseFader, _loseGroup);
            ShowAfterDelay(winBanner, _winFader, _winGroup);
        }

        private void HandleFailed()
        {
            Debug.Log($"[ResultScreenUI] Lose - showing banner in {showDelaySeconds}s");
            FadeOut(_winFader, _winGroup);
            ShowAfterDelay(loseBanner, _loseFader, _loseGroup);
        }

        private void ShowAfterDelay(GameObject banner, CanvasGroupFader fader, CanvasGroup group)
        {
            CancelPendingShow();
            _pendingShowRoutine = StartCoroutine(ShowAfterDelayRoutine(banner, fader, group));
        }

        private IEnumerator ShowAfterDelayRoutine(GameObject banner, CanvasGroupFader fader, CanvasGroup group)
        {
            yield return new WaitForSeconds(showDelaySeconds);
            _pendingShowRoutine = null;
            FadeIn(banner, fader, group);
        }

        private void CancelPendingShow()
        {
            if (_pendingShowRoutine == null) return;

            StopCoroutine(_pendingShowRoutine);
            _pendingShowRoutine = null;
        }

        private static void FadeIn(GameObject banner, CanvasGroupFader fader, CanvasGroup group)
        {
            if (banner == null) return;

            banner.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = true;
            }
            if (fader != null) fader.SetTarget(1f);
        }

        private static void FadeOut(CanvasGroupFader fader, CanvasGroup group)
        {
            if (group != null) group.blocksRaycasts = false; // don't block the next level's pieces while fading
            if (fader != null) fader.SetTarget(0f);
        }
    }
}
