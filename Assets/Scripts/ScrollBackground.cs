using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    public class ScrollBackground : MonoBehaviour
    {
        [SerializeField] private RawImage background;
        [SerializeField] private float scrollSpeed = 1f;
        [Tooltip("Scroll direction in UV space. (1, 0) = clouds drift sideways only.")]
        [SerializeField] private Vector2 direction = new Vector2(1f, 0f);

        private void Start()
        {
            if (background == null)
            {
                Debug.LogError("No background image assigned!");
                return;
            }
        }
        private void Update()
        {
            ScrollBG();
        }
        private void ScrollBG()
        {
            if (background != null)
            {
                // wrap the offset so it never grows huge (precision) - the texture repeats, so this is seamless
                Vector2 o = direction * (Time.time * scrollSpeed);
                o.x -= Mathf.Floor(o.x); o.y -= Mathf.Floor(o.y);
                var r = background.uvRect;
                background.uvRect = new Rect(o.x, o.y, r.width, r.height);
            }            
        }
    }
}
