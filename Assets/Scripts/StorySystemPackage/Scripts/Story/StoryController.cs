using System;
using System.Collections;
using UnityEngine;


namespace tmkoc.butterfly
{
    public class StoryController : MonoBehaviour
    {
        [Header("Story Data")]
        [SerializeField] private StoryData storyData;

        [Header("Sub-components (auto-found if empty)")]
        [SerializeField] private StoryUI       storyUI;
        [SerializeField] private StoryAnimator storyAnimator;

        [Header("Timing")]
        [SerializeField] private float minimumSlideDuration = 3f;

        public Action OnStoryFinished;
        private int  _slideIndex;
        private bool _isSkipped;
        private Func<int, float> _narration;

        private void Awake()
        {
            if (storyUI       == null) storyUI       = GetComponentInChildren<StoryUI>(true);
            if (storyAnimator == null) storyAnimator = GetComponentInChildren<StoryAnimator>(true);
        }

        /// <summary>Swap the story before activating the canvas. narration(slideIndex) returns the voice clip length or -1.</summary>
        public void SetStoryData(StoryData data, Func<int, float> narration = null)
        {
            storyData = data;
            _narration = narration;
        }

        private void OnEnable()
        {
            _slideIndex = 0;
            _isSkipped  = false;
            StartCoroutine(PlayRoutine());
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }
        public void SkipStory()
        {
            if (_isSkipped) return;
            _isSkipped = true;
            StopAllCoroutines();
            storyAnimator?.StopImmediate();
            RuntimeAudioLoader.Instance?.StopCommonAudioSource();
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
                storyUI?.ShowSlide(slide);
                storyAnimator?.AnimateSlideIn(slide.transitionIn);
                float dur = _narration != null ? _narration(_slideIndex) : -1f;
                if (dur < 2f) dur = Mathf.Max(minimumSlideDuration, slide.displayDuration);
                yield return new WaitForSeconds(dur + 0.5f);
                if (_isSkipped) break;
                storyAnimator?.AnimateSlideOut(slide.transitionOut);
                yield return new WaitForSeconds(0.5f);
                _slideIndex++;
            }
            Finish();
        }
        private void Finish()
        {
            OnStoryFinished?.Invoke();
        }
    }
}
