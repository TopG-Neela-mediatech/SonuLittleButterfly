using System;
using UnityEngine;

namespace tmkoc.butterfly
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private EndPanelScript endPanelScript;
        [SerializeField] private TutorialController tutorialController;
        private static GameManager instance;


        public static GameManager Instance { get { return instance; } }
        public LevelManager LevelManager { get { return levelManager; } }
        public SoundManager SoundManager { get { return soundManager; } }
        public EndPanelScript EndPanelScript { get { return endPanelScript; } }
        public TutorialController TutorialController { get { return tutorialController; } }


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            Screen.orientation = ScreenOrientation.Portrait;
        }


        #region Events
        public event Action OnLevelStart;
        public event Action OnLevelWin;
        public event Action OnLevelLose;
        /// <summary>A merge happened on the board (tier created).</summary>
        public event Action<int> OnItemMerged;
        /// <summary>A goal item was delivered to the garden (tier, remaining goals).</summary>
        public event Action<int, int> OnGoalDelivered;
        public event Action<LifeStage> OnStageComplete;
        public event Action OnGameComplete;

        public void InvokeLevelStart() => OnLevelStart?.Invoke();
        public void InvokeLevelWin() => OnLevelWin?.Invoke();
        public void InvokeLevelLose() => OnLevelLose?.Invoke();
        public void InvokeItemMerged(int tier) => OnItemMerged?.Invoke(tier);
        public void InvokeGoalDelivered(int tier, int remaining) => OnGoalDelivered?.Invoke(tier, remaining);
        public void InvokeStageComplete(LifeStage stage) => OnStageComplete?.Invoke(stage);
        public void InvokeGameComplete() => OnGameComplete?.Invoke();
        #endregion
    }
}
