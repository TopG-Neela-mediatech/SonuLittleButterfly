using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// End-of-game gratification: the life cycle (Egg → Caterpillar → Chrysalis → Butterfly),
    /// the Butterfly Garden Helper badge and the 10-star reward. No button: it holds for a moment, then hands control back.
    /// </summary>
    public class FinalCelebrationPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform[] lifeCycleSteps;
        [SerializeField] private RectTransform badge;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [Tooltip("Seconds the finished celebration stays on screen before continuing.")]
        [SerializeField] private float holdSeconds = 2.5f;

        private Action onContinue;
        private Sequence showSeq;

        private bool initialized;

        // The panel starts inactive in the scene, so Awake only runs on the first Show(); it must not deactivate.
        private void Awake() => Init();

        private void Init()
        {
            if (initialized) return;
            initialized = true;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show(int starsAwarded, Action continueCallback)
        {
            onContinue = continueCallback;
            gameObject.SetActive(true);
            Init();
            transform.SetAsLastSibling();
            if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.DOFade(1f, 0.4f); }
            if (titleText != null) titleText.text = "Butterfly Garden Helper!";
            if (rewardText != null) rewardText.text = starsAwarded > 0 ? $"You earned a badge and {starsAwarded} stars!" : "You earned a badge!";
            showSeq?.Kill(); // a second Show restarts cleanly instead of an old sequence hiding the new one
            var seq = showSeq = DOTween.Sequence();
            if (lifeCycleSteps != null)
                foreach (var s in lifeCycleSteps)
                {
                    if (s == null) continue;
                    s.localScale = Vector3.zero;
                    seq.Append(s.DOScale(1f, 0.45f).SetEase(Ease.OutBack));
                }
            if (badge != null)
            {
                badge.localScale = Vector3.zero;
                seq.Append(badge.DOScale(1f, 0.6f).SetEase(Ease.OutBack));
                seq.Append(badge.DORotate(new Vector3(0, 0, 360f), 1.2f, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.OutCubic));
            }
            seq.AppendInterval(holdSeconds);
            seq.OnComplete(() => { Hide(); onContinue?.Invoke(); });
        }

        private void Hide()
        {
            if (badge != null) badge.DOKill();
            gameObject.SetActive(false);
        }
    }
}
