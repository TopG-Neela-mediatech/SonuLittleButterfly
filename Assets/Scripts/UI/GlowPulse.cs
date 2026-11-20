using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Soft breathing halo (e.g. behind the caterpillar so it stands out from the leaf). Put it on an Image child with its own
    /// Canvas sorted just under the creature's canvas; it fades and scales gently in a loop while enabled.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.3f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.55f;
        [SerializeField] private float minScale = 0.95f;
        [SerializeField] private float maxScale = 1.05f;
        [SerializeField] private float period = 1.6f;

        private Image image;
        private Sequence loop;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
        }

        private void OnEnable()
        {
            loop?.Kill();
            var c = image.color; c.a = minAlpha; image.color = c;
            transform.localScale = Vector3.one * minScale;
            float half = Mathf.Max(0.1f, period * 0.5f);
            loop = DOTween.Sequence()
                .Append(image.DOFade(maxAlpha, half).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(maxScale, half).SetEase(Ease.InOutSine))
                .Append(image.DOFade(minAlpha, half).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(minScale, half).SetEase(Ease.InOutSine))
                .SetLoops(-1)
                .SetLink(gameObject);
        }

        private void OnDisable() => loop?.Kill();
    }
}
