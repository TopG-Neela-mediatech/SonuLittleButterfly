using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Keeps the full-screen BG art covering the screen (like AspectRatioFitter.EnvelopeParent) but slides it
    /// vertically so the art's lawn line stays under the garden area on every aspect ratio.
    /// Without this, wide screens (iPads, 3:4) scale the art by width and the lawn drops below the garden props.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BackgroundAligner : MonoBehaviour
    {
        [SerializeField] private RectTransform gardenView;
        [Tooltip("Width / height of the art (BG_1 is 1080x1920).")]
        [SerializeField] private float artAspect = 1080f / 1920f;
        [Tooltip("Point of the art (0 = top, 1 = bottom) that must line up with the bottom of the garden area.")]
        [SerializeField, Range(0f, 1f)] private float artAnchorFromTop = 0.4f;

        private RectTransform rt;
        private Vector2 lastParentSize;
        private float lastGardenBottom = float.NaN;

        private void Awake() => rt = (RectTransform)transform;

        private void LateUpdate()
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Vector2 p = parent.rect.size;
            float gardenTop = GardenBottomFromTop(parent, p);
            if (p == lastParentSize && Mathf.Approximately(gardenTop, lastGardenBottom)) return;
            lastParentSize = p; lastGardenBottom = gardenTop;

            float h = Mathf.Max(p.y, p.x / artAspect);
            float w = h * artAspect;
            // move the art up (positive) until its anchor line meets the garden bottom, never uncovering the screen
            float up = float.IsNaN(gardenTop) ? 0f : Mathf.Clamp(artAnchorFromTop * h - gardenTop, 0f, h - p.y);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, up);
        }

        /// <summary>Distance from the parent's top edge to the garden's bottom edge, in parent units.</summary>
        private float GardenBottomFromTop(RectTransform parent, Vector2 size)
        {
            if (gardenView == null) return float.NaN;
            var c = new Vector3[4];
            gardenView.GetWorldCorners(c);
            Vector3 local = parent.InverseTransformPoint(c[0]); // bottom-left
            float topY = (1f - parent.pivot.y) * size.y;
            return topY - local.y;
        }
    }
}
