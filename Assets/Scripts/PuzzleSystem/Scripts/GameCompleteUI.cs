using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Shows the Game Complete banner when LevelFlow finishes the last level,
    /// and wires its Replay/Home buttons. Single job: this one banner and its
    /// two buttons - level progression logic itself lives in LevelFlow.
    /// </summary>
    public class GameCompleteUI : MonoBehaviour
    {
        [SerializeField] private LevelFlow levelFlow;
        [SerializeField] private GameObject banner;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button homeButton;

        [Tooltip("Seconds to wait after the last level is won before the Game Complete banner fades in")]
        [SerializeField] private float showDelaySeconds = 4f;

        private CanvasGroupFader _fader;
        private CanvasGroup _group;
        private Coroutine _pendingShowRoutine;

        private void Awake()
        {
            if (banner != null)
            {
                _fader = banner.GetComponent<CanvasGroupFader>();
                _group = banner.GetComponent<CanvasGroup>();
            }

            if (replayButton != null) replayButton.onClick.AddListener(HandleReplayClicked);
            if (homeButton != null) homeButton.onClick.AddListener(HandleHomeClicked);
        }

        private void OnEnable()
        {
            if (levelFlow != null) levelFlow.OnGameCompleted += HandleGameCompleted;
        }

        private void OnDisable()
        {
            if (levelFlow != null) levelFlow.OnGameCompleted -= HandleGameCompleted;

            if (_pendingShowRoutine != null)
            {
                StopCoroutine(_pendingShowRoutine);
                _pendingShowRoutine = null;
            }
        }

        private void HandleGameCompleted()
        {
            Debug.Log($"[GameCompleteUI] Game completed - showing banner in {showDelaySeconds}s");

            if (_pendingShowRoutine != null) StopCoroutine(_pendingShowRoutine);
            _pendingShowRoutine = StartCoroutine(ShowBannerAfterDelay());
        }

        private IEnumerator ShowBannerAfterDelay()
        {
            yield return new WaitForSeconds(showDelaySeconds);

#if PLAYSCHOOL_MAIN
                    EffectParticleControll.Instance.SpawnGameEndPanel();
                    GameOverEndPanel.Instance.AddTheListnerRetryGame(() => HandleReplayClicked());
#else
            //Your testing End panel
            if (banner != null)
            {
                banner.SetActive(true);
                if (_group != null)
                {
                    _group.alpha = 0f;
                    _group.blocksRaycasts = true;
                }
                if (_fader != null) _fader.SetTarget(1f);
            }
#endif
            _pendingShowRoutine = null;
        }

        private void HandleReplayClicked()
        {
            Debug.Log("[GameCompleteUI] Replay clicked");

            if (_group != null) _group.blocksRaycasts = false;
            if (_fader != null) _fader.SetTarget(0f);

            if (levelFlow != null) levelFlow.ReplayFromStart();
        }

        private void HandleHomeClicked()
        {
            Debug.Log("[GameCompleteUI] Home clicked - quitting");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            //Application.Quit();
#endif
        }
    }
}
