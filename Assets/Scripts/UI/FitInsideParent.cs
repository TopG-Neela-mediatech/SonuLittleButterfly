using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Uniformly scales a fixed-size panel down (never up) so it fits inside its parent with a margin.
    /// Keeps authored popups/modals intact on narrow phones and foldables.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FitInsideParent : MonoBehaviour
    {
        [SerializeField] private float margin = 40f;

        private RectTransform rt;
        private Vector2 lastParent, lastSize;

        private void Awake() => rt = (RectTransform)transform;
        private void OnEnable() { lastParent = Vector2.zero; LateUpdate(); }

        private void LateUpdate()
        {
            if (rt == null) rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Vector2 p = parent.rect.size, s = rt.rect.size;
            if (p == lastParent && s == lastSize) return;
            lastParent = p; lastSize = s;
            if (s.x <= 0f || s.y <= 0f) return;
            float k = Mathf.Min(1f, (p.x - 2f * margin) / s.x, (p.y - 2f * margin) / s.y);
            rt.localScale = new Vector3(k, k, 1f);
        }
    }
}
