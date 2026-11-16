using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// Bridge to the shared Playschool win/lose popup (WinLosePanelCanvas). The popup is normally spawned
    /// through PlayschoolCommon (which lives in PlayschoolMainScene); when the gameplay scene is opened
    /// directly in the editor the fallback prefab is instantiated instead so the flow still works.
    /// </summary>
    public class EndPanelScript : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject winLosePanelFallbackPrefab;
        [SerializeField] private Button[] homeButtons;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            if (PlayschoolCommon.Instance != null)
                PlayschoolCommon.Instance.SpawnplayschoolWinLosePanel();
            else if (WinLosePanelScript.Instance == null && winLosePanelFallbackPrefab != null)
                Instantiate(winLosePanelFallbackPrefab);
        }

        private void Start()
        {
            if (homeButtons == null) return;
            foreach (var homeButton in homeButtons)
                if (homeButton != null)
                    homeButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
        }

        public void ShowWin()
        {
            if (WinLosePanelScript.Instance == null)
            {
                Debug.LogWarning("WinLosePanelScript missing; loading next level directly.");
                GameManager.Instance.LevelManager.LoadNextLevel();
                return;
            }
            IsShowing = true;
            WinLosePanelScript.Instance.ShowNextLevelPopUp(() => { IsShowing = false; GameManager.Instance.LevelManager.LoadNextLevel(); });
        }

        public void ShowLose()
        {
            if (WinLosePanelScript.Instance == null)
            {
                GameManager.Instance.LevelManager.StartLevel();
                return;
            }
            IsShowing = true;
            WinLosePanelScript.Instance.ShowRetryPopUp(() => { IsShowing = false; GameManager.Instance.LevelManager.StartLevel(); });
        }
    }
}
