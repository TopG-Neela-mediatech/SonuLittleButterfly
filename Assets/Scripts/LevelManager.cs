using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Level flow: intro story → levels (from the LevelDatabase) → stage gratification + stage story
    /// at the end of each life-cycle stage → final badge/stars after the last level.
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

        public int currentLevelIndex { get; private set; }
        public int TotalLevels => database != null && database.levels != null ? database.levels.Length : 0;
        public LevelDefinition CurrentLevelData => database != null && currentLevelIndex < TotalLevels ? database.levels[currentLevelIndex] : null;
        public ThemeData CurrentTheme => CurrentLevelData != null ? database.GetTheme(CurrentLevelData.theme) : null;
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
            levelController.InitializeLevel(def, CurrentTheme, currentLevelIndex, TotalLevels);
            GameManager.Instance.SoundManager?.PlayBGM();
            if (database.IsStageStart(currentLevelIndex)) GameManager.Instance.SoundManager?.PlayLevelIntro(def.theme);
        }

        private void OnLevelWin()
        {
            GameManager.Instance.SoundManager?.PlayLevelOutro();
            bool stageEnd = database.IsStageEnd(currentLevelIndex);
            bool last = currentLevelIndex >= TotalLevels - 1;
            if (!stageEnd)
            {
                StartCoroutine(LoadWinPanelWithDelay(winPanelDelay));
                return;
            }
            var theme = CurrentTheme;
            GameManager.Instance.SoundManager?.PlaySFX(SoundManager.SfxType.StageComplete);
            levelController.PlayStageTransform(() =>
            {
                GameManager.Instance.InvokeStageComplete(theme.stage);
                var stage = theme.stage;
                PlayStory(theme.stageEndStory, i => GameManager.Instance.SoundManager?.PlayStageStory(stage, i) ?? -1f, () =>
                {
                    if (last) ShowFinalCelebration();
                    else ShowWinPanel();
                });
            });
        }

        private void ShowFinalCelebration()
        {
            GameManager.Instance.SoundManager?.PlaySFX(SoundManager.SfxType.Celebrate);
            GameManager.Instance.SoundManager?.PlayFinalOutro();
            GameManager.Instance.InvokeGameComplete();
            int awarded = AwardCompletionStars();
            if (finalPanel != null) finalPanel.Show(awarded, ShowWinPanel);
            else ShowWinPanel();
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

        public void LoadNextLevel()
        {
            if (currentLevelIndex >= TotalLevels) return;
            if (currentLevelIndex == 0 && introStory != null)
                PlayStory(introStory, i => GameManager.Instance.SoundManager?.PlayStorySlide(i) ?? -1f, StartLevel);
            else
                StartLevel();
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
        }
    }
}
