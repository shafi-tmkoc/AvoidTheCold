using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Displays the star rating on the WinBanner after a level is completed.
    /// Call ShowStars(count) right after HelperGameCategoryDataSaver.LevelCompleted().
    /// </summary>
    public class StarDisplayUI : MonoBehaviour
    {
        [Tooltip("5 star images in order. Sprite swaps between filled/empty.")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;

        public void ShowStars(int count)
        {
            count = Mathf.Clamp(count, 0, starImages.Length);
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].gameObject.SetActive(true);
                starImages[i].sprite = i < count ? starFilled : starEmpty;
            }
            Debug.Log($"[StarDisplayUI] Showing {count} / {starImages.Length} stars");
        }

        public void HideStars()
        {
            foreach (var s in starImages)
                if (s != null) s.gameObject.SetActive(false);
        }
    }
}
