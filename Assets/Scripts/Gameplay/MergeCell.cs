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

        public void OnPointerClick(PointerEventData eventData) => board?.OnCellTapped(this);
    }
}
