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

        [Header("Branch growth (Chrysalis stage)")]
        [Tooltip("Filled (horizontal, origin left) image: the branch grows out from the left edge.")]
        [SerializeField] private Image branchImage;
        [Tooltip("Parent of the branch, its leaves and the chrysalis; scaled down so they stay inside the garden on narrow screens.")]
        [SerializeField] private RectTransform branchHolder;
        [SerializeField] private float branchEdgeMargin = 12f;
        [Tooltip("Spot on top of the branch stub where the fed caterpillar waits during the Chrysalis stage (child of Branch Holder).")]
        [SerializeField] private RectTransform caterpillarBranchSeat;
        [SerializeField] private float caterpillarFullScale = 1.1f;
        [Tooltip("Fill the caterpillar climbs onto at the end of the Caterpillar stage (must reach the chrysalis).")]
        [SerializeField, Range(0f, 1f)] private float branchStubFill = 0.65f;
        [Tooltip("Stage progress at which the branch reaches its full length.")]
        [SerializeField, Range(0.05f, 1f)] private float branchFullAt = 0.6f;
        [Tooltip("Leaves on the twig tips, popped in as the branch grows.")]
        [SerializeField] private RectTransform[] branchLeaves;
        [Tooltip("Stage progress at which each leaf appears (same order as Branch Leaves).")]
        [SerializeField] private float[] branchLeafThresholds;

        [Header("Colors")]
        [SerializeField] private Color dryPlantColor = new Color(1f, 0.93f, 0.7f);
        [SerializeField] private Color healthyPlantColor = Color.white;

        /// <summary>Where goal items fly to: the thing this stage is caring for.</summary>
        public RectTransform DeliveryTarget
        {
            get
            {
                switch (stage)
                {
                    case LifeStage.Caterpillar: if (caterpillar != null) return caterpillar; break;
                    case LifeStage.Chrysalis: if (caterpillar != null) return caterpillar; break;
                    case LifeStage.Butterfly:
                        if (flowers != null && flowers.Length > 0 && flowers[deliveries % flowers.Length] != null) return flowers[deliveries % flowers.Length];
                        break;
                }
                return deliveryTarget != null ? deliveryTarget : plant;
            }
        }

        [Header("Bushes (Butterfly stage)")]
        [SerializeField, Range(0.1f, 1f)] private float bushStartScale = 0.6f;
        private int deliveries;

        private LifeStage stage;
        private Vector2 butterflyHome;
        private Vector2 caterpillarHome;
        private Quaternion caterpillarHomeRot = Quaternion.identity;
        private Transform caterpillarHomeParent;
        private Vector2 caterpillarHomeAnchor;
        private Tween idleTween;
        private Vector3[] leafScales;

        private void Awake()
        {
            var t = transform;
            if (plant == null) plant = t.Find("Plant") as RectTransform;
            if (plantImage == null && plant != null) plantImage = plant.GetComponent<Image>();
            if (branch == null) branch = t.Find("Branch") as RectTransform;
            if (branchImage == null && branch != null) branchImage = branch.GetComponent<Image>();
            if (branchHolder == null) branchHolder = t.Find("BranchHolder") as RectTransform;
            if (caterpillarBranchSeat == null && branchHolder != null) caterpillarBranchSeat = branchHolder.Find("CaterpillarSeat") as RectTransform;
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
            if (branchLeaves != null)
            {
                leafScales = new Vector3[branchLeaves.Length];
                for (int i = 0; i < branchLeaves.Length; i++) leafScales[i] = branchLeaves[i] != null ? branchLeaves[i].localScale : Vector3.one;
            }
            if (caterpillar != null)
            {
                caterpillarHome = caterpillar.anchoredPosition; caterpillarHomeRot = caterpillar.localRotation;
                caterpillarHomeParent = caterpillar.parent; caterpillarHomeAnchor = caterpillar.anchorMin;
            }
        }

        /// <summary>Story caption: only shown while a stage transformation plays; empty hides the pill.</summary>
        public void SetCaption(string text)
        {
            if (caption == null) return;
            caption.text = text;
            var pill = caption.transform.parent as RectTransform;
            if (pill == null || pill == transform) return;
            bool show = !string.IsNullOrEmpty(text);
            if (show == pill.gameObject.activeSelf) { if (show) { pill.DOKill(true); pill.DOPunchScale(Vector3.one * 0.06f, 0.3f, 5, 0.5f); } return; }
            pill.DOKill();
            if (show) { pill.gameObject.SetActive(true); pill.localScale = new Vector3(0.6f, 0.6f, 1f); pill.DOScale(1f, 0.35f).SetEase(Ease.OutBack); }
            else pill.gameObject.SetActive(false);
        }

        /// <summary>Level start: creatures fly / crawl in from off screen, plants and props pop up.</summary>
        public void PlayIntro(float delay)
        {
            if (butterfly != null && butterfly.gameObject.activeSelf)
            {
                idleTween?.Kill();
                butterfly.DOKill();
                butterfly.anchoredPosition = butterflyHome + new Vector2(750f, 420f);
                butterfly.localRotation = Quaternion.Euler(0f, 0f, 18f);
                butterfly.DOAnchorPos(butterflyHome, 1.05f).SetDelay(delay).SetEase(Ease.OutSine).OnComplete(StartIdle);
                butterfly.DOLocalRotate(Vector3.zero, 1.05f).SetDelay(delay).SetEase(Ease.OutSine);
            }
            Pop(plant, delay + 0.1f);
            Pop(caterpillar, delay + 0.45f);
            Pop(branch, delay + 0.25f);
            Pop(chrysalis, delay + 0.4f);
            for (int i = 0; i < flowers.Length; i++) Pop(flowers[i], delay + 0.15f + i * 0.08f);
        }

        private static void Pop(RectTransform r, float delay)
        {
            if (r == null || !r.gameObject.activeSelf) return;
            r.DOKill(true);
            var target = r.localScale;
            r.localScale = Vector3.zero;
            r.DOScale(target, 0.5f).SetDelay(delay).SetEase(Ease.OutBack);
        }

        /// <summary>Static layout for a stage at a given goal progress (0..1).</summary>
        public void ShowStage(LifeStage newStage, float progress)
        {
            KillTweens();
            stage = newStage;
            deliveries = 0;
            FitBranch();
            // only what the stage is about: 1 plant (dry -> green), 2 caterpillar eating on the plant, 3 caterpillar waiting on the
            // growing branch, 4 two flowering bushes + butterfly. Egg, chrysalis and the mother / emerging butterfly appear only
            // inside the end-of-stage sequences.
            Show(plant, stage == LifeStage.Egg || stage == LifeStage.Caterpillar);
            Show(branch, stage == LifeStage.Chrysalis);
            Show(egg, false);
            Show(caterpillar, stage == LifeStage.Caterpillar || stage == LifeStage.Chrysalis);
            Show(chrysalis, false);
            Show(butterfly, stage == LifeStage.Butterfly);
            Show(sunGlow, false);
            if (flowers != null) foreach (var f in flowers) Show(f, stage == LifeStage.Butterfly);
            if (nightOverlay != null) nightOverlay.color = new Color(0.1f, 0.12f, 0.3f, 0f);
            if (butterfly != null) butterfly.anchoredPosition = butterflyHome;
            if (butterfly != null) butterfly.localScale = Vector3.one;
            if (chrysalis != null) chrysalis.localScale = Vector3.one;
            if (branch != null) branch.localScale = Vector3.one;
            SeatCaterpillar(stage == LifeStage.Chrysalis);
            if (flowers != null) foreach (var f in flowers) if (f != null) f.localRotation = Quaternion.identity;
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
                    if (plant != null) plant.localScale = Vector3.one;
                    if (plantImage != null) plantImage.color = healthyPlantColor;
                    if (caterpillar != null)
                    {
                        float cs = Mathf.Lerp(0.6f, caterpillarFullScale, p); // grows as it eats
                        if (caterpillar.gameObject.activeInHierarchy && cs > caterpillar.localScale.y + 0.001f)
                        {
                            idleTween?.Kill(); caterpillar.DOKill(true);
                            caterpillar.localScale = Vector3.one * caterpillar.localScale.y;
                            caterpillar.DOScale(cs, 0.5f).SetEase(Ease.OutBack).OnComplete(StartIdle);
                        }
                        else caterpillar.localScale = Vector3.one * cs;
                    }
                    break;
                case LifeStage.Chrysalis:
                    if (caterpillar != null && !DOTween.IsTweening(caterpillar)) caterpillar.localScale = Vector3.one * caterpillarFullScale;
                    SetBranchGrowth(p);
                    break;
                case LifeStage.Butterfly:
                    float bush = Mathf.Lerp(bushStartScale, 1f, p);
                    foreach (var f in flowers)
                    {
                        if (f == null) continue;
                        f.DOKill(true);
                        if (f.gameObject.activeInHierarchy && bush > f.localScale.x + 0.001f) f.DOScale(bush, 0.5f).SetEase(Ease.OutBack);
                        else f.localScale = Vector3.one * bush;
                    }
                    break;
            }
            if (stage != LifeStage.Egg) { SetPlantSprite(1f); SetLeaves(1f); }
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

        /// <summary>Branch grows from the stub to full length, then leaves pop out on the twig tips (see Branch/Ref_4.png).</summary>
        private void SetBranchGrowth(float p)
        {
            if (branchImage != null)
            {
                float fill = Mathf.Lerp(branchStubFill, 1f, Mathf.Clamp01(p / branchFullAt));
                branchImage.DOKill();
                if (branch != null && branch.gameObject.activeInHierarchy && fill > branchImage.fillAmount + 0.001f)
                    branchImage.DOFillAmount(fill, 0.6f).SetEase(Ease.OutCubic);
                else branchImage.fillAmount = fill;
            }
            SetBranchLeaves(p, true);
        }

        private void SetBranchLeaves(float p, bool animate)
        {
            if (branchLeaves == null) return;
            for (int i = 0; i < branchLeaves.Length; i++)
            {
                var leaf = branchLeaves[i];
                if (leaf == null) continue;
                float at = branchLeafThresholds != null && i < branchLeafThresholds.Length ? branchLeafThresholds[i] : (i + 1f) / (branchLeaves.Length + 1f);
                bool on = p >= at;
                if (on == leaf.gameObject.activeSelf) continue;
                leaf.DOKill();
                leaf.gameObject.SetActive(on);
                var target = leafScales != null && i < leafScales.Length ? leafScales[i] : Vector3.one; // mirrored leaves stay mirrored
                leaf.localScale = target;
                if (on && animate && branch != null && branch.gameObject.activeInHierarchy)
                {
                    leaf.localScale = Vector3.zero;
                    leaf.DOScale(target, 0.45f).SetDelay(0.3f).SetEase(Ease.OutBack);
                }
            }
        }

        /// <summary>Quick pop when a goal item lands in the garden.</summary>
        public void PlayDeliveryPop()
        {
            transform.DOKill(true);
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.04f, 0.5f, 5, 0.5f);
            RectTransform fed = stage == LifeStage.Egg ? plant
                : stage == LifeStage.Caterpillar ? caterpillar
                : stage == LifeStage.Chrysalis ? caterpillar
                : flowers != null && flowers.Length > 0 ? flowers[deliveries % flowers.Length] : null;
            deliveries++;
            if (fed != null && fed.gameObject.activeInHierarchy && stage == LifeStage.Egg)
            {
                fed.DOKill(true);
                fed.DOPunchScale(Vector3.one * 0.12f, 0.45f, 6, 0.6f);
            }
            else if (fed != null && fed.gameObject.activeInHierarchy)
            {
                // these also grow from SetProgress: punch on top of the growth tween without killing it
                fed.DOPunchRotation(new Vector3(0f, 0f, 6f), 0.45f, 6, 0.6f);
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
                        Show(butterfly, true);
                        butterfly.localScale = Vector3.one;
                        butterfly.anchoredPosition = butterflyHome + new Vector2(750f, 420f);
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
                    if (caterpillar != null) seq.Join(caterpillar.DOScale(0.6f, 0.6f).SetEase(Ease.OutBack));
                    seq.AppendInterval(0.8f);
                    break;

                case LifeStage.Caterpillar:
                    SetCaption("The caterpillar climbs to the branch...");
                    Show(branch, true);
                    SetBranchLeaves(0f, false);
                    if (branch != null) branch.localScale = Vector3.one;
                    if (branchImage != null) { branchImage.fillAmount = 0f; seq.Append(branchImage.DOFillAmount(branchStubFill, 1f).SetEase(Ease.OutCubic)); }
                    else if (branch != null) { branch.localScale = new Vector3(0f, 1f, 1f); seq.Append(branch.DOScaleX(1f, 0.6f).SetEase(Ease.OutBack)); }
                    // the big caterpillar climbs onto the stub and waits there; the chrysalis only forms once the branch is strong (Level 3)
                    if (caterpillar != null && caterpillarBranchSeat != null)
                    {
                        var seat = caterpillarBranchSeat;
                        // match the seat's world scale while still parented to the plant, so re-parenting doesn't jump
                        float k = caterpillar.parent != null ? seat.lossyScale.x / caterpillar.parent.lossyScale.x : 1f;
                        seq.Append(caterpillar.DOMove(seat.position, 1.4f).SetEase(Ease.InOutSine));
                        seq.Join(caterpillar.DORotateQuaternion(seat.rotation, 0.8f));
                        seq.Join(caterpillar.DOScale(caterpillarFullScale * k, 1.4f));
                        seq.AppendCallback(() => { SeatCaterpillar(true); caterpillar.localScale = Vector3.one * caterpillarFullScale; SetCaption("It waits on the branch. Let's make it strong!"); });
                        seq.Append(caterpillar.DOPunchScale(Vector3.one * 0.1f, 0.4f, 5, 0.5f));
                    }
                    seq.AppendInterval(1f);
                    break;

                case LifeStage.Chrysalis:
                    SetCaption("The caterpillar hangs beneath the strong branch...");
                    if (caterpillar != null && chrysalis != null)
                    {
                        // turn upside down and hang from the trunk where the chrysalis will form
                        seq.Append(caterpillar.DOMove(chrysalis.position, 1.1f).SetEase(Ease.InOutSine));
                        seq.Join(caterpillar.DOLocalRotate(new Vector3(0f, 0f, 180f), 1.1f));
                        seq.AppendCallback(() => { SetCaption("...and forms a chrysalis!"); Show(chrysalis, true); chrysalis.localScale = Vector3.zero; });
                        seq.Append(caterpillar.DOScale(0f, 0.5f).SetEase(Ease.InBack));
                        seq.Join(chrysalis.DOScale(1f, 0.7f).SetEase(Ease.OutBack));
                        seq.AppendCallback(() => Show(caterpillar, false));
                        seq.AppendInterval(0.5f);
                    }
                    // slow and readable: the chrysalis rests and grows over the days, wiggles, then the butterfly hatches
                    seq.AppendCallback(() => SetCaption("Days pass as the chrysalis rests and grows..."));
                    if (chrysalis != null)
                    {
                        seq.Append(DayNightCycle(2.4f));
                        seq.Join(chrysalis.DOScale(1.2f, 2.4f).SetEase(Ease.InOutSine));
                        seq.Append(DayNightCycle(2.4f));
                        seq.Join(chrysalis.DOScale(1.4f, 2.4f).SetEase(Ease.InOutSine));
                        seq.AppendCallback(() => SetCaption("Something is moving inside!"));
                        seq.Append(chrysalis.DOPunchRotation(new Vector3(0f, 0f, 10f), 1.2f, 6, 0.4f));
                        seq.Append(chrysalis.DOPunchRotation(new Vector3(0f, 0f, 14f), 1.2f, 8, 0.4f));
                        seq.AppendInterval(0.3f);
                    }
                    else seq.Append(DayNightCycle(3f));
                    seq.AppendCallback(() => { SetCaption("A butterfly emerges from the chrysalis!"); Show(butterfly, true); if (butterfly != null && chrysalis != null) { butterfly.position = chrysalis.TransformPoint(chrysalis.rect.center); butterfly.localScale = Vector3.zero; } });
                    if (chrysalis != null) seq.Append(chrysalis.DOScale(0f, 1f).SetEase(Ease.InBack));
                    if (butterfly != null)
                    {
                        seq.Join(butterfly.DOScale(1f, 1.4f).SetEase(Ease.OutBack));
                        seq.AppendInterval(0.6f);
                        seq.Append(butterfly.DOAnchorPos(butterflyHome, 2f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => Show(chrysalis, false));
                    seq.AppendInterval(1.5f); // let it land before the win panel
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
                            seq.Append(butterfly.DOMove(flower.TransformPoint(new Vector3(0f, flower.rect.height * 0.55f, 0f)), 0.7f).SetEase(Ease.InOutSine));
                            seq.Append(butterfly.DOPunchScale(Vector3.one * 0.15f, 0.35f, 4, 0.5f));
                        }
                        seq.Append(butterfly.DOAnchorPos(butterflyHome + new Vector2(0f, 120f), 1f).SetEase(Ease.InOutSine));
                        seq.Append(butterfly.DOAnchorPos(butterflyHome, 0.8f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendInterval(0.5f);
                    break;
            }
            seq.OnComplete(() => { SetCaption(""); StartIdle(); done?.Invoke(); });
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
            if (flowers != null) foreach (var f in flowers) if (f != null) f.DOKill();
            if (butterfly != null) butterfly.localRotation = Quaternion.identity;
            if (nightOverlay != null) nightOverlay.DOKill();
            if (branchImage != null) branchImage.DOKill();
        }

        /// <summary>Puts the caterpillar on the plant's big leaf (Caterpillar stage) or on top of the branch stub (Chrysalis stage).</summary>
        private void SeatCaterpillar(bool onBranch)
        {
            if (caterpillar == null) return;
            caterpillar.DOKill();
            if (onBranch && caterpillarBranchSeat != null)
            {
                caterpillar.SetParent(caterpillarBranchSeat, false);
                caterpillar.anchorMin = caterpillar.anchorMax = new Vector2(0.5f, 0.5f);
                caterpillar.anchoredPosition = Vector2.zero;
                caterpillar.localRotation = Quaternion.identity;
            }
            else if (caterpillarHomeParent != null)
            {
                caterpillar.SetParent(caterpillarHomeParent, false);
                caterpillar.anchorMin = caterpillar.anchorMax = caterpillarHomeAnchor;
                caterpillar.anchoredPosition = caterpillarHome;
                caterpillar.localRotation = caterpillarHomeRot; // tilted to sit on the leaf
            }
        }

        /// <summary>Scales BranchHolder so the fully grown branch and its leaves end inside the garden's right edge.</summary>
        private void FitBranch()
        {
            if (branchHolder == null || branch == null) return;
            var garden = (RectTransform)transform;
            float right = 0f;
            var corners = new Vector3[4];
            void Grow(RectTransform r)
            {
                if (r == null) return;
                r.GetWorldCorners(corners);
                foreach (var c in corners) right = Mathf.Max(right, branchHolder.InverseTransformPoint(c).x);
            }
            // leaves may be mid-pop (scale 0): measure them at their resting scale
            Grow(branch);
            if (branchLeaves != null)
                for (int i = 0; i < branchLeaves.Length; i++)
                {
                    var leaf = branchLeaves[i];
                    if (leaf == null) continue;
                    var keep = leaf.localScale;
                    leaf.localScale = leafScales != null && i < leafScales.Length ? leafScales[i] : Vector3.one;
                    Grow(leaf);
                    leaf.localScale = keep;
                }
            float available = garden.rect.width - branchEdgeMargin - branchHolder.anchoredPosition.x;
            float k = right > 1f ? Mathf.Min(1f, available / right) : 1f;
            branchHolder.localScale = new Vector3(k, k, 1f);
        }

        private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) FitBranch(); }

        private static void Show(RectTransform r, bool on) { if (r != null) r.gameObject.SetActive(on); }

        private void OnDisable() => KillTweens();
    }
}
