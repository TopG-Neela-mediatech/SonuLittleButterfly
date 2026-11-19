using UnityEngine;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Every voice key below is a "voiceover_title" row in AudioXL/Sonu's Little Butterfly - Audio Script.xlsx;
    /// RuntimeAudioLoader looks clips up by that exact string.
    /// Voice lines are addressed by string key through RuntimeAudioLoader (downloaded per language);
    /// every Play* method returns the clip length so callers can pace the game, or -1 when missing.
    /// Local SFX use scene AudioSources. All calls are safe when RuntimeAudioLoader is absent
    /// (e.g. when the gameplay scene is opened directly in the editor).
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private ButterflyAudioMapper audioMapper;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private SFXData[] sfxData;

        public enum SfxType { None, Merge, Move, Wrong, Deliver, StageComplete, Celebrate }

        /// <summary>Narrated moments of the end-of-level animations in the garden (the caption pill shows the same line).</summary>
        public enum GardenMoment { EggLaid, EggHatched, CaterpillarToBranch, ChrysalisForms, ChrysalisRests, ChrysalisMoving, ButterflyEmerges, ButterflyNectar }

        [System.Serializable]
        public class SFXData
        {
            public SfxType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public void PlayBGM()
        {
            if (bgmSource == null || bgmSource.clip == null || bgmSource.isPlaying) return;
            bgmSource.Play();
        }
        public void StopBGM() { if (bgmSource != null) bgmSource.Stop(); }

        public void PlaySFX(SfxType type)
        {
            if (sfxSource == null || sfxData == null) return;
            foreach (var s in sfxData)
            {
                if (s.type == type && s.clip != null)
                {
                    sfxSource.PlayOneShot(s.clip, s.volume);
                    return;
                }
            }
        }

        public float PlayStorySlide(int slideIndex) => PlayKey(audioMapper.storySlides, slideIndex);
        public float PlayStageStory(LifeStage stage, int slideIndex)
        {
            string[] keys = stage switch
            {
                LifeStage.Egg => audioMapper.stage1EndSlides,
                LifeStage.Caterpillar => audioMapper.stage2EndSlides,
                LifeStage.Chrysalis => audioMapper.stage3EndSlides,
                _ => audioMapper.stage4EndSlides,
            };
            return PlayKey(keys, slideIndex);
        }
        public float PlayLevelIntro(MergeTheme theme)
        {
            string[] keys = theme switch
            {
                MergeTheme.Water => audioMapper.waterIntros,
                MergeTheme.Leaf => audioMapper.leafIntros,
                MergeTheme.Twig => audioMapper.twigIntros,
                _ => audioMapper.flowerIntros,
            };
            return PlayRandom(keys);
        }
        public float PlayLevelOutro() => PlayRandom(audioMapper.levelOutros);
        public float PlayGoalDelivered() => PlayRandom(audioMapper.goalDelivered);
        public float PlayWrongMove() => PlayRandom(audioMapper.wrongMove);
        public float PlayIdle() => PlayRandom(audioMapper.idle);
        public float PlayFinalOutro() => PlayKey(new[] { audioMapper.finalOutro }, 0);
        public float PlayGardenMoment(GardenMoment moment)
        {
            string key = moment switch
            {
                GardenMoment.EggLaid => audioMapper.eggLaid,
                GardenMoment.EggHatched => audioMapper.eggHatched,
                GardenMoment.CaterpillarToBranch => audioMapper.caterpillarToBranch,
                GardenMoment.ChrysalisForms => audioMapper.chrysalisForms,
                GardenMoment.ChrysalisRests => audioMapper.chrysalisRests,
                GardenMoment.ChrysalisMoving => audioMapper.chrysalisMoving,
                GardenMoment.ButterflyEmerges => audioMapper.butterflyEmerges,
                _ => audioMapper.butterflyNectar,
            };
            return PlayKey(new[] { key }, 0);
        }

        public void StopVoice() => RuntimeAudioLoader.Instance?.StopCommonAudioSource();

        private float PlayKey(string[] keys, int index)
        {
            if (keys == null || index < 0 || index >= keys.Length || string.IsNullOrEmpty(keys[index])) return -1f;
            if (RuntimeAudioLoader.Instance == null) return -1f;
            return RuntimeAudioLoader.Instance.PlayRuntimeAudio(keys[index]);
        }
        private float PlayRandom(string[] keys)
        {
            if (keys == null || keys.Length == 0) return -1f;
            return PlayKey(keys, Random.Range(0, keys.Length));
        }
    }

    [System.Serializable]
    public class ButterflyAudioMapper
    {
        [Header("Intro story (one key per slide)")]
        public string[] storySlides = { "story1", "story2", "story3" };
        [Header("Stage-end stories")]
        public string[] stage1EndSlides = { "stage1_end1", "stage1_end2" };
        public string[] stage2EndSlides = { "stage2_end1", "stage2_end2" };
        public string[] stage3EndSlides = { "stage3_end1", "stage3_end2" };
        public string[] stage4EndSlides = { "stage4_end1", "stage4_end2" };
        [Header("Level intros per merge theme (random pick)")]
        public string[] waterIntros = { "water_intro1" };
        public string[] leafIntros = { "leaf_intro1" };
        public string[] twigIntros = { "twig_intro1" };
        public string[] flowerIntros = { "flower_intro1" };
        [Header("Feedback")]
        public string[] levelOutros = { "outro1", "outro2" };
        public string[] goalDelivered = { "goal1", "goal2" };
        public string[] wrongMove = { "wrong1", "wrong2" };
        public string[] idle = { "idle1", "idle2" };
        public string finalOutro = "final_outro";
        [Header("End-of-level garden moments (narrate the caption pill)")]
        public string eggLaid = "egg_laid";
        public string eggHatched = "egg_hatch";
        public string caterpillarToBranch = "caterpillar_branch";
        public string chrysalisForms = "chrysalis_form";
        public string chrysalisRests = "chrysalis_rest";
        public string chrysalisMoving = "chrysalis_move";
        public string butterflyEmerges = "butterfly_emerge";
        public string butterflyNectar = "butterfly_nectar";
    }
}
