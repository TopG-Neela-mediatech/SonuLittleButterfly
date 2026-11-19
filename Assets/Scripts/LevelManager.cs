using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Level flow (mirrors the HTML prototype): 4 main levels, one per life-cycle stage, each made of 3-4
    /// sub-levels (rounds) played back to back while the garden grows. After a main level: stage
    /// transformation → win panel (auto-continues) → the story leading into the next main level → next level.
    /// Stories only play before a main level (intro before level 1); the last level ends with its story
    /// and the final badge/stars.
    /// Progress is saved through HelperGameCategoryDataSaver like the other Playschool games.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Button playSchoolBackButton;

        [Header("Butterfly Flow")]
        [SerializeField] private LevelDatabase database;
        [SerializeField] private ButterflyLevelController levelController;
        [SerializeField] private StoryController storyController;
        [SerializeField] private StoryData introStory;
        [SerializeField] private FinalCelebrationPanel finalPanel;

        [Header("Rewards")]
        [SerializeField] private int completionStars = 10;
        [SerializeField] private string completionTransactionId = "SonuLittleButterfly_GameComplete";

        [Header("Timing")]
        [SerializeField] private float winPanelDelay = 1f;

        /// <summary>Main level index (0..3). This is what is saved.</summary>
        public int currentLevelIndex { get; private set; }
        /// <summary>Round inside the current main level (not saved: a main level restarts from its first round).</summary>
        public int currentSubIndex { get; private set; }
        public int TotalLevels => database != null ? database.MainCount : 0;
        public MainLevel CurrentMain => database != null ? database.GetMain(currentLevelIndex) : null;
        public LevelDefinition CurrentLevelData
        {
            get
            {
                var main = CurrentMain;
                return main != null && currentSubIndex < main.SubCount ? main.subLevels[currentSubIndex] : null;
            }
        }
        public ThemeData CurrentTheme => CurrentMain != null ? database.GetTheme(CurrentMain.theme) : null;
        public void StartLevel() => GameManager.Instance.InvokeLevelStart();

        private Action pendingStoryCallback;

        private void Awake()
        {
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
            SetDataSaver();
            if (playSchoolBackButton != null)
                playSchoolBackButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
        }

        private void Start()
        {
            GameManager.Instance.OnLevelStart -= OnLevelStart;
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin -= OnLevelWin;
            GameManager.Instance.OnLevelWin += OnLevelWin;
            if (levelController != null) levelController.OnSubLevelComplete += OnSubLevelComplete;
            if (storyController != null)
            {
                storyController.OnStoryFinished += OnStoryFinished;
                storyController.gameObject.SetActive(false);
            }
            if (currentLevelIndex == 0 && introStory != null)
                PlayStory(introStory, i => GameManager.Instance.SoundManager?.PlayStorySlide(i) ?? -1f, StartLevel);
            else
                StartLevel();
        }

        private void OnLevelStart()
        {
            var def = CurrentLevelData;
            if (def == null) return;
            var main = CurrentMain;
            bool firstRound = currentSubIndex == 0;
            levelController.InitializeLevel(def, CurrentTheme, currentLevelIndex, TotalLevels, currentSubIndex, main.SubCount, firstRound);
            if (firstRound)
            {
                GameManager.Instance.SoundManager?.PlayBGM();
                GameManager.Instance.SoundManager?.PlayLevelIntro(main.theme);
            }
        }

        /// <summary>A round's goals are all delivered: next round, or the main level is won.</summary>
        private void OnSubLevelComplete()
        {
            var main = CurrentMain;
            if (main != null && currentSubIndex < main.SubCount - 1)
            {
                currentSubIndex++;
                levelController.PlaySubLevelOutro(() => GameManager.Instance.InvokeLevelStart());
                return;
            }
            GameManager.Instance.InvokeLevelWin();
        }

        private void OnLevelWin()
        {
            bool last = currentLevelIndex >= TotalLevels - 1;
            var theme = CurrentTheme;
            GameManager.Instance.SoundManager?.PlaySFX(SoundManager.SfxType.StageComplete);
            levelController.PlayStageTransform(() =>
            {
                // after the narrated garden animation, so the two voice lines don't cut each other off
                GameManager.Instance.SoundManager?.PlayLevelOutro();
                GameManager.Instance.InvokeStageComplete(theme.stage);
                if (!last) { StartCoroutine(LoadWinPanelWithDelay(winPanelDelay)); return; }
                // the very end: closing story, then the badge and stars, then the win panel
                var stage = theme.stage;
                PlayStory(theme.stageEndStory, i => GameManager.Instance.SoundManager?.PlayStageStory(stage, i) ?? -1f, ShowFinalCelebration);
            });
        }

        private void ShowFinalCelebration()
        {
            GameManager.Instance.SoundManager?.PlaySFX(SoundManager.SfxType.Celebrate);
            GameManager.Instance.SoundManager?.PlayFinalOutro();
            GameManager.Instance.InvokeGameComplete();
            int awarded = AwardCompletionStars();
            if (finalPanel != null) finalPanel.Show(awarded, FinishGame);
            else FinishGame();
        }

        /// <summary>All four stages done. Inside the Playschool app the shared game-end panel pops up; standalone shows the win panel.</summary>
        private void FinishGame()
        {
#if PLAYSCHOOL_MAIN
            currentLevelIndex = TotalLevels;
            HelperGameCategoryDataSaver.LevelCompleted(TotalLevels);
            EffectParticleControll.Instance.SpawnGameEndPanel();
            GameManager.Instance.SoundManager.StopBGM();
            GameOverEndPanel.Instance.AddTheListnerRetryGame();
#else
            ShowWinPanel();
#endif
        }

        private int AwardCompletionStars()
        {
            try
            {
                if (completionStars > 0 && StarWallet.Credit(completionStars, completionTransactionId, StarChangeReason.GameCompleted))
                    return completionStars;
            }
            catch (Exception e) { Debug.LogWarning("Star award skipped: " + e.Message); }
            return 0;
        }

        private IEnumerator LoadWinPanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowWinPanel();
        }

        private void ShowWinPanel()
        {
            GameManager.Instance.EndPanelScript.ShowWin();
            SaveLevel();
        }

        private void SetDataSaver()
        {
            HelperGameCategoryDataSaver.Init(Mathf.Max(1, TotalLevels));
            currentLevelIndex = HelperGameCategoryDataSaver.GetStartLevel();
            if (currentLevelIndex < 0 || currentLevelIndex >= TotalLevels) currentLevelIndex = 0;
        }

        private void SaveLevel()
        {
            currentLevelIndex++;
            if (currentLevelIndex >= TotalLevels)
            {
                HelperGameCategoryDataSaver.LevelCompleted(TotalLevels);
#if PLAYSCHOOL_MAIN
                EffectParticleControll.Instance.SpawnGameEndPanel();
                GameManager.Instance.SoundManager.StopBGM();
                GameOverEndPanel.Instance.AddTheListnerRetryGame();
                return;
#endif
                currentLevelIndex = 0;
                return;
            }
            HelperGameCategoryDataSaver.LevelCompleted(currentLevelIndex);
        }

        /// <summary>Called by the win panel: the story leading into the next main level, then that level.</summary>
        public void LoadNextLevel()
        {
            if (currentLevelIndex >= TotalLevels) return;
            currentSubIndex = 0;
            if (currentLevelIndex == 0)
            {
                if (introStory != null) PlayStory(introStory, i => GameManager.Instance.SoundManager?.PlayStorySlide(i) ?? -1f, StartLevel);
                else StartLevel();
                return;
            }
            var prev = database.GetTheme(database.GetMain(currentLevelIndex - 1).theme);
            var stage = prev.stage;
            PlayStory(prev.stageEndStory, i => GameManager.Instance.SoundManager?.PlayStageStory(stage, i) ?? -1f, StartLevel);
        }

        private void PlayStory(StoryData data, Func<int, float> narration, Action done)
        {
            if (storyController == null || data == null || data.slides == null || data.slides.Length == 0)
            {
                done?.Invoke();
                return;
            }
            pendingStoryCallback = done;
            storyController.SetStoryData(data, narration);
            storyController.gameObject.SetActive(true);
        }

        private void OnStoryFinished()
        {
            if (storyController != null) storyController.gameObject.SetActive(false);
            var cb = pendingStoryCallback;
            pendingStoryCallback = null;
            cb?.Invoke();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelStart -= OnLevelStart;
                GameManager.Instance.OnLevelWin -= OnLevelWin;
            }
            if (storyController != null) storyController.OnStoryFinished -= OnStoryFinished;
            if (levelController != null) levelController.OnSubLevelComplete -= OnSubLevelComplete;
        }
    }
}
