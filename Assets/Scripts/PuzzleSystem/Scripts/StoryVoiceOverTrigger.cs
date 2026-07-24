using UnityEngine;
using StorySystem.Story;

namespace Puzzle
{
    /// <summary>
    /// Plays the matching Storyboard{N} voiceover each time StoryController
    /// shows a new slide. Pure listener - the story system itself is
    /// untouched beyond the one OnSlideShown event it raises.
    /// </summary>
    public class StoryVoiceOverTrigger : MonoBehaviour
    {
        [SerializeField] private StoryController storyController;
        [SerializeField] private VoiceOverPlayer voicePlayer;

        private void OnEnable()
        {
            if (storyController != null) storyController.OnSlideShown += HandleSlideShown;
        }

        private void OnDisable()
        {
            if (storyController != null) storyController.OnSlideShown -= HandleSlideShown;
        }

        private void HandleSlideShown(int slideIndex)
        {
            if (voicePlayer == null) return;

            string title = "Storyboard" + (slideIndex + 1);
            Debug.Log($"[StoryVoiceOverTrigger] Slide {slideIndex} -> {title}");
            voicePlayer.Play(title);
        }
    }
}
