using System;
using UnityEngine;

namespace tmkoc.butterfly
{
    public enum MergeTheme { Water, Leaf, Twig, Flower }
    public enum LifeStage { Egg, Caterpillar, Chrysalis, Butterfly }

    [Serializable]
    public class GoalRequirement
    {
        [Range(2, 4)] public int tier = 2;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class LevelDefinition
    {
        [TextArea(1, 2)] public string objective;
        public MergeTheme theme;
        [Min(2)] public int columns = 3;
        [Min(2)] public int rows = 3;
        public GoalRequirement[] goals;
        public bool isTutorial;

        /// <summary>Tier-1 items needed to build every goal exactly once (two items of a tier make one of the next).</summary>
        public int TotalSpawnCount
        {
            get
            {
                int total = 0;
                if (goals == null) return total;
                foreach (var g in goals) total += g.count * (1 << (g.tier - 1));
                return total;
            }
        }
    }

    [Serializable]
    public class ThemeData
    {
        public MergeTheme theme;
        public LifeStage stage;
        public string stageTitle;
        [TextArea(1, 2)] public string gardenCaption;
        public string[] tierNames = new string[4];
        [Tooltip("Placeholder: one sprite per tier. Leave empty to use the fallback sprite tinted with 'tint'.")]
        public Sprite[] tierSprites = new Sprite[4];
        public Color tint = Color.white;
        [Tooltip("Story shown after the last level of this stage (the stage gratification).")]
        public StoryData stageEndStory;
    }

    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "SonuButterfly/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        public ThemeData[] themes = new ThemeData[4];
        public LevelDefinition[] levels;

        public ThemeData GetTheme(MergeTheme theme)
        {
            foreach (var t in themes) if (t != null && t.theme == theme) return t;
            return themes.Length > 0 ? themes[0] : null;
        }

        public bool IsStageEnd(int levelIndex)
        {
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length) return false;
            if (levelIndex == levels.Length - 1) return true;
            return levels[levelIndex + 1].theme != levels[levelIndex].theme;
        }

        public bool IsStageStart(int levelIndex)
        {
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length) return false;
            return levelIndex == 0 || levels[levelIndex - 1].theme != levels[levelIndex].theme;
        }
    }
}
