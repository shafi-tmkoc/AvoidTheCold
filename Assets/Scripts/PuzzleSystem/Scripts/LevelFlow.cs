using System;
using System.Collections;
using StorySystem.Story;
using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Decides which level to load and when: shows the intro story only on
    /// the very first run, then loads the current saved level. On mission
    /// success, advances to the next level (looping back to the first after
    /// the last); on failure, reloads the same level. No level-select -
    /// straight, continuous progression.
    /// </summary>
    public class LevelFlow : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelData[] levels;
        [SerializeField] private MissionResolver missionResolver;
        [SerializeField] private StoryController introStory;
        [SerializeField] private IntroPanel introPanel;
        [SerializeField] private VoiceOverPlayer voicePlayer;

        [Tooltip("Seconds the win/lose banner stays up before auto-continuing")]
        [SerializeField] private float resultDisplaySeconds = 4f;

        [Tooltip("Delay between Tutorial1 and Tutorial2 VO lines, played once on the very first gameplay start")]
        [SerializeField] private float tutorialVoiceOverGap = 3f;

        /// <summary>Raised when the last level is won, instead of auto-looping to level 1.</summary>
        public event Action OnGameCompleted;

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

        private void Start()
        {
            if (introPanel != null)
            {
                introPanel.OnIntroFinished += HandleIntroFinished;
                introPanel.Show();
            }
            else
            {
                ProceedPastIntro();
            }
        }

        private void HandleIntroFinished()
        {
            introPanel.OnIntroFinished -= HandleIntroFinished;
            ProceedPastIntro();
        }

        private void ProceedPastIntro()
        {
            if (!LevelProgressStore.HasSeenIntroStory && introStory != null)
            {
                Debug.Log("[LevelFlow] First run - showing intro story");
                LevelProgressStore.HasSeenIntroStory = true;
                LevelProgressStore.Save();

                AudioManager.Instance.StopBG();
                introStory.OnStoryFinished += HandleStoryFinished;
                introStory.gameObject.SetActive(true);
            }
            else
            {
                LoadCurrentLevel();
            }
        }

        private void HandleStoryFinished()
        {
            introStory.OnStoryFinished -= HandleStoryFinished;
            introStory.gameObject.SetActive(false);
            Debug.Log("[LevelFlow] Intro story finished");
            AudioManager.Instance.PlayBG();
            PlayTutorialVoiceOver();
            LoadCurrentLevel();
        }

        /// <summary>Plays Tutorial1 then Tutorial2 once, the first time gameplay is ever reached.</summary>
        private void PlayTutorialVoiceOver()
        {
            if (voicePlayer == null) return;

            voicePlayer.Play(VoiceOverTitles.Tutorial1);
            StartCoroutine(PlayTutorial2AfterDelay());
        }

        private IEnumerator PlayTutorial2AfterDelay()
        {
            yield return new WaitForSeconds(tutorialVoiceOverGap);
            voicePlayer.Play(VoiceOverTitles.Tutorial2);
        }

        private void LoadCurrentLevel()
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.Log("[LevelFlow] No levels assigned - nothing to load");
                return;
            }

            int index = Mathf.Clamp(LevelProgressStore.CurrentLevel - 1, 0, levels.Length - 1);
            Debug.Log($"[LevelFlow] Loading level {index + 1}");
            levelLoader.LoadLevel(levels[index]);
        }

        private void HandleSuccess()
        {
            int next = LevelProgressStore.CurrentLevel + 1;
            bool wasLastLevel = levels != null && next > levels.Length;
            AudioManager.Instance.Win();
            if (wasLastLevel)
            {
                LevelProgressStore.CurrentLevel = 1;
                LevelProgressStore.Save();
                Debug.Log("[LevelFlow] Final level complete - showing Game Complete screen");
                OnGameCompleted?.Invoke();
                return;
            }

            LevelProgressStore.CurrentLevel = next;
            LevelProgressStore.Save();
            Debug.Log($"[LevelFlow] Win - advancing to level {next}");
            StartCoroutine(ContinueAfterDelay());
        }

        /// <summary>Called by the Game Complete screen's Replay button - starts a fresh run from level 1.</summary>
        public void ReplayFromStart()
        {
            Debug.Log("[LevelFlow] Replay - starting from level 1");
            LoadCurrentLevel();
        }

        private void HandleFailed()
        {
            Debug.Log("[LevelFlow] Lose - retrying same level");
            StartCoroutine(ContinueAfterDelay());
        }

        private IEnumerator ContinueAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);
            LoadCurrentLevel();
        }
    }
}
