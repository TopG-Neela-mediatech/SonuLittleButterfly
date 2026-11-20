using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace tmkoc.butterfly
{
    /// <summary>Updates Canvas elements per slide. Background, foreground, caption.</summary>
    public class StoryUI : MonoBehaviour
    {
        [Header("Slide UI")]
        [SerializeField] private Image            backgroundImage;
        [SerializeField] private Image            foregroundImage;
        [SerializeField] private TextMeshProUGUI  captionText;
        [SerializeField] private CanvasGroup      slideCanvasGroup;
        [SerializeField] private Image            captionBackground;

        [Header("Full-scene slides (fillSlideArea)")]
        [Tooltip("Space kept free around a full-scene foreground: left/right margin, bottom (above the caption box), top (below the skip button).")]
        [SerializeField] private float fillSideMargin = 50f;
        [SerializeField] private float fillBottom = 310f;
        [SerializeField] private float fillTop = 220f;

        private bool defaultsCaptured;
        private Vector2 fgAnchorMin, fgAnchorMax, fgPos, fgSize;
        private Coroutine swapRoutine;

        public void ShowSlide(StorySlide slide)
        {
            if (backgroundImage != null) { backgroundImage.sprite = slide.backgroundSprite; backgroundImage.enabled = slide.backgroundSprite != null; }
            if (foregroundImage != null)
            {
                if (swapRoutine != null) { StopCoroutine(swapRoutine); swapRoutine = null; }
                foregroundImage.canvasRenderer.SetAlpha(1f);
                foregroundImage.sprite = slide.foregroundSprite;
                foregroundImage.enabled = slide.foregroundSprite != null;
                LayoutForeground(slide.fillSlideArea);
            }
            if (captionText     != null) { captionText.text = slide.captionText; captionText.enabled = !string.IsNullOrEmpty(slide.captionText); }
            if (captionBackground != null) captionBackground.enabled = !string.IsNullOrEmpty(slide.captionText);
            if (slideCanvasGroup != null) slideCanvasGroup.alpha = 1f;
        }

        /// <summary>Quick cross-fade of the foreground to another picture (caption and slide stay).</summary>
        public void SwapForeground(Sprite sprite)
        {
            if (foregroundImage == null || sprite == null) return;
            if (swapRoutine != null) StopCoroutine(swapRoutine);
            swapRoutine = StartCoroutine(SwapRoutine(sprite));
        }

        private System.Collections.IEnumerator SwapRoutine(Sprite sprite)
        {
            const float half = 0.2f;
            foregroundImage.CrossFadeAlpha(0f, half, false);
            yield return new WaitForSeconds(half);
            foregroundImage.sprite = sprite;
            foregroundImage.enabled = true;
            foregroundImage.CrossFadeAlpha(1f, half, false);
            swapRoutine = null;
        }

        /// <summary>Default: the prefab's centred square. fill: stretch over the free area above the caption box (aspect kept).</summary>
        private void LayoutForeground(bool fill)
        {
            var rt = foregroundImage.rectTransform;
            if (!defaultsCaptured)
            {
                defaultsCaptured = true;
                fgAnchorMin = rt.anchorMin; fgAnchorMax = rt.anchorMax; fgPos = rt.anchoredPosition; fgSize = rt.sizeDelta;
            }
            if (fill)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(fillSideMargin, fillBottom);
                rt.offsetMax = new Vector2(-fillSideMargin, -fillTop);
            }
            else
            {
                rt.anchorMin = fgAnchorMin; rt.anchorMax = fgAnchorMax; rt.anchoredPosition = fgPos; rt.sizeDelta = fgSize;
            }
            foregroundImage.preserveAspect = true;
        }

        public void HideSlide()  { if (slideCanvasGroup != null) slideCanvasGroup.alpha = 0f; }
        public void SetAlpha(float a) { if (slideCanvasGroup != null) slideCanvasGroup.alpha = a; }
    }
}