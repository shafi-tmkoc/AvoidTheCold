using System;
using System.Collections;
using UnityEngine;

namespace Puzzle
{
    /// <summary>
    /// Full-screen intro panel shown for a fixed duration every time the
    /// game opens (not per level) - LevelFlow calls Show() once at Start,
    /// before deciding whether to play the storyboard or jump to gameplay.
    /// </summary>
    public class IntroPanel : MonoBehaviour
    {
        [SerializeField] private float displaySeconds = 5f;
        [SerializeField] private VoiceOverPlayer voicePlayer;
        [SerializeField] private string voiceoverTitle = VoiceOverTitles.Intro;

        public event Action OnIntroFinished;

        public void Show()
        {
            Debug.Log("[IntroPanel] Showing intro");
            gameObject.SetActive(true);
            if (voicePlayer != null) voicePlayer.Play(voiceoverTitle);
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displaySeconds);

            Debug.Log("[IntroPanel] Intro finished");
            gameObject.SetActive(false);
            OnIntroFinished?.Invoke();
        }
    }
}
