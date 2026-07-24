using UnityEngine;

namespace Puzzle
{
    /// <summary>
    /// Reads/writes level progress to PlayerPrefs: which level the player is
    /// on, and whether the intro story has already been shown. Plain static
    /// data access - no behavior, no events.
    /// </summary>
    public static class LevelProgressStore
    {
        private const string LevelKey = "Puzzle_CurrentLevel";
        private const string StoryShownKey = "Puzzle_IntroStoryShown";
        private const string HandTutorialShownKey = "Puzzle_HandTutorialShown";

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(LevelKey, 1);
            set => PlayerPrefs.SetInt(LevelKey, value);
        }

        public static bool HasSeenIntroStory
        {
            get => PlayerPrefs.GetInt(StoryShownKey, 0) == 1;
            set => PlayerPrefs.SetInt(StoryShownKey, value ? 1 : 0);
        }

        /// <summary>Whether the level-1 hand drag-and-drop hint has already been shown once, ever.</summary>
        public static bool HasSeenHandTutorial
        {
            get => PlayerPrefs.GetInt(HandTutorialShownKey, 0) == 1;
            set => PlayerPrefs.SetInt(HandTutorialShownKey, value ? 1 : 0);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
