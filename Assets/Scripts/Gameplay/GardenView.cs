using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// The garden picture at the top of the screen. Props are Images using the SonuButterflyAsset art,
    /// shown/hidden and tweened per life-cycle stage, mirroring drawScene()/transformScene() in the prototype.
    /// Every prop is a serialized reference; missing ones are looked up by child name.
    /// </summary>
    public class GardenView : MonoBehaviour
    {
        [Header("Props")]
        [SerializeField] private RectTransform plant;
        [SerializeField] private Image plantImage;
        [SerializeField] private RectTransform branch;
        [SerializeField] private RectTransform egg;
        [SerializeField] private RectTransform caterpillar;
        [SerializeField] private RectTransform chrysalis;
        [SerializeField] private RectTransform butterfly;
        [SerializeField] private RectTransform[] flowers;
        [SerializeField] private RectTransform sunGlow;
        [SerializeField] private Image nightOverlay;
        [SerializeField] private RectTransform deliveryTarget;
        [SerializeField] private TextMeshProUGUI caption;

        [Header("Plant growth (Egg stage)")]
        [Tooltip("Plant sprites from small to fully grown, picked by watering progress (e.g. Half Plant, Plant_01).")]
        [SerializeField] private Sprite[] plantGrowthSprites;
        [Tooltip("Leaves around the plant that turn from dry to fresh as the plant is watered.")]
        [SerializeField] private Image[] plantLeaves;
        [SerializeField] private Sprite[] dryLeafSprites;
        [SerializeField] private Sprite[] freshLeafSprites;

        [Header("Branch leaf (Chrysalis stage)")]
        [SerializeField] private Image branchLeaf;
        [SerializeField] private Sprite branchDryLeafSprite;
        [SerializeField] private Sprite branchFreshLeafSprite;

        [Header("Colors")]
        [SerializeField] private Color dryPlantColor = new Color(1f, 0.93f, 0.7f);
        [SerializeField] private Color healthyPlantColor = Color.white;

        public RectTransform DeliveryTarget => deliveryTarget != null ? deliveryTarget : plant;

        private LifeStage stage;
        private Vector2 butterflyHome;
        private Vector2 caterpillarHome;
        private Tween idleTween;

        private void Awake()
        {
            var t = transform;
            if (plant == null) plant = t.Find("Plant") as RectTransform;
            if (plantImage == null && plant != null) plantImage = plant.GetComponent<Image>();
            if (branch == null) branch = t.Find("Branch") as RectTransform;
            if (egg == null) egg = t.Find("Egg") as RectTransform;
            if (caterpillar == null) caterpillar = t.Find("Caterpillar") as RectTransform;
            if (chrysalis == null) chrysalis = t.Find("Chrysalis") as RectTransform;
            if (butterfly == null) butterfly = t.Find("Butterfly") as RectTransform;
            if (sunGlow == null) sunGlow = t.Find("SunGlow") as RectTransform;
            if (nightOverlay == null) nightOverlay = t.Find("NightOverlay")?.GetComponent<Image>();
            if (deliveryTarget == null) deliveryTarget = t.Find("DeliveryTarget") as RectTransform;
            if (caption == null) caption = t.Find("CaptionPill/Caption")?.GetComponent<TextMeshProUGUI>();
            if (flowers == null || flowers.Length == 0)
            {
                var list = new System.Collections.Generic.List<RectTransform>();
                for (int i = 0; i < 4; i++) { var f = t.Find("Flower" + i) as RectTransform; if (f != null) list.Add(f); }
                flowers = list.ToArray();
            }
            if (butterfly != null) butterflyHome = butterfly.anchoredPosition;
            if (caterpillar != null) caterpillarHome = caterpillar.anchoredPosition;
        }

        public void SetCaption(string text) { if (caption != null) caption.text = text; }

        /// <summary>Static layout for a stage at a given goal progress (0..1).</summary>
        public void ShowStage(LifeStage newStage, float progress)
        {
            KillTweens();
            stage = newStage;
            Show(plant, true);
            Show(branch, stage == LifeStage.Caterpillar && progress > 0.1f || stage == LifeStage.Chrysalis);
            Show(egg, false);
            Show(caterpillar, stage == LifeStage.Caterpillar);
            Show(chrysalis, stage == LifeStage.Chrysalis);
            Show(butterfly, stage == LifeStage.Egg || stage == LifeStage.Butterfly);
            Show(sunGlow, stage == LifeStage.Chrysalis && progress > 0.3f);
            if (nightOverlay != null) nightOverlay.color = new Color(0.1f, 0.12f, 0.3f, 0f);
            if (butterfly != null) butterfly.anchoredPosition = butterflyHome;
            if (caterpillar != null) caterpillar.anchoredPosition = caterpillarHome;
            if (butterfly != null) butterfly.localScale = Vector3.one;
            if (chrysalis != null) chrysalis.localScale = Vector3.one;
            SetProgress(progress);
            StartIdle();
        }

        /// <summary>Progress-driven growth within the stage.</summary>
        public void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            switch (stage)
            {
                case LifeStage.Egg:
                    if (plant != null) plant.localScale = Vector3.one * (0.75f + p * 0.25f);
                    if (plantImage != null) plantImage.color = Color.Lerp(dryPlantColor, healthyPlantColor, p);
                    SetPlantSprite(p);
                    SetLeaves(p);
                    break;
                case LifeStage.Caterpillar:
                    if (plant != null) plant.localScale = Vector3.one * 0.95f;
                    if (plantImage != null) plantImage.color = healthyPlantColor;
                    if (caterpillar != null) caterpillar.localScale = Vector3.one * (0.7f + p * 0.4f);
                    Show(branch, p > 0.1f);
                    break;
                case LifeStage.Chrysalis:
                    if (plant != null) plant.localScale = Vector3.one * 0.9f;
                    if (plantImage != null) plantImage.color = healthyPlantColor;
                    Show(sunGlow, p > 0.3f);
                    break;
                case LifeStage.Butterfly:
                    if (plant != null) plant.localScale = Vector3.one * 0.9f;
                    if (plantImage != null) plantImage.color = healthyPlantColor;
                    int visible = 1 + Mathf.FloorToInt(p * 3.99f);
                    for (int i = 0; i < flowers.Length; i++) Show(flowers[i], i < visible);
                    break;
            }
            if (stage != LifeStage.Butterfly) foreach (var f in flowers) Show(f, false);
            if (stage != LifeStage.Egg) { SetPlantSprite(1f); SetLeaves(1f); }
            SetBranchLeaf(stage == LifeStage.Chrysalis ? p : 1f);
        }

        private void SetPlantSprite(float p)
        {
            if (plantImage == null || plantGrowthSprites == null || plantGrowthSprites.Length == 0) return;
            int i = Mathf.Clamp(Mathf.FloorToInt(p * plantGrowthSprites.Length), 0, plantGrowthSprites.Length - 1);
            if (plantGrowthSprites[i] != null) plantImage.sprite = plantGrowthSprites[i];
        }

        /// <summary>Leaf i turns fresh once progress passes (i+1)/(n+1).</summary>
        private void SetLeaves(float p)
        {
            if (plantLeaves == null) return;
            int n = plantLeaves.Length;
            for (int i = 0; i < n; i++)
            {
                var leaf = plantLeaves[i];
                if (leaf == null) continue;
                bool fresh = p >= (i + 1f) / (n + 1f);
                var arr = fresh ? freshLeafSprites : dryLeafSprites;
                if (arr != null && i < arr.Length && arr[i] != null && leaf.sprite != arr[i])
                {
                    leaf.sprite = arr[i];
                    if (fresh) { leaf.rectTransform.DOKill(true); leaf.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 5, 0.6f); }
                }
            }
        }

        private void SetBranchLeaf(float p)
        {
            if (branchLeaf == null) return;
            var sp = p >= 0.5f ? branchFreshLeafSprite : branchDryLeafSprite;
            if (sp != null) branchLeaf.sprite = sp;
        }

        /// <summary>Quick pop when a goal item lands in the garden.</summary>
        public void PlayDeliveryPop()
        {
            transform.DOKill(true);
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.04f, 0.5f, 5, 0.5f);
            if (plant != null)
            {
                plant.DOKill(true);
                plant.DOPunchScale(Vector3.one * 0.12f, 0.45f, 6, 0.6f);
            }
        }

        /// <summary>Stage gratification animation (transformScene in the prototype). Calls done when finished.</summary>
        public void PlayStageTransform(Action done)
        {
            KillTweens();
            var seq = DOTween.Sequence();
            switch (stage)
            {
                case LifeStage.Egg:
                    // mother butterfly lands on the leaf, lays an egg, time passes, caterpillar hatches
                    Show(egg, false); Show(caterpillar, false);
                    if (butterfly != null && (egg != null || plant != null))
                    {
                        var spot = egg != null ? egg : plant;
                        Vector3 landing = spot.TransformPoint(new Vector3(0f, spot.rect.height * 0.35f, 0f));
                        seq.Append(butterfly.DOMove(landing, 1.2f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => { Show(egg, true); if (egg != null) { egg.localScale = Vector3.zero; } SetCaption("The butterfly lays an egg on the healthy leaf!"); });
                    if (egg != null) seq.Append(egg.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
                    if (butterfly != null) seq.Append(butterfly.DOAnchorPos(butterflyHome + new Vector2(260f, 160f), 1.4f).SetEase(Ease.InSine));
                    seq.AppendCallback(() => Show(butterfly, false));
                    seq.Append(DayNightCycle(1.6f));
                    seq.AppendCallback(() => { SetCaption("The egg hatches into a caterpillar!"); Show(caterpillar, true); if (caterpillar != null) caterpillar.localScale = Vector3.zero; });
                    if (egg != null) seq.Append(egg.DOScale(0f, 0.4f).SetEase(Ease.InBack));
                    if (caterpillar != null) seq.Join(caterpillar.DOScale(0.75f, 0.6f).SetEase(Ease.OutBack));
                    seq.AppendInterval(0.8f);
                    break;

                case LifeStage.Caterpillar:
                    SetCaption("The caterpillar climbs to the branch...");
                    Show(branch, true);
                    if (branch != null) { branch.localScale = new Vector3(0f, 1f, 1f); seq.Append(branch.DOScaleX(1f, 0.6f).SetEase(Ease.OutBack)); }
                    if (caterpillar != null && (chrysalis != null || branch != null))
                    {
                        var hang = chrysalis != null ? chrysalis.position : branch.position;
                        seq.Append(caterpillar.DOMove(hang, 1.4f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => { SetCaption("The caterpillar forms a chrysalis!"); Show(chrysalis, true); if (chrysalis != null) chrysalis.localScale = Vector3.zero; });
                    if (caterpillar != null) seq.Append(caterpillar.DOScale(0f, 0.5f).SetEase(Ease.InBack));
                    if (chrysalis != null) seq.Join(chrysalis.DOScale(1f, 0.7f).SetEase(Ease.OutBack));
                    seq.AppendCallback(() => Show(caterpillar, false));
                    seq.AppendInterval(0.8f);
                    break;

                case LifeStage.Chrysalis:
                    SetCaption("Days pass as the chrysalis rests...");
                    seq.Append(DayNightCycle(2.2f));
                    seq.AppendCallback(() => { SetCaption("A butterfly emerges from the chrysalis!"); Show(butterfly, true); if (butterfly != null && chrysalis != null) { butterfly.position = chrysalis.TransformPoint(chrysalis.rect.center); butterfly.localScale = Vector3.zero; } });
                    if (chrysalis != null) seq.Append(chrysalis.DOScale(0f, 0.5f).SetEase(Ease.InBack));
                    if (butterfly != null)
                    {
                        seq.Join(butterfly.DOScale(1f, 0.8f).SetEase(Ease.OutBack));
                        seq.Append(butterfly.DOAnchorPos(butterflyHome, 1.3f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => Show(chrysalis, false));
                    seq.AppendInterval(0.6f);
                    break;

                case LifeStage.Butterfly:
                    SetCaption("The butterfly drinks nectar and flies!");
                    foreach (var f in flowers) Show(f, true);
                    if (butterfly != null)
                    {
                        foreach (var f in flowers)
                        {
                            if (f == null) continue;
                            var flower = f;
                            seq.Append(butterfly.DOMove(flower.TransformPoint(new Vector3(0f, flower.rect.height * 0.2f, 0f)), 0.7f).SetEase(Ease.InOutSine));
                            seq.Append(butterfly.DOPunchScale(Vector3.one * 0.15f, 0.35f, 4, 0.5f));
                        }
                        seq.Append(butterfly.DOAnchorPos(butterflyHome + new Vector2(0f, 120f), 1f).SetEase(Ease.InOutSine));
                        seq.Append(butterfly.DOAnchorPos(butterflyHome, 0.8f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendInterval(0.5f);
                    break;
            }
            seq.OnComplete(() => { StartIdle(); done?.Invoke(); });
        }

        private Tween DayNightCycle(float duration)
        {
            if (nightOverlay == null) return DOVirtual.DelayedCall(duration, () => { });
            var c = nightOverlay.color;
            var s = DOTween.Sequence();
            s.Append(nightOverlay.DOColor(new Color(c.r, c.g, c.b, 0.55f), duration * 0.4f));
            s.Append(nightOverlay.DOColor(new Color(c.r, c.g, c.b, 0f), duration * 0.6f));
            return s;
        }

        private void StartIdle()
        {
            idleTween?.Kill();
            if (butterfly != null && butterfly.gameObject.activeSelf)
            {
                butterfly.anchoredPosition = butterflyHome;
                idleTween = butterfly.DOAnchorPosY(butterflyHome.y + 18f, 1.1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            else if (caterpillar != null && caterpillar.gameObject.activeSelf)
            {
                var s = caterpillar.localScale;
                idleTween = caterpillar.DOScaleX(s.x * 1.08f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }

        private void KillTweens()
        {
            idleTween?.Kill();
            idleTween = null;
            foreach (var r in new[] { plant, branch, egg, caterpillar, chrysalis, butterfly }) if (r != null) r.DOKill();
            if (nightOverlay != null) nightOverlay.DOKill();
        }

        private static void Show(RectTransform r, bool on) { if (r != null) r.gameObject.SetActive(on); }

        private void OnDisable() => KillTweens();
    }
}
