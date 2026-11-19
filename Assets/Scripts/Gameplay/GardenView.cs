using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// The garden at the top of the screen. Everything happens on one branch growing out of the tree (TreeImage):
    ///   1 Egg         - the branch's two leaves are watered from dry to green; the butterfly lays an egg on the big leaf.
    ///   2 Caterpillar - the caterpillar eats on that leaf and grows, then crawls onto the branch.
    ///   3 Chrysalis   - the branch grows longer and sprouts leaves while the caterpillar waits; then chrysalis, days, butterfly.
    ///   4 Butterfly   - water grows the flowering bushes; the butterfly drinks nectar.
    /// Camera: a level starts zoomed out and zooms in; the level end zooms back out.
    ///   Levels 1-3: the tree is anchored to the garden's bottom-left and its scene layout IS the zoomed-in framing (trunk at the
    ///   left edge, branch across). Zooming out slides and scales it until the whole tree stands on the ground.
    ///   Level 4: the tree is hidden; the bush holder (anchored bottom-centre) scales up from small to its scene size.
    /// </summary>
    public class GardenView : MonoBehaviour
    {
        [Header("Props")]
        [Tooltip("Tree image: parent of the branch holder. Anchored and pivoted bottom-left; its scene position/scale is the zoomed-in view.")]
        [SerializeField] private RectTransform tree;
        [SerializeField] private RectTransform branch;
        [SerializeField] private RectTransform egg;
        [SerializeField] private RectTransform caterpillar;
        [SerializeField] private RectTransform chrysalis;
        [SerializeField] private RectTransform butterfly;
        [SerializeField] private RectTransform[] flowers;
        [SerializeField] private Image nightOverlay;
        [SerializeField] private RectTransform deliveryTarget;
        [SerializeField] private TextMeshProUGUI caption;

        [Header("Branch")]
        [Tooltip("Filled (horizontal, origin left) image: the branch grows out of the trunk.")]
        [SerializeField] private Image branchImage;
        [Tooltip("Child of the tree: the branch, its leaves, the caterpillar seat and the chrysalis.")]
        [SerializeField] private RectTransform branchHolder;
        [Tooltip("Spot on top of the branch where the fed caterpillar waits during the Chrysalis stage.")]
        [SerializeField] private RectTransform caterpillarBranchSeat;
        [SerializeField] private float caterpillarFullScale = 1.1f;
        [Tooltip("Branch length (fill) in stages 1-2; it must reach the caterpillar seat and the chrysalis.")]
        [SerializeField, Range(0f, 1f)] private float branchStubFill = 0.65f;
        [Tooltip("Chrysalis-stage progress at which the branch reaches its full length.")]
        [SerializeField, Range(0.05f, 1f)] private float branchFullAt = 0.6f;
        [Tooltip("Leaves on the twig tips.")]
        [SerializeField] private RectTransform[] branchLeaves;
        [Tooltip("Chrysalis-stage progress at which each leaf appears. Negative = on the branch from the start (watered in stage 1).")]
        [SerializeField] private float[] branchLeafThresholds;
        [Tooltip("Index in Branch Leaves of the big leaf the egg is laid on and the caterpillar eats on.")]
        [SerializeField] private int eggLeafIndex = 1;
        [SerializeField] private Sprite dryLeafSprite;
        [SerializeField] private Sprite freshLeafSprite;
        [SerializeField, Range(0.1f, 1f)] private float dryLeafScale = 0.7f;

        [Header("Zoom")]
        [Tooltip("Garden space kept free at the top for the level pill and round dots.")]
        [SerializeField] private float topReserve = 210f;
        [SerializeField] private float sidePadding = 20f;
        [Tooltip("Level 4: parent of the two bushes, anchored bottom-centre; its scene scale is the zoomed-in view.")]
        [SerializeField] private RectTransform bushHolder;
        [SerializeField, Range(0.1f, 1f)] private float bushZoomOutScale = 0.55f;
        [Tooltip("Extra tree-local margin around the branch when measuring the whole-tree view.")]
        [SerializeField] private float branchFocusMargin = 20f;
        [SerializeField] private float zoomInDuration = 1.6f;
        [SerializeField] private float zoomOutDuration = 1.4f;

        [Header("Bushes (Butterfly stage)")]
        [SerializeField, Range(0.1f, 1f)] private float bushStartScale = 0.7f;

        /// <summary>Where goal items fly to: the thing this stage is caring for.</summary>
        public RectTransform DeliveryTarget
        {
            get
            {
                switch (stage)
                {
                    case LifeStage.Egg: if (egg != null) return egg; break; // top of the big leaf
                    case LifeStage.Caterpillar:
                    case LifeStage.Chrysalis: if (caterpillar != null) return caterpillar; break;
                    case LifeStage.Butterfly:
                        if (flowers != null && flowers.Length > 0 && flowers[deliveries % flowers.Length] != null) return flowers[deliveries % flowers.Length];
                        break;
                }
                return deliveryTarget;
            }
        }

        private int deliveries;
        private LifeStage stage;
        private Vector2 butterflyHome;
        private Vector2 caterpillarHome;
        private Quaternion caterpillarHomeRot = Quaternion.identity;
        private Transform caterpillarHomeParent;
        private Vector2 caterpillarHomeAnchor;
        private Tween idleTween;
        private Vector3[] leafScales;
        private Image[] leafImages;

        private float zoomT = 1f; // 0 = zoomed out, 1 = zoomed in (the scene layout)
        private Vector2 treeInPos, bushInPos;
        private Vector3 treeInScale = Vector3.one, bushInScale = Vector3.one;
        private Tween zoomTween;
        private Rect branchFocus;  // tree-local
        private bool focusReady;

        private RectTransform BigLeaf => branchLeaves != null && eggLeafIndex >= 0 && eggLeafIndex < branchLeaves.Length ? branchLeaves[eggLeafIndex] : null;

        private void Awake()
        {
            var t = transform;
            if (tree == null) tree = t.Find("TreeImage") as RectTransform;
            if (branchHolder == null && tree != null) branchHolder = tree.Find("BranchHolder") as RectTransform;
            if (branch == null && branchHolder != null) branch = branchHolder.Find("Branch") as RectTransform;
            if (branchImage == null && branch != null) branchImage = branch.GetComponent<Image>();
            if (caterpillarBranchSeat == null && branchHolder != null) caterpillarBranchSeat = branchHolder.Find("CaterpillarSeat") as RectTransform;
            if (chrysalis == null && branchHolder != null) chrysalis = branchHolder.Find("Chrysalis") as RectTransform;
            if (butterfly == null) butterfly = t.Find("Butterfly") as RectTransform;
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
            if (tree != null) { treeInPos = tree.anchoredPosition; treeInScale = tree.localScale; }
            if (bushHolder != null) { bushInPos = bushHolder.anchoredPosition; bushInScale = bushHolder.localScale; }
            if (branchLeaves != null)
            {
                leafScales = new Vector3[branchLeaves.Length];
                leafImages = new Image[branchLeaves.Length];
                for (int i = 0; i < branchLeaves.Length; i++)
                {
                    leafScales[i] = branchLeaves[i] != null ? branchLeaves[i].localScale : Vector3.one;
                    leafImages[i] = branchLeaves[i] != null ? branchLeaves[i].GetComponent<Image>() : null;
                }
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

        /// <summary>Main level start: show the whole tree, then zoom in to the branch; the butterfly flies in, props pop.</summary>
        public void PlayIntro(float delay)
        {
            SetZoomInstant(0f);
            ZoomTo(1f, zoomInDuration, delay + 0.5f);
            float propDelay = delay + 0.5f + zoomInDuration * 0.8f;
            if (butterfly != null && butterfly.gameObject.activeSelf)
            {
                idleTween?.Kill();
                butterfly.DOKill();
                butterfly.anchoredPosition = butterflyHome + new Vector2(750f, 420f);
                butterfly.localRotation = Quaternion.Euler(0f, 0f, 18f);
                butterfly.DOAnchorPos(butterflyHome, 1.05f).SetDelay(propDelay).SetEase(Ease.OutSine).OnComplete(StartIdle);
                butterfly.DOLocalRotate(Vector3.zero, 1.05f).SetDelay(propDelay).SetEase(Ease.OutSine);
            }
            Pop(caterpillar, propDelay);
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
            if (zoomTween == null || !zoomTween.IsActive()) SetZoomInstant(1f); // rounds inside a level stay zoomed in
            // Egg, chrysalis and the mother / emerging butterfly appear only inside the end-of-stage sequences.
            Show(tree, stage != LifeStage.Butterfly); // nectar level: just the bushes
            if (bushHolder != null) bushHolder.gameObject.SetActive(stage == LifeStage.Butterfly);
            Show(branch, true);
            Show(egg, false);
            Show(caterpillar, stage == LifeStage.Caterpillar || stage == LifeStage.Chrysalis);
            Show(chrysalis, false);
            Show(butterfly, stage == LifeStage.Butterfly);
            if (flowers != null) foreach (var f in flowers) Show(f, stage == LifeStage.Butterfly);
            if (nightOverlay != null) nightOverlay.color = new Color(0.1f, 0.12f, 0.3f, 0f);
            if (butterfly != null) { butterfly.anchoredPosition = butterflyHome; butterfly.localScale = Vector3.one; }
            if (chrysalis != null) { chrysalis.localScale = Vector3.one; chrysalis.localRotation = Quaternion.identity; }
            if (branch != null) branch.localScale = Vector3.one;
            if (egg != null) egg.localScale = Vector3.one;
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
                    SetBranchFill(branchStubFill);
                    SetBranchLeaves(0f, true);
                    SetLeafHealth(p);
                    break;
                case LifeStage.Caterpillar:
                    SetBranchFill(branchStubFill);
                    SetBranchLeaves(0f, true);
                    SetLeafHealth(1f);
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
                    SetBranchFill(Mathf.Lerp(branchStubFill, 1f, Mathf.Clamp01(p / branchFullAt)));
                    SetBranchLeaves(p, true);
                    SetLeafHealth(1f);
                    break;
                case LifeStage.Butterfly:
                    SetBranchFill(1f);
                    SetBranchLeaves(1f, false);
                    SetLeafHealth(1f);
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
        }

        // ---------------------------------------------------------------- branch
        private void SetBranchFill(float fill)
        {
            if (branchImage == null) return;
            branchImage.DOKill();
            if (branch != null && branch.gameObject.activeInHierarchy && fill > branchImage.fillAmount + 0.001f)
                branchImage.DOFillAmount(fill, 0.6f).SetEase(Ease.OutCubic);
            else branchImage.fillAmount = fill;
        }

        private float LeafThreshold(int i) =>
            branchLeafThresholds != null && i < branchLeafThresholds.Length ? branchLeafThresholds[i] : (i + 1f) / (branchLeaves.Length + 1f);

        /// <summary>Leaves with a negative threshold are always there; the rest pop out as the Chrysalis-stage branch grows.</summary>
        private void SetBranchLeaves(float p, bool animate)
        {
            if (branchLeaves == null) return;
            for (int i = 0; i < branchLeaves.Length; i++)
            {
                var leaf = branchLeaves[i];
                if (leaf == null) continue;
                bool on = LeafThreshold(i) < 0f || p >= LeafThreshold(i);
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

        /// <summary>Stage 1: the branch's starting leaves go from small and dry to full and green as they are watered.</summary>
        private void SetLeafHealth(float p)
        {
            if (branchLeaves == null) return;
            int n = 0;
            for (int i = 0; i < branchLeaves.Length; i++) if (LeafThreshold(i) < 0f) n++;
            int k = 0;
            for (int i = 0; i < branchLeaves.Length; i++)
            {
                if (LeafThreshold(i) >= 0f) continue;
                var leaf = branchLeaves[i];
                var img = leafImages != null && i < leafImages.Length ? leafImages[i] : null;
                k++;
                if (leaf == null) continue;
                bool fresh = p >= k / (n + 1f);
                var sp = fresh ? freshLeafSprite : dryLeafSprite;
                if (img != null && sp != null && img.sprite != sp)
                {
                    bool turningGreen = fresh && img.sprite == dryLeafSprite;
                    img.sprite = sp;
                    if (turningGreen && leaf.gameObject.activeInHierarchy) leaf.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.45f, 6, 0.6f);
                }
                var rest = leafScales != null && i < leafScales.Length ? leafScales[i] : Vector3.one;
                var s = rest * Mathf.Lerp(dryLeafScale, 1f, p);
                s.z = 1f;
                if (!DOTween.IsTweening(leaf)) leaf.localScale = s;
            }
        }

        // ---------------------------------------------------------------- zoom
        /// <summary>Tree-local rect of the fully grown branch and its leaves (measured at rest, once).</summary>
        private void EnsureFocus()
        {
            if (focusReady || tree == null || branchHolder == null) return;
            var corners = new Vector3[4];
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            void Grow(RectTransform r)
            {
                if (r == null) return;
                r.GetWorldCorners(corners);
                foreach (var c in corners)
                {
                    var l = tree.InverseTransformPoint(c);
                    xMin = Mathf.Min(xMin, l.x); xMax = Mathf.Max(xMax, l.x); yMin = Mathf.Min(yMin, l.y); yMax = Mathf.Max(yMax, l.y);
                }
            }
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
            if (xMin > xMax) return;
            // tree-local coordinates are measured from the pivot (bottom-left)
            branchFocus = Rect.MinMaxRect(xMin - branchFocusMargin, yMin - branchFocusMargin, xMax + branchFocusMargin, yMax + branchFocusMargin);
            focusReady = true;
        }

        /// <summary>Slides and scales the anchored tree (or, in level 4, the bush holder) between zoomed out (0) and its scene layout (1).</summary>
        private void ApplyZoom()
        {
            float t = Mathf.SmoothStep(0f, 1f, zoomT);
            if (stage == LifeStage.Butterfly)
            {
                if (bushHolder == null) return;
                // scale about the bottom-centre anchor: the bushes stay on the ground and grow toward the viewer
                bushHolder.localScale = Vector3.Lerp(bushInScale * bushZoomOutScale, bushInScale, t);
                bushHolder.anchoredPosition = bushInPos;
                return;
            }
            if (tree == null) return;
            ZoomedOutTree(out var outScale, out var outPos);
            // scale on a log curve so the zoom speed feels even; position follows the same curve
            float k = Mathf.Exp(Mathf.Lerp(Mathf.Log(outScale), Mathf.Log(treeInScale.x), t));
            float f = Mathf.Abs(treeInScale.x - outScale) > 1e-4f ? (k - outScale) / (treeInScale.x - outScale) : t;
            tree.localScale = new Vector3(k, k, 1f);
            tree.anchoredPosition = Vector2.Lerp(outPos, treeInPos, f);
        }

        /// <summary>The whole tree (plus the branch reaching past its right edge) standing on the garden's ground, centred.</summary>
        private void ZoomedOutTree(out float scale, out Vector2 pos)
        {
            EnsureFocus();
            var g = ((RectTransform)transform).rect;
            var treeRect = new Rect(0f, 0f, tree.rect.width, tree.rect.height);
            var focus = focusReady ? Rect.MinMaxRect(Mathf.Min(0f, branchFocus.xMin), 0f, Mathf.Max(treeRect.xMax, branchFocus.xMax), treeRect.yMax) : treeRect;
            var target = new Rect(sidePadding, 0f, g.width - 2f * sidePadding, g.height - topReserve * 0.8f);
            scale = Mathf.Min(target.width / Mathf.Max(1f, focus.width), target.height / Mathf.Max(1f, focus.height));
            // tree anchored/pivoted at the garden's bottom-left: centre it horizontally, roots on the ground
            pos = new Vector2(target.center.x - focus.center.x * scale, target.yMin - focus.yMin * scale);
        }

        private void SetZoomInstant(float t)
        {
            zoomTween?.Kill();
            zoomT = t;
            ApplyZoom();
        }

        private Tween ZoomTo(float t, float duration, float delay = 0f)
        {
            zoomTween?.Kill();
            zoomTween = DOTween.To(() => zoomT, v => { zoomT = v; ApplyZoom(); }, t, duration).SetDelay(delay).SetEase(Ease.Linear);
            return zoomTween;
        }

        private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) ApplyZoom(); }

        /// <summary>Quick pop when a goal item lands in the garden.</summary>
        public void PlayDeliveryPop()
        {
            RectTransform fed = stage == LifeStage.Egg ? BigLeaf
                : stage == LifeStage.Caterpillar || stage == LifeStage.Chrysalis ? caterpillar
                : flowers != null && flowers.Length > 0 ? flowers[deliveries % flowers.Length] : null;
            deliveries++;
            // a wiggle doesn't fight the growth tweens that SetProgress starts on the same object
            if (fed != null && fed.gameObject.activeInHierarchy) fed.DOPunchRotation(new Vector3(0f, 0f, 6f), 0.45f, 6, 0.6f);
        }

        /// <summary>Stage gratification animation (on the branch), then the camera zooms out to the whole tree. Calls done when finished.</summary>
        public void PlayStageTransform(Action done)
        {
            KillTweens();
            var seq = DOTween.Sequence();
            switch (stage)
            {
                case LifeStage.Egg:
                    // mother butterfly lands on the big leaf, lays an egg, time passes, caterpillar hatches
                    Show(egg, false); Show(caterpillar, false);
                    if (butterfly != null && egg != null)
                    {
                        Show(butterfly, true);
                        butterfly.localScale = Vector3.one;
                        butterfly.anchoredPosition = butterflyHome + new Vector2(750f, 420f);
                        Vector3 landing = egg.TransformPoint(new Vector3(0f, egg.rect.height * 0.35f, 0f));
                        seq.Append(butterfly.DOMove(landing, 1.2f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => { Show(egg, true); if (egg != null) { egg.localScale = Vector3.zero; } SetCaption("The butterfly lays an egg on the healthy leaf!"); Voice(SoundManager.GardenMoment.EggLaid); });
                    if (egg != null) seq.Append(egg.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
                    if (butterfly != null) seq.Append(butterfly.DOAnchorPos(butterflyHome + new Vector2(260f, 160f), 1.4f).SetEase(Ease.InSine));
                    seq.AppendCallback(() => Show(butterfly, false));
                    seq.Append(DayNightCycle(1.6f));
                    seq.AppendCallback(() => { SetCaption("The egg hatches into a caterpillar!"); Voice(SoundManager.GardenMoment.EggHatched); SeatCaterpillar(false); Show(caterpillar, true); if (caterpillar != null) caterpillar.localScale = Vector3.zero; });
                    if (egg != null) seq.Append(egg.DOScale(0f, 0.4f).SetEase(Ease.InBack));
                    if (caterpillar != null) seq.Join(caterpillar.DOScale(0.6f, 0.6f).SetEase(Ease.OutBack));
                    seq.AppendCallback(() => Show(egg, false));
                    seq.AppendInterval(0.8f);
                    break;

                case LifeStage.Caterpillar:
                    // the big caterpillar crawls off its leaf onto the branch and waits there; the chrysalis only forms once the branch is strong (Level 3)
                    SetCaption("The caterpillar crawls onto the branch...");
                    Voice(SoundManager.GardenMoment.CaterpillarToBranch); // one line covers this caption and the next
                    if (caterpillar != null && caterpillarBranchSeat != null)
                    {
                        var seat = caterpillarBranchSeat;
                        // match the seat's world scale while still parented to the leaf, so re-parenting doesn't jump
                        float k = caterpillar.parent != null ? seat.lossyScale.x / caterpillar.parent.lossyScale.x : 1f;
                        seq.Append(caterpillar.DOMove(seat.position, 1.6f).SetEase(Ease.InOutSine));
                        seq.Join(caterpillar.DORotateQuaternion(seat.rotation, 0.9f));
                        seq.Join(caterpillar.DOScale(caterpillarFullScale * k, 1.6f));
                        seq.AppendCallback(() => { SeatCaterpillar(true); caterpillar.localScale = Vector3.one * caterpillarFullScale; SetCaption("It waits on the branch. Let's make it strong!"); });
                        seq.Append(caterpillar.DOPunchScale(Vector3.one * 0.1f, 0.4f, 5, 0.5f));
                    }
                    seq.AppendInterval(1f);
                    break;

                case LifeStage.Chrysalis:
                    SetCaption("The caterpillar hangs beneath the strong branch...");
                    Voice(SoundManager.GardenMoment.ChrysalisForms); // one line covers "...and forms a chrysalis!" too
                    if (caterpillar != null && chrysalis != null)
                    {
                        // turn upside down and hang from the branch where the chrysalis will form
                        seq.Append(caterpillar.DOMove(chrysalis.position, 1.1f).SetEase(Ease.InOutSine));
                        seq.Join(caterpillar.DOLocalRotate(new Vector3(0f, 0f, 180f), 1.1f));
                        seq.AppendCallback(() => { SetCaption("...and forms a chrysalis!"); Show(chrysalis, true); chrysalis.localScale = Vector3.zero; });
                        seq.Append(caterpillar.DOScale(0f, 0.5f).SetEase(Ease.InBack));
                        seq.Join(chrysalis.DOScale(1f, 0.7f).SetEase(Ease.OutBack));
                        seq.AppendCallback(() => Show(caterpillar, false));
                        seq.AppendInterval(0.5f);
                    }
                    // slow and readable: the chrysalis rests and grows over the days, wiggles, then the butterfly hatches
                    seq.AppendCallback(() => { SetCaption("Days pass as the chrysalis rests and grows..."); Voice(SoundManager.GardenMoment.ChrysalisRests); });
                    if (chrysalis != null)
                    {
                        seq.Append(DayNightCycle(2.4f));
                        seq.Join(chrysalis.DOScale(1.2f, 2.4f).SetEase(Ease.InOutSine));
                        seq.Append(DayNightCycle(2.4f));
                        seq.Join(chrysalis.DOScale(1.4f, 2.4f).SetEase(Ease.InOutSine));
                        seq.AppendCallback(() => { SetCaption("Something is moving inside!"); Voice(SoundManager.GardenMoment.ChrysalisMoving); });
                        seq.Append(chrysalis.DOPunchRotation(new Vector3(0f, 0f, 10f), 1.2f, 6, 0.4f));
                        seq.Append(chrysalis.DOPunchRotation(new Vector3(0f, 0f, 14f), 1.2f, 8, 0.4f));
                        seq.AppendInterval(0.3f);
                    }
                    else seq.Append(DayNightCycle(3f));
                    seq.AppendCallback(() => { SetCaption("A butterfly emerges from the chrysalis!"); Voice(SoundManager.GardenMoment.ButterflyEmerges); Show(butterfly, true); if (butterfly != null && chrysalis != null) { butterfly.position = chrysalis.TransformPoint(chrysalis.rect.center); butterfly.localScale = Vector3.zero; } });
                    if (chrysalis != null) seq.Append(chrysalis.DOScale(0f, 1f).SetEase(Ease.InBack));
                    if (butterfly != null)
                    {
                        seq.Join(butterfly.DOScale(1f, 1.4f).SetEase(Ease.OutBack));
                        seq.AppendInterval(0.6f);
                        seq.Append(butterfly.DOAnchorPos(butterflyHome, 2f).SetEase(Ease.InOutSine));
                    }
                    seq.AppendCallback(() => Show(chrysalis, false));
                    seq.AppendInterval(1f);
                    break;

                case LifeStage.Butterfly:
                    SetCaption("The butterfly drinks nectar and flies!");
                    Voice(SoundManager.GardenMoment.ButterflyNectar);
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
            // level end: zoom out to the whole tree before the win panel; the next level zooms back in (PlayIntro)
            seq.AppendCallback(() => SetCaption(""));
            seq.Append(ZoomTo(0f, zoomOutDuration));
            seq.AppendInterval(0.8f);
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
            foreach (var r in new[] { branch, egg, caterpillar, chrysalis, butterfly }) if (r != null) r.DOKill();
            if (flowers != null) foreach (var f in flowers) if (f != null) f.DOKill();
            if (branchLeaves != null) foreach (var l in branchLeaves) if (l != null) l.DOKill(true); // finish wiggles / pops in place
            if (butterfly != null) butterfly.localRotation = Quaternion.identity;
            if (nightOverlay != null) nightOverlay.DOKill();
            if (branchImage != null) branchImage.DOKill();
        }

        /// <summary>Puts the caterpillar on the big leaf (Caterpillar stage) or on top of the branch (Chrysalis stage).</summary>
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
                caterpillar.localRotation = caterpillarHomeRot; // sits on the leaf's top edge
            }
        }

        private static void Show(RectTransform r, bool on) { if (r != null) r.gameObject.SetActive(on); }

        private static void Voice(SoundManager.GardenMoment moment) => GameManager.Instance?.SoundManager?.PlayGardenMoment(moment);

        private void OnDisable() { KillTweens(); zoomTween?.Kill(); }
    }
}
