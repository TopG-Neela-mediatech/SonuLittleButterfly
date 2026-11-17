using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>One merge item on the board. Forwards pointer events to the MergeBoard that owns it.</summary>
    public class MergeItemView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI tierLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        public int CellIndex { get; private set; }
        public int Tier { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        private MergeBoard board;
        private bool dragging;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (background == null) background = GetComponent<Image>();
            if (icon == null) icon = transform.Find("Icon")?.GetComponent<Image>();
            if (nameLabel == null) nameLabel = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (tierLabel == null) tierLabel = transform.Find("Tier")?.GetComponent<TextMeshProUGUI>();
        }

        public void Bind(MergeBoard owner, int cellIndex, int tier, ThemeData theme, Sprite fallbackSprite)
        {
            board = owner;
            CellIndex = cellIndex;
            Tier = tier;
            Apply(tier, theme, fallbackSprite);
        }

        /// <summary>Visual only (used for goal icons and the flying copy).</summary>
        public void Apply(int tier, ThemeData theme, Sprite fallbackSprite)
        {
            Tier = tier;
            Sprite sprite = null;
            if (theme != null && theme.tierSprites != null && tier - 1 < theme.tierSprites.Length) sprite = theme.tierSprites[tier - 1];
            bool placeholder = sprite == null;
            if (icon != null)
            {
                icon.sprite = placeholder ? fallbackSprite : sprite;
                icon.color = placeholder && theme != null ? theme.tint : Color.white;
                icon.preserveAspect = true;
                // Tier-based size so every tier reads differently even with placeholder art.
                if (isSelected) { selectTween?.Kill(); selectTween = null; isSelected = false; }
                icon.rectTransform.localScale = Vector3.one * IconScale(tier);
            }
            if (nameLabel != null)
            {
                string n = theme != null && theme.tierNames != null && tier - 1 < theme.tierNames.Length ? theme.tierNames[tier - 1] : "Tier " + tier;
                nameLabel.text = n;
            }
            if (tierLabel != null) tierLabel.text = tier.ToString();
        }

        private bool isSelected;
        private Tween selectTween;

        /// <summary>Selected items gently bob (scale pulse on the icon) instead of changing colour.</summary>
        public void SetSelected(bool selected)
        {
            if (selected == isSelected) return;
            isSelected = selected;
            selectTween?.Kill();
            selectTween = null;
            if (icon == null) return;
            var baseScale = Vector3.one * IconScale(Tier);
            icon.rectTransform.localScale = baseScale;
            if (selected)
                selectTween = icon.rectTransform.DOScale(baseScale * 1.14f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private static float IconScale(int tier) => 0.66f + 0.11f * (tier - 1);

        private void OnDisable() { selectTween?.Kill(); selectTween = null; isSelected = false; }

        public void SetRaycast(bool on)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = on;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = false;
            board?.OnItemPointerDown(this, eventData);
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            board?.OnItemBeginDrag(this, eventData);
        }
        public void OnDrag(PointerEventData eventData) => board?.OnItemDrag(this, eventData);
        public void OnEndDrag(PointerEventData eventData)
        {
            board?.OnItemRelease(this, eventData, true);
            dragging = false;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging) board?.OnItemRelease(this, eventData, false);
        }
    }
}
