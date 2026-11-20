using UnityEngine;

namespace tmkoc.butterfly
{
    public enum SlideTransitionType { FadeIn, FadeOut, SlideFromRight, SlideFromLeft, ScaleUp, Instant }

    [System.Serializable]
    public class StorySlide
    {
        [Header("Visuals")]
        public Sprite  backgroundSprite;
        public Sprite  foregroundSprite;
        [TextArea(2, 4)]
        public string  captionText;
        [Tooltip("Foreground fills the whole area above the caption (full storyboard scenes) instead of the default square.")]
        public bool    fillSlideArea;

        [Header("Midway swap (optional)")]
        [Tooltip("Foreground swaps to this part-way through the slide; the caption and voice line carry on.")]
        public Sprite  midwayForegroundSprite;
        [Tooltip("When the swap happens, as a fraction of the slide's voice length (or of Display Duration while there is no audio).")]
        [Range(0.1f, 0.9f)] public float midwaySwapAt = 0.5f;

        [Header("Timing")]
        public float   displayDuration = 3f;

        [Header("Transition")]
        public SlideTransitionType transitionIn  = SlideTransitionType.FadeIn;
        public SlideTransitionType transitionOut = SlideTransitionType.FadeOut;

        [Header("Audio")]
        public AudioClip narrationClip;
        public AudioClip slideEnterSFX;
    }

    [CreateAssetMenu(fileName = "Story_New", menuName = "StorySystem/Story Data", order = 1)]
    public class StoryData : ScriptableObject
    {
        [Header("Slides")]
        public StorySlide[] slides;

        [Header("Skip")]
        public bool isSkippable = true;

        [Header("Music")]
        public AudioClip storyBGM;
        [Range(0f, 1f)] public float bgmVolume = 0.7f;
    }
}
