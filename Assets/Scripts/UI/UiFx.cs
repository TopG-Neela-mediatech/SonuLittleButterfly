using System;
using System.Collections.Generic;
using AssetKits.ParticleImage;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    public enum FxType { Sparkle, Puff, Confetti }

    /// <summary>
    /// Pooled UI effects. Each effect type has a few pre-made ParticleImage emitters that are reused
    /// round-robin (a new Play() just restarts the oldest one), plus a small pool of glow rings
    /// driven by DOTween. Nothing is instantiated or destroyed while playing.
    /// </summary>
    public class UiFx : MonoBehaviour
    {
        [Serializable]
        private class Effect
        {
            public FxType type;
            public ParticleImage template;
            [Min(1)] public int poolSize = 3;
            [NonSerialized] public readonly List<ParticleImage> pool = new List<ParticleImage>();
            [NonSerialized] public int next;
        }

        [SerializeField] private Effect[] effects;
        [SerializeField] private Image ringTemplate;
        [SerializeField, Min(1)] private int ringPoolSize = 4;

        public static UiFx Instance { get; private set; }

        private readonly List<Image> rings = new List<Image>();
        private int nextRing;

        private void Awake()
        {
            Instance = this;
            if (effects != null)
                foreach (var e in effects)
                {
                    if (e.template == null) continue;
                    e.pool.Clear();
                    e.pool.Add(e.template);
                    for (int i = 1; i < e.poolSize; i++)
                    {
                        var clone = Instantiate(e.template, e.template.transform.parent);
                        clone.name = e.template.name + "_" + i;
                        e.pool.Add(clone);
                    }
                    foreach (var p in e.pool) { p.raycastTarget = false; p.Stop(true); }
                }
            if (ringTemplate != null)
            {
                rings.Add(ringTemplate);
                for (int i = 1; i < ringPoolSize; i++) rings.Add(Instantiate(ringTemplate, ringTemplate.transform.parent));
                foreach (var r in rings) { r.raycastTarget = false; r.gameObject.SetActive(false); }
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Plays a particle burst at a world position (e.g. a RectTransform's position).</summary>
        public void Play(FxType type, Vector3 worldPos)
        {
            if (effects == null) return;
            foreach (var e in effects)
            {
                if (e.type != type || e.pool.Count == 0) continue;
                var p = e.pool[e.next];
                e.next = (e.next + 1) % e.pool.Count;
                p.transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
                p.Stop(true);
                p.Play();
                return;
            }
        }

        /// <summary>Expanding, fading glow ring (size in canvas units).</summary>
        public void Ring(Vector3 worldPos, float size, Color color)
        {
            if (rings.Count == 0) return;
            var r = rings[nextRing];
            nextRing = (nextRing + 1) % rings.Count;
            var rt = r.rectTransform;
            rt.DOKill(); r.DOKill();
            r.gameObject.SetActive(true);
            rt.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one * 0.35f;
            r.color = color;
            rt.DOScale(1.25f, 0.45f).SetEase(Ease.OutCubic);
            r.DOFade(0f, 0.45f).SetEase(Ease.InQuad).OnComplete(() => r.gameObject.SetActive(false));
        }
    }
}
