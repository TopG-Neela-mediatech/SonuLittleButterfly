using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Hand pointer that drags from one item to its mergeable neighbour. Forced on the tutorial level,
    /// then shown again after the player idles for a while, as in the reference games.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private RectTransform hand;
        [SerializeField] private CanvasGroup handCanvasGroup;
        [SerializeField] private ButterflyLevelController levelController;
        [SerializeField] private float idleTimeout = 10f;
        [SerializeField] private float dragDuration = 0.9f;

        private Coroutine idleRoutine;
        private Sequence handSeq;
        private bool levelActive;
        private bool forced;

        private void Awake()
        {
            if (handCanvasGroup == null && hand != null) handCanvasGroup = hand.GetComponent<CanvasGroup>();
            HideHand();
        }

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin += OnLevelEnd;
            GameManager.Instance.OnLevelLose += OnLevelEnd;
            if (levelController != null && levelController.Board != null)
            {
                levelController.Board.OnPlayerInteracted += OnPlayerInteracted;
                levelController.Board.OnMerged += OnMerged;
            }
        }
        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnLevelStart -= OnLevelStart;
            GameManager.Instance.OnLevelWin -= OnLevelEnd;
            GameManager.Instance.OnLevelLose -= OnLevelEnd;
            if (levelController != null && levelController.Board != null)
            {
                levelController.Board.OnPlayerInteracted -= OnPlayerInteracted;
                levelController.Board.OnMerged -= OnMerged;
            }
        }

        private void OnLevelStart()
        {
            levelActive = true;
            forced = levelController != null && levelController.CurrentLevel != null && levelController.CurrentLevel.isTutorial;
            if (forced) ShowHint(); else RestartIdleTimer();
        }
        public void OnLevelRestarted() => OnLevelStart();
        private void OnLevelEnd() { levelActive = false; HideHand(); StopIdleTimer(); }
        private void OnPlayerInteracted() { if (!forced) HideHand(); RestartIdleTimer(); }
        private void OnMerged(int tier, int cell) { forced = false; HideHand(); RestartIdleTimer(); }

        private void RestartIdleTimer()
        {
            StopIdleTimer();
            if (levelActive) idleRoutine = StartCoroutine(IdleRoutine());
        }
        private void StopIdleTimer() { if (idleRoutine != null) StopCoroutine(idleRoutine); idleRoutine = null; }
        private IEnumerator IdleRoutine()
        {
            yield return new WaitForSeconds(idleTimeout);
            if (levelActive && levelController != null && !levelController.IsBusy)
            {
                GameManager.Instance?.SoundManager?.PlayIdle();
                ShowHint();
            }
        }

        private void ShowHint()
        {
            if (hand == null || levelController == null || levelController.Board == null) return;
            var board = levelController.Board;
            if (!board.TryFindMergeablePair(out int a, out int b)) { HideHand(); return; }
            var from = board.GetCellRect(a); var to = board.GetCellRect(b);
            if (from == null || to == null) return;
            handSeq?.Kill();
            hand.gameObject.SetActive(true);
            if (handCanvasGroup != null) handCanvasGroup.alpha = 0f;
            hand.position = from.position;
            handSeq = DOTween.Sequence();
            if (handCanvasGroup != null) handSeq.Append(handCanvasGroup.DOFade(1f, 0.25f));
            handSeq.Append(hand.DOScale(0.85f, 0.2f));
            handSeq.Append(hand.DOMove(to.position, dragDuration).SetEase(Ease.InOutSine));
            handSeq.Append(hand.DOScale(1f, 0.2f));
            if (handCanvasGroup != null) handSeq.Append(handCanvasGroup.DOFade(0f, 0.3f));
            handSeq.AppendCallback(() => hand.position = from.position);
            handSeq.AppendInterval(0.3f);
            handSeq.SetLoops(-1);
        }

        private void HideHand()
        {
            handSeq?.Kill();
            handSeq = null;
            if (hand != null) { hand.DOKill(); hand.localScale = Vector3.one; hand.gameObject.SetActive(false); }
        }
    }
}
