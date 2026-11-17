using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Keeps UI clear of notches, punch holes and the home indicator (Screen.safeArea).
    /// FitRect: stretches this RectTransform to the safe area (use on a full-screen content root).
    /// PushIn: keeps the authored anchoredPosition and moves it inward by the inset on the edge(s) it is anchored to
    /// (use on a corner button that must stay under a full-screen background parent).
    /// Re-applies whenever the resolution, orientation or safe area changes.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        public enum Mode { FitRect, PushIn }

        [SerializeField] private Mode mode = Mode.FitRect;
        [Tooltip("PushIn only: the position authored for a device without a notch.")]
        [SerializeField] private Vector2 basePosition;
        [SerializeField] private bool captureBaseOnAwake = true;

        private RectTransform rt;
        private Canvas canvas;
        private Rect lastSafe;
        private Vector2Int lastSize;

        private void Awake()
        {
            rt = (RectTransform)transform;
            if (mode == Mode.PushIn && captureBaseOnAwake && Application.isPlaying) basePosition = rt.anchoredPosition;
        }

        private void OnEnable() { lastSize = Vector2Int.zero; Apply(); }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastSize.x || Screen.height != lastSize.y) Apply();
        }

        public void Apply()
        {
            if (!Application.isPlaying) return; // keep the authored layout in edit mode
            if (rt == null) rt = (RectTransform)transform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            lastSafe = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            if (mode == Mode.FitRect)
            {
                var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
                var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
                rt.anchorMin = min; rt.anchorMax = max;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                return;
            }

            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            if (scale <= 0f) scale = 1f;
            float left = safe.xMin / scale, right = (Screen.width - safe.xMax) / scale;
            float bottom = safe.yMin / scale, top = (Screen.height - safe.yMax) / scale;
            var a = (rt.anchorMin + rt.anchorMax) * 0.5f;
            var offset = Vector2.zero;
            if (a.x < 0.34f) offset.x += left; else if (a.x > 0.66f) offset.x -= right;
            if (a.y > 0.66f) offset.y -= top; else if (a.y < 0.34f) offset.y += bottom;
            rt.anchoredPosition = basePosition + offset;
        }
    }
}
