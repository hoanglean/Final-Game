using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OooAdventure
{
    public sealed class HomeMenu : MonoBehaviour
    {
        public const string SceneName = "Home";
        public const string ForestScene = "Level01_FrozenForest";
        public const string BossScene = "Boss01_FrostGuardian";

        [SerializeField] GameObject mainPanel, guidePanel, chapterPanel;
        [SerializeField] GameObject[] levelPanels;

        GameObject currentPanel;
        bool loading;

        void Start() => ShowMain();

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        public void ShowMain() => Show(mainPanel);
        public void ShowGuide() => Show(guidePanel);
        public void ShowChapters() => Show(chapterPanel);

        public void ShowChapter(int index)
        {
            if (index >= 0 && index < levelPanels.Length) Show(levelPanels[index]);
        }

        public void Back()
        {
            if (currentPanel == null || currentPanel == mainPanel || loading) return;
            if (currentPanel == guidePanel || currentPanel == chapterPanel) ShowMain();
            else ShowChapters();
        }

        public void Play() => Load(ForestScene);
        public void PlayBoss() => Load(BossScene);

        void Show(GameObject next)
        {
            if (loading || next == null) return;
            if (currentPanel != null) currentPanel.SetActive(false);
            currentPanel = next;
            currentPanel.SetActive(true);
            if (EventSystem.current != null)
            {
                var first = next.GetComponentInChildren<UnityEngine.UI.Button>();
                EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
            }
        }

        void Load(string scene)
        {
            if (loading || !Application.CanStreamedLevelBeLoaded(scene)) return;
            loading = true;
            Time.timeScale = 1;
            SceneManager.LoadScene(scene);
        }
    }
}

