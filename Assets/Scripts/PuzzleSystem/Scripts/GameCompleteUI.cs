using UnityEngine;
using UnityEngine.UI;

namespace Puzzle
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

        private CanvasGroupFader _fader;
        private CanvasGroup _group;

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
        }

        private void HandleGameCompleted()
        {
            Debug.Log("[GameCompleteUI] Showing Game Complete banner");

            if (banner == null) return;

            banner.SetActive(true);
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = true;
            }
            if (_fader != null) _fader.SetTarget(1f);
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
            Application.Quit();
#endif
        }
    }
}
