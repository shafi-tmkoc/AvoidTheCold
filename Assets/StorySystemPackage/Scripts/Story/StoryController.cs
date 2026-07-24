using System;
using System.Collections;
using UnityEngine;
using StorySystem.Data;
namespace StorySystem.Story
{
    public class StoryController : MonoBehaviour
    {
        [Header("Story Data")]
        [SerializeField] private StoryData storyData;

        [Header("Sub-components (auto-found if empty)")]
        [SerializeField] private StoryUI       storyUI;
        [SerializeField] private StoryAnimator storyAnimator;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource narrationSource;

        // Set by whoever activates this prefab (e.g. LevelManager)
        public Action OnStoryFinished;

        /// <summary>Raised with the slide index each time a new slide is shown (e.g. for VO sync).</summary>
        public event Action<int> OnSlideShown;

        private int  _slideIndex;
        private bool _isSkipped;

        private void Awake()
        {
            if (storyUI       == null) storyUI       = GetComponentInChildren<StoryUI>(true);
            if (storyAnimator == null) storyAnimator = GetComponentInChildren<StoryAnimator>(true);
        }

        private void OnEnable()
        {
            _slideIndex = 0;
            _isSkipped  = false;
            PlayBGM();
            StartCoroutine(PlayRoutine());           
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
            if (narrationSource != null && narrationSource.isPlaying) narrationSource.Stop();
        }

        public void SkipStory()
        {
            if (_isSkipped) return;
            _isSkipped = true;
            StopAllCoroutines();
            if (narrationSource != null && narrationSource.isPlaying) narrationSource.Stop();
            Finish();
        }

        private IEnumerator PlayRoutine()
        {
            if (storyData == null || storyData.slides == null || storyData.slides.Length == 0)
            {
                Finish(); yield break;
            }

            while (_slideIndex < storyData.slides.Length && !_isSkipped)
            {
                var slide = storyData.slides[_slideIndex];

                if (slide.slideEnterSFX != null && narrationSource != null)
                    narrationSource.PlayOneShot(slide.slideEnterSFX);

                storyUI?.ShowSlide(slide);
                storyAnimator?.AnimateSlideIn(slide.transitionIn);
                OnSlideShown?.Invoke(_slideIndex);

                if (slide.narrationClip != null && narrationSource != null)
                {
                    narrationSource.clip = slide.narrationClip;
                    narrationSource.Play();
                }

                float dur = slide.displayDuration > 0f ? slide.displayDuration : 3f;
                yield return new WaitForSeconds(dur);

                if (_isSkipped) break;

                storyAnimator?.AnimateSlideOut(slide.transitionOut);
                yield return new WaitForSeconds(0.35f);
                _slideIndex++;
            }

            Finish();
        }

        private void Finish()
        {
            if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
            OnStoryFinished?.Invoke();
        }

        private void PlayBGM()
        {
            if (bgmSource == null || storyData?.storyBGM == null) return;
            bgmSource.clip   = storyData.storyBGM;
            bgmSource.volume = storyData.bgmVolume;
            bgmSource.loop   = true;
            bgmSource.Play();
        }
    }
}
