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

        [Header("Timing")]
        [SerializeField] private float deliverDelay = 0.9f;
        [SerializeField] private float flightDuration = 0.8f;

        public LevelDefinition CurrentLevel { get; private set; }
        public ThemeData CurrentTheme { get; private set; }
        public MergeBoard Board => board;
        public GardenView Garden => garden;
        public bool IsBusy => busy;

        private readonly Dictionary<int, int> remaining = new Dictionary<int, int>();
        private readonly Dictionary<int, int> initial = new Dictionary<int, int>();
        private int levelIndex, totalLevels;
        private bool busy;

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

        public void InitializeLevel(LevelDefinition def, ThemeData theme, int index, int total)
        {
            StopAllCoroutines();
            CurrentLevel = def;
            CurrentTheme = theme;
            levelIndex = index;
            totalLevels = total;
            busy = false;

            remaining.Clear(); initial.Clear();
            foreach (var g in def.goals)
            {
                remaining[g.tier] = remaining.TryGetValue(g.tier, out var r) ? r + g.count : g.count;
                initial[g.tier] = remaining[g.tier];
            }

            if (levelLabel != null) levelLabel.text = $"LEVEL {index + 1} / {total}";
            if (objectiveText != null) objectiveText.text = def.objective;
            if (instructionText != null) instructionText.text = "Tap or drag two matching items to merge them!";
            if (hintText != null) hintText.text = "Match two items of the same kind to grow them.";

            board.Setup(def, theme);
            goalsPanel?.Setup(def.goals, theme, board.FallbackSprite);
            garden.SetCaption(theme != null ? theme.gardenCaption : "");
            garden.ShowStage(theme != null ? theme.stage : LifeStage.Egg, Progress());
        }

        public void Restart()
        {
            if (busy || CurrentLevel == null) return;
            InitializeLevel(CurrentLevel, CurrentTheme, levelIndex, totalLevels);
            GameManager.Instance?.TutorialController?.OnLevelRestarted();
        }

        private float Progress()
        {
            int total = 0, left = 0;
            foreach (var kv in initial) total += kv.Value;
            foreach (var kv in remaining) left += kv.Value;
            return total == 0 ? 1f : Mathf.Clamp01((float)(total - left) / total);
        }

        private void HandleMoved() { if (hintText != null) hintText.text = "Now merge two matching items."; }

        private void HandleInvalidMove()
        {
            if (hintText != null) hintText.text = "Find two matching items to merge.";
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Wrong);
        }

        private void HandleMerged(int tier, int cell)
        {
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Merge);
            GameManager.Instance?.InvokeItemMerged(tier);
            bool goal = remaining.TryGetValue(tier, out var left) && left > 0;
            if (hintText != null) hintText.text = goal ? "Great match! Watch what happens..." : "Wonderful! Keep merging.";
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
                var flyer = Instantiate(board.ItemTemplate, flightLayer);
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
                Destroy(flyer.gameObject);
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
            GameManager.Instance?.SoundManager?.PlaySFX(SoundManager.SfxType.Deliver);
            GameManager.Instance?.SoundManager?.PlayGoalDelivered();
            int totalLeft = 0; foreach (var kv in remaining) totalLeft += kv.Value;
            GameManager.Instance?.InvokeGoalDelivered(tier, totalLeft);
            if (hintText != null) hintText.text = "Yay! You helped the garden grow!";

            if (totalLeft == 0)
            {
                yield return new WaitForSeconds(0.7f);
                GameManager.Instance?.InvokeLevelWin();
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
            garden.PlayStageTransform(() => { busy = false; done?.Invoke(); });
        }
    }
}
