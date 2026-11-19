using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Lives on the level root (LevelMain/Level1). Initializes a level from its definition, owns the
    /// goal bookkeeping and the "goal item flies into the garden" moment, and reports the win.
    /// </summary>
    public class ButterflyLevelController : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private GardenView garden;
        [SerializeField] private MergeBoard board;
        [SerializeField] private GoalsPanel goalsPanel;
        [SerializeField] private RectTransform flightLayer;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private Button restartButton;

        [Header("Intro (slides in at level start)")]
        [SerializeField] private RectTransform levelPill;
        [SerializeField] private RectTransform goalsRect;
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private float introDuration = 1.1f;

        [Header("Round dots (sub-levels of the main level)")]
        [SerializeField] private Image[] subDots;
        [SerializeField] private Color dotDone = new Color(1f, 0.71f, 0.33f, 1f);
        [SerializeField] private Color dotCurrent = Color.white;
        [SerializeField] private Color dotPending = new Color(1f, 1f, 1f, 0.45f);

        [Header("Timing")]
        [SerializeField] private float deliverDelay = 0.9f;
        [SerializeField] private float flightDuration = 0.8f;

        public LevelDefinition CurrentLevel { get; private set; }
        public ThemeData CurrentTheme { get; private set; }
        public MergeBoard Board => board;
        public GardenView Garden => garden;
        public bool IsBusy => busy;
        public float IntroDuration => introDuration;

        /// <summary>All goals of the current round delivered (LevelManager decides: next round or main level won).</summary>
        public event Action OnSubLevelComplete;

        private readonly Dictionary<int, int> remaining = new Dictionary<int, int>();
        private readonly Dictionary<int, int> initial = new Dictionary<int, int>();
        private int levelIndex, totalLevels, subIndex, subCount = 1;
        private bool busy;
        private MergeItemView flyer;      // reused for every goal delivery
        private Vector2 levelPillHome, boardHome, objectiveHome;
        private bool homesCaptured;
        private Sequence introSeq;

        private void Awake()
        {
            if (garden == null) garden = GetComponentInChildren<GardenView>(true);
            if (board == null) board = GetComponentInChildren<MergeBoard>(true);
            if (goalsPanel == null) goalsPanel = GetComponentInChildren<GoalsPanel>(true);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
        }

        private void OnEnable()
        {
            if (board == null) return;
            board.OnMerged += HandleMerged;
            board.OnInvalidMove += HandleInvalidMove;
            board.OnMoved += HandleMoved;
        }
        private void OnDisable()
        {
            if (board == null) return;
            board.OnMerged -= HandleMerged;
            board.OnInvalidMove -= HandleInvalidMove;
            board.OnMoved -= HandleMoved;
        }

        /// <param name="index">main level (0..3)</param>
        /// <param name="sub">round inside the main level; the garden shows growth across rounds</param>
        /// <param name="fullIntro">first round: the garden creatures fly in too; later rounds only swap the board</param>
        public void InitializeLevel(LevelDefinition def, ThemeData theme, int index, int total, int sub, int subTotal, bool fullIntro)
        {
            StopAllCoroutines();
            CurrentLevel = def;
            CurrentTheme = theme;
            levelIndex = index;
            totalLevels = total;
            subIndex = Mathf.Max(0, sub);
            subCount = Mathf.Max(1, subTotal);
            busy = false;
            lastFullIntro = fullIntro;

            remaining.Clear(); initial.Clear();
            foreach (var g in def.goals)
            {
                remaining[g.tier] = remaining.TryGetValue(g.tier, out var r) ? r + g.count : g.count;
                initial[g.tier] = remaining[g.tier];
            }

            SetTextBarVisible(true, false);
            if (levelLabel != null) levelLabel.text = $"LEVEL {index + 1}";
            if (objectiveText != null) objectiveText.text = def.objective;
            if (instructionText != null) instructionText.gameObject.SetActive(false);
            if (hintText != null) hintText.gameObject.SetActive(false);

            board.Setup(def, theme);
            goalsPanel?.Setup(def.goals, theme, board.FallbackSprite);
            garden.SetCaption("");
            garden.ShowStage(theme != null ? theme.stage : LifeStage.Egg, Progress());
            RefreshDots(false);
            PlayIntro(fullIntro);
        }

        private bool lastFullIntro = true;

        private void RefreshDots(bool punchCompleted)
        {
            if (subDots == null) return;
            for (int i = 0; i < subDots.Length; i++)
            {
                var d = subDots[i];
                if (d == null) continue;
                d.gameObject.SetActive(subCount > 1 && i < subCount);
                d.color = i < subIndex ? dotDone : i == subIndex ? dotCurrent : dotPending;
                d.rectTransform.DOKill(true);
                d.rectTransform.localScale = Vector3.one * (i == subIndex ? 1.25f : 1f);
            }
            if (punchCompleted && subIndex < subDots.Length && subDots[subIndex] != null)
            {
                var d = subDots[subIndex];
                d.color = dotDone;
                d.rectTransform.DOPunchScale(Vector3.one * 0.6f, 0.45f, 6, 0.5f);
            }
        }

        /// <summary>Between rounds: the finished round's dot fills, the board slides away; then done() starts the next round.</summary>
        public void PlaySubLevelOutro(Action done)
        {
            busy = true;
            board.Locked = true;
            RefreshDots(true);
            if (subDots != null && subIndex < subDots.Length && subDots[subIndex] != null && UiFx.Instance != null)
                UiFx.Instance.Ring(subDots[subIndex].transform.position, 90f, new Color(1f, 0.8f, 0.4f, 0.9f));
            var seq = DOTween.Sequence();
            seq.AppendInterval(0.35f);
            if (boardRect != null)
            {
                float h = ((RectTransform)transform).rect.height;
                seq.Append(boardRect.DOAnchorPos(boardHome - new Vector2(0f, h), 0.45f).SetEase(Ease.InBack, 1.2f));
            }
            if (goalsRect != null) seq.Join(goalsRect.DOScale(0f, 0.3f).SetEase(Ease.InBack));
            seq.OnComplete(() => done?.Invoke());
        }

        /// <summary>Level start: the garden creatures fly/crawl in, the board slides up, the HUD drops in, items pop.</summary>
        private void PlayIntro(bool fullIntro)
        {
            CaptureHomes();
            introSeq?.Kill(true);
            busy = true;
            board.Locked = true;
            introSeq = DOTween.Sequence();
            float h = ((RectTransform)transform).rect.height;
            if (levelPill != null && fullIntro)
            {
                levelPill.anchoredPosition = levelPillHome + new Vector2(0f, 320f);
                introSeq.Insert(0f, levelPill.DOAnchorPos(levelPillHome, 0.5f).SetEase(Ease.OutBack));
            }
            if (objectiveText != null)
            {
                var ort = objectiveText.rectTransform;
                ort.anchoredPosition = objectiveHome + new Vector2(0f, 60f);
                objectiveText.alpha = 0f;
                introSeq.Insert(0.2f, ort.DOAnchorPos(objectiveHome, 0.4f).SetEase(Ease.OutCubic));
                introSeq.Insert(0.2f, objectiveText.DOFade(1f, 0.4f));
            }
            if (boardRect != null)
            {
                boardRect.anchoredPosition = boardHome - new Vector2(0f, h);
                introSeq.Insert(0.1f, boardRect.DOAnchorPos(boardHome, 0.6f).SetEase(Ease.OutBack, 0.9f));
            }
            if (goalsRect != null)
            {
                goalsRect.localScale = Vector3.zero;
                introSeq.Insert(0.45f, goalsRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            }
            board.PlayItemsIntro(0.55f);
            if (fullIntro) garden.PlayIntro(0.05f);
            introSeq.AppendInterval(Mathf.Max(0f, introDuration - introSeq.Duration()));
            introSeq.OnComplete(() => { busy = false; board.Locked = false; });
        }

        private void CaptureHomes()
        {
            if (homesCaptured) return;
            homesCaptured = true;
            if (levelPill != null) levelPillHome = levelPill.anchoredPosition;
            if (boardRect != null) boardHome = boardRect.anchoredPosition;
            if (objectiveText != null) objectiveHome = objectiveText.rectTransform.anchoredPosition;
        }

        private void OnDestroy() => introSeq?.Kill();

        public void Restart()
        {
            if (busy || CurrentLevel == null) return;
            InitializeLevel(CurrentLevel, CurrentTheme, levelIndex, totalLevels, subIndex, subCount, lastFullIntro);
            GameManager.Instance?.TutorialController?.OnLevelRestarted();
        }

        /// <summary>Growth of the whole main level: finished rounds plus the share of this round's goals delivered.</summary>
        private float Progress()
        {
            int total = 0, left = 0;
            foreach (var kv in initial) total += kv.Value;
            foreach (var kv in remaining) left += kv.Value;
            float round = total == 0 ? 1f : Mathf.Clamp01((float)(total - left) / total);
            return Mathf.Clamp01((subIndex + round) / subCount);
        }

        private void HandleMoved() => GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Move);

        private float lastWrongVoice = -100f;
        private void HandleInvalidMove()
        {
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Wrong);
            // the gentle "find two that match" hint, at most every few seconds so repeated taps don't spam it
            if (Time.time - lastWrongVoice < 4f) return;
            lastWrongVoice = Time.time;
            GameManager.Instance?.SoundManager?.PlayWrongMove();
        }

        private void HandleMerged(int tier, int cell)
        {
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Merge);
            GameManager.Instance?.InvokeItemMerged(tier);
            bool goal = remaining.TryGetValue(tier, out var left) && left > 0;
            if (!goal) return;
            busy = true;
            board.Locked = true;
            StartCoroutine(DeliverRoutine(tier, cell));
        }

        private IEnumerator DeliverRoutine(int tier, int cell)
        {
            yield return new WaitForSeconds(deliverDelay);
            var source = board.GetItem(cell);
            var target = garden.DeliveryTarget;
            if (source != null && target != null && flightLayer != null)
            {
                if (flyer == null) flyer = Instantiate(board.ItemTemplate, flightLayer);
                flyer.gameObject.SetActive(true);
                flyer.Apply(tier, CurrentTheme, board.FallbackSprite);
                var frt = flyer.Rect;
                frt.sizeDelta = source.Rect.rect.size;
                frt.position = source.Rect.position;
                frt.localScale = Vector3.one;
                flyer.SetRaycast(false);
                board.RemoveItem(cell);
                var seq = DOTween.Sequence();
                seq.Append(frt.DOMove(target.position, flightDuration).SetEase(Ease.InQuad));
                seq.Join(frt.DOScale(0.45f, flightDuration).SetEase(Ease.InQuad));
                var cg = flyer.GetComponent<CanvasGroup>();
                if (cg != null) seq.Join(cg.DOFade(0.25f, flightDuration));
                yield return seq.WaitForCompletion();
                if (cg != null) cg.alpha = 1f;
                flyer.gameObject.SetActive(false);
            }
            else
            {
                board.RemoveItem(cell);
                yield return new WaitForSeconds(flightDuration);
            }

            remaining[tier] = Mathf.Max(0, remaining[tier] - 1);
            goalsPanel?.SetCollected(tier, initial[tier] - remaining[tier]);
            garden.SetProgress(Progress());
            garden.PlayDeliveryPop();
            if (UiFx.Instance != null && target != null)
            {
                UiFx.Instance.Play(FxType.Confetti, target.position);
                UiFx.Instance.Ring(target.position, 360f, new Color(1f, 0.95f, 0.6f, 0.85f));
            }
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Deliver);
            GameManager.Instance?.SoundManager?.PlayGoalDelivered();
            int totalLeft = 0; foreach (var kv in remaining) totalLeft += kv.Value;
            GameManager.Instance?.InvokeGoalDelivered(tier, totalLeft);

            if (totalLeft == 0)
            {
                yield return new WaitForSeconds(0.7f);
                if (OnSubLevelComplete != null) OnSubLevelComplete.Invoke();
                else GameManager.Instance?.InvokeLevelWin();
            }
            else
            {
                busy = false;
                board.Locked = false;
            }
        }

        /// <summary>Plays the stage gratification on the garden; the board stays locked.</summary>
        public void PlayStageTransform(Action done)
        {
            busy = true;
            board.Locked = true;
            RefreshDots(true); // last round's dot fills
            SetTextBarVisible(false, true); // the garden animation plays without the objective / goal bar under it
            garden.PlayStageTransform(() => { busy = false; done?.Invoke(); });
        }

        /// <summary>Objective line + goal chips. Hidden while a level-end animation plays, shown again by InitializeLevel.</summary>
        private void SetTextBarVisible(bool visible, bool animate)
        {
            foreach (var rt in new[] { objectiveText != null ? objectiveText.rectTransform : null, goalsRect })
            {
                if (rt == null) continue;
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.DOKill();
                if (visible) { rt.gameObject.SetActive(true); cg.alpha = 1f; continue; }
                if (!animate) { cg.alpha = 0f; rt.gameObject.SetActive(false); continue; }
                var go = rt.gameObject;
                cg.DOFade(0f, 0.35f).OnComplete(() => go.SetActive(false));
            }
        }
    }
}
