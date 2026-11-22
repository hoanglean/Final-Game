using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OooAdventure
{
    public sealed class HomeMenu : MonoBehaviour
    {
        public const string SceneName = "Home";
        public const string ForestScene = "Level01_FrozenForest";
        public const string BossScene = "Boss01_FrostGuardian";

        [SerializeField] GameObject mainPanel, guidePanel, chapterPanel;
        [SerializeField] GameObject[] levelPanels;

        GameObject currentPanel, areaPanel, selectedChapter;
        Button[] areaLevels;
        TMP_Text areaHeading, areaSubtitle;
        TMP_Text[] areaCaptions;
        Sprite lockedLevelIcon;
        bool loading;

        void Start()
        {
            areaPanel = Instantiate(levelPanels[0], levelPanels[0].transform.parent);
            areaPanel.name = "Area Level Panel";
            areaPanel.SetActive(false);
            areaHeading = areaPanel.transform.Find("Heading").GetComponent<TMP_Text>();
            areaSubtitle = areaPanel.transform.Find("Subtitle").GetComponent<TMP_Text>();
            areaLevels = areaPanel.GetComponentsInChildren<Button>(true)
                .Where(button => button.name != "Back")
                .OrderBy(button => ((RectTransform)button.transform).anchoredPosition.x)
                .ToArray();
            lockedLevelIcon = areaLevels.First(button => !button.interactable).transform.Find("Icon").GetComponent<Image>().sprite;

            areaCaptions = areaPanel.GetComponentsInChildren<TMP_Text>(true)
                .Where(label => label.name.StartsWith("Level Caption", System.StringComparison.Ordinal))
                .OrderBy(label => label.rectTransform.anchoredPosition.x)
                .ToArray();
            for (int i = 0; i < areaLevels.Length; i++)
            {
                areaLevels[i].name = areaCaptions[i].text = "LV" + (i + 1);
                areaCaptions[i].color = new Color(.94f, .97f, 1f);
            }

            foreach (var chapter in levelPanels)
            {
                foreach (var area in chapter.GetComponentsInChildren<Button>(true))
                {
                    if (area.name == "Back") continue;
                    area.onClick = new Button.ButtonClickedEvent();
                    switch (area.name)
                    {
                        case "Frost Guardian":
                            area.interactable = Application.CanStreamedLevelBeLoaded(BossScene);
                            area.onClick.AddListener(PlayBoss);
                            break;
                        case "Mecha Boss":
                        case "Hiệp Sĩ Bóng Tối":
                            area.interactable = false;
                            break;
                        default:
                            area.interactable = true;
                            string firstScene = area.name == "Rừng Băng" ? ForestScene : null;
                            area.onClick.AddListener(() => ShowArea(chapter, area, firstScene));
                            break;
                    }
                }
            }

            ShowMain();
        }

        void ShowArea(GameObject chapter, Button area, string firstScene)
        {
            selectedChapter = chapter;
            areaHeading.text = area.name;
            const int levelCount = 3;
            bool firstPlayable = !string.IsNullOrEmpty(firstScene) && Application.CanStreamedLevelBeLoaded(firstScene);
            areaSubtitle.text = firstPlayable ? "Chọn cấp độ để chơi" : "Các cấp độ đang phát triển";
            var areaIcon = area.transform.Find("Icon").GetComponent<Image>().sprite;
            for (int i = 0; i < areaLevels.Length; i++)
            {
                var level = areaLevels[i];
                bool visible = i < levelCount;
                level.gameObject.SetActive(visible);
                areaCaptions[i].gameObject.SetActive(visible);
                if (!visible) continue;

                float x = (i - (levelCount - 1) / 2f) * 200f;
                var levelRect = (RectTransform)level.transform;
                levelRect.anchoredPosition = new Vector2(x, levelRect.anchoredPosition.y);
                var captionRect = areaCaptions[i].rectTransform;
                captionRect.anchoredPosition = new Vector2(x, captionRect.anchoredPosition.y);
                bool playable = i == 0 && firstPlayable;
                level.interactable = playable;
                level.onClick = new Button.ButtonClickedEvent();
                if (playable) level.onClick.AddListener(() => Load(firstScene));
                var icon = level.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = playable ? areaIcon : lockedLevelIcon;
                icon.color = playable ? Color.white : new Color(.59f, .73f, .84f);
            }
            Show(areaPanel);
        }

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
            else if (currentPanel == areaPanel) Show(selectedChapter);
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
                var first = next.GetComponentsInChildren<Button>().FirstOrDefault(button => button.IsInteractable());
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

