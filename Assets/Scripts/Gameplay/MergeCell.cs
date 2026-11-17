using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>A board cell. Tapping an empty cell moves the currently selected item here.</summary>
    public class MergeCell : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Outline outline;

        public int Index { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        private MergeBoard board;

        public void Bind(MergeBoard owner, int index)
        {
            board = owner;
            Index = index;
            if (background == null) background = GetComponent<Image>();
            if (outline == null) outline = GetComponent<Outline>();
            SetHighlight(false);
        }

        public void SetHighlight(bool on)
        {
            if (outline != null) outline.enabled = on;
        }

        /// <summary>Quick colour flash (wrong move feedback).</summary>
        public void Flash(Color color)
        {
            if (background == null) return;
            if (!hasBaseColor) { baseColor = background.color; hasBaseColor = true; }
            background.DOKill();
            background.color = baseColor;
            background.DOColor(color, 0.12f).SetLoops(2, LoopType.Yoyo).OnComplete(() => background.color = baseColor);
        }
        private Color baseColor;
        private bool hasBaseColor;

        private void OnDestroy() { if (background != null) background.DOKill(); }

        public void OnPointerClick(PointerEventData eventData) => board?.OnCellTapped(this);
    }
}
