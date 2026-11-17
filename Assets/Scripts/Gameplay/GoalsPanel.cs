using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>Row of goal chips: icon, item name and "collected x / y".</summary>
    public class GoalsPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private GameObject chipTemplate;
        [SerializeField] private Color pendingColor = new Color(1f, 0.93f, 0.79f);
        [SerializeField] private Color doneColor = new Color(0.77f, 0.91f, 0.68f);

        private readonly List<Chip> chips = new List<Chip>();

        private class Chip
        {
            public int tier; public int total;
            public GameObject go; public Image bg; public MergeItemView icon; public TextMeshProUGUI name; public TextMeshProUGUI count;
        }

        private void Awake()
        {
            if (container == null) container = (RectTransform)transform;
            if (chipTemplate == null && transform.childCount > 0) chipTemplate = transform.GetChild(0).gameObject;
            if (chipTemplate != null) chipTemplate.SetActive(false);
        }

        public void Setup(GoalRequirement[] goals, ThemeData theme, Sprite fallback)
        {
            foreach (var c in chips) if (c.go != null) Destroy(c.go);
            chips.Clear();
            if (goals == null || chipTemplate == null) return;
            foreach (var g in goals)
            {
                var go = Instantiate(chipTemplate, container);
                go.SetActive(true);
                var chip = new Chip
                {
                    tier = g.tier,
                    total = g.count,
                    go = go,
                    bg = go.GetComponent<Image>(),
                    icon = go.GetComponentInChildren<MergeItemView>(true),
                    name = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                    count = go.transform.Find("Count")?.GetComponent<TextMeshProUGUI>()
                };
                if (chip.icon != null) chip.icon.Apply(g.tier, theme, fallback);
                if (chip.name != null) chip.name.text = theme != null && g.tier - 1 < theme.tierNames.Length ? theme.tierNames[g.tier - 1] : "Tier " + g.tier;
                chips.Add(chip);
                Refresh(chip, 0);
            }
        }

        public void SetCollected(int tier, int collected)
        {
            foreach (var c in chips)
            {
                if (c.tier != tier) continue;
                Refresh(c, collected);
                if (c.go != null)
                {
                    c.go.transform.DOKill(true);
                    c.go.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 6, 0.6f);
                    UiFx.Instance?.Play(FxType.Sparkle, c.icon != null ? c.icon.transform.position : c.go.transform.position);
                }
            }
        }

        private void Refresh(Chip c, int collected)
        {
            if (c.count != null) c.count.text = $"{collected}/{c.total}";
            if (c.bg != null) c.bg.color = collected >= c.total ? doneColor : pendingColor;
        }
    }
}
