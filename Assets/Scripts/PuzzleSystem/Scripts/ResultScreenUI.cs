using System;
using System.Collections;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Shows the matching win/lose banner when the mission resolves (after a
    /// short delay), holds it fully visible for a while, then fades it back
    /// out and reports that the cycle is complete - so whoever loads the
    /// next level (LevelFlow) knows when it's safe to do so, instead of
    /// running its own separate timer.
    /// </summary>
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private GameObject winBanner;
        [SerializeField] private GameObject loseBanner;

        [Tooltip("Seconds to wait after the mission resolves before the banner fades in")]
        [SerializeField] private float showDelaySeconds = 2f;

        [Tooltip("Seconds the banner stays fully visible before fading out")]
        [SerializeField] private float holdSeconds = 5f;

        /// <summary>Raised once the banner has finished its show/hold/fade-out cycle.</summary>
        public event Action OnResultCycleComplete;

        private CanvasGroupFader _winFader;
        private CanvasGroupFader _loseFader;
        private CanvasGroup _winGroup;
        private CanvasGroup _loseGroup;
        private Coroutine _pendingCycleRoutine;

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

        /// <summary>Fades both banners out and cancels any pending cycle - call before starting a new level attempt.</summary>
        public void HideAll()
        {
            CancelPendingCycle();
            FadeOut(_loseFader, _loseGroup);
            FadeOut(_winFader, _winGroup);
        }

        private void HandleSuccess()
        {
            Debug.Log($"[ResultScreenUI] Win - showing in {showDelaySeconds}s, holding {holdSeconds}s");
            FadeOut(_loseFader, _loseGroup);
            RunResultCycle(winBanner, _winFader, _winGroup);
        }

        private void HandleFailed()
        {
            Debug.Log($"[ResultScreenUI] Lose - showing in {showDelaySeconds}s, holding {holdSeconds}s");
            FadeOut(_winFader, _winGroup);
            RunResultCycle(loseBanner, _loseFader, _loseGroup);
        }

        private void RunResultCycle(GameObject banner, CanvasGroupFader fader, CanvasGroup group)
        {
            CancelPendingCycle();
            _pendingCycleRoutine = StartCoroutine(ResultCycleRoutine(banner, fader, group));
        }

        private IEnumerator ResultCycleRoutine(GameObject banner, CanvasGroupFader fader, CanvasGroup group)
        {
            yield return new WaitForSeconds(showDelaySeconds);
            FadeIn(banner, fader, group);

            yield return new WaitForSeconds(holdSeconds);
            FadeOut(fader, group);

            _pendingCycleRoutine = null;
            Debug.Log("[ResultScreenUI] Result cycle complete");
            OnResultCycleComplete?.Invoke();
        }

        private void CancelPendingCycle()
        {
            if (_pendingCycleRoutine == null) return;

            StopCoroutine(_pendingCycleRoutine);
            _pendingCycleRoutine = null;
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
