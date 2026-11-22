using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace OooAdventure
{
    public class Level01Game : MonoBehaviour
    {
        public enum Mode { Playing = 1, Paused, Dead, Won }
        public static Level01Game Instance { get; private set; }
        public FinnController player;
        public Image healthFill;
        public TMP_Text healthText, shardText, objectiveText, notificationText, promptText, panelTitle, panelBody;
        [SerializeField] GameObject notificationPanel, promptPanel;
        public GameObject overlay, pausePanel, resultPanel;
        public Button resumeButton, homeButton;
        public Button primaryButton, secondaryButton;
        public TMP_Text primaryText, secondaryText;
        public int RequiredShards = 3;
        public int Shards { get; private set; }
        public Mode CurrentMode { get; private set; }
        public bool IsPlaying => CurrentMode == Mode.Playing;
        float notificationUntil, promptUntil;
        AudioSource audioSource;
        AudioClip cue;
        void Awake()
        {
            Instance = this; Time.timeScale = 1;
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"));
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Enemy"));
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("PlayerSword"), LayerMask.NameToLayer("Player"));
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("PlayerSword"), LayerMask.NameToLayer("PlayerSword"));
        }
        protected virtual void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.volume = .12f;
            var samples = new float[4410];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 440 / 44100) * (1f - (float)i / samples.Length);
            cue = AudioClip.Create("Soft gameplay cue", samples.Length, 1, 44100, false); cue.SetData(samples, 0);
            primaryButton.onClick.AddListener(GoHome);
            secondaryButton.onClick.AddListener(RestartLevel);
            resumeButton.onClick.AddListener(BeginRun);
            homeButton.interactable = true;
            homeButton.onClick.AddListener(GoHome);
            RefreshHud(); BeginRun();
        }
        void Update()
        {
            var key = Keyboard.current;
            if (key != null && key.escapeKey.wasPressedThisFrame)
            { if (IsPlaying && !player.IsDead) ShowPanel(Mode.Paused); else if (CurrentMode == Mode.Paused) BeginRun(); }
            if (Time.unscaledTime > notificationUntil) SetMessage(notificationText, notificationPanel, "");
            if (Time.unscaledTime > promptUntil) SetMessage(promptText, promptPanel, "");
        }
        public void BeginRun()
        { CurrentMode = Mode.Playing; overlay.SetActive(false); Time.timeScale = 1; }
        public void CollectShard()
        {
            Shards = Mathf.Min(RequiredShards, Shards + 1); RefreshHud(); PlayTone(1000, .12f);
            Notify(Shards == RequiredShards ? "Đủ 3 bánh răng! Tới cổng cuối rừng và nhấn E để sửa cổng." : "Đã nhặt được một bánh răng đồng!", 4);
        }
        public virtual void RefreshHud()
        {
            healthFill.fillAmount = (float)player.Health / player.maxHealth;
            healthText.text = player.Health + " / " + player.maxHealth;
            shardText.text = Shards + " / " + RequiredShards;
            objectiveText.text = Shards < RequiredShards ? "Thu thập 3 bánh răng" : "Sửa cổng thời gian  [E]";
        }
        public void Notify(string text, float seconds = 4)
        {
            SetMessage(notificationText, notificationPanel, text);
            notificationUntil = Time.unscaledTime + seconds;
        }
        public void Prompt(string text)
        {
            SetMessage(promptText, promptPanel, text);
            promptUntil = Time.unscaledTime + .1f;
        }
        static void SetMessage(TMP_Text label, GameObject panel, string text)
        {
            if (label.text != text) label.text = text;
            if (panel == null) return;
            bool visible = !string.IsNullOrEmpty(text);
            if (panel.activeSelf != visible) panel.SetActive(visible);
        }
        public void PlayerDied() { StartCoroutine(DeathDelay()); }
        IEnumerator DeathDelay()
        { yield return new WaitForSeconds(1f); ShowPanel(Mode.Dead); }
        public bool TryFinish()
        {
            if (Shards < RequiredShards) { Notify("Cần đủ 3 bánh răng để sửa cổng. Bánh răng cuối do kẻ giữ cổng canh giữ.", 5); 
            return false; 
            }
            ShowPanel(Mode.Won); return true;
        }
        public void RestartLevel()
        { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        void GoHome()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(HomeMenu.SceneName);
        }
        protected virtual void ShowPanel(Mode mode)
        {
            CurrentMode = mode; Time.timeScale = 0; overlay.SetActive(true);
            bool paused = mode == Mode.Paused;
            pausePanel.SetActive(paused);
            resultPanel.SetActive(!paused);
            if (paused) return;
            secondaryButton.gameObject.SetActive(true);
            secondaryText.text = "CHƠI LẠI TỪ ĐẦU";
            if (mode == Mode.Dead)
            { panelTitle.text = "ĐỪNG BỎ CUỘC, FINN!"; panelBody.text = "Chọn chơi lại để bắt đầu màn từ đầu."; }
            else
            { panelTitle.text = "ĐÃ SỬA CỔNG THỜI GIAN!"; panelBody.text = "MÀN 01 HOÀN THÀNH\n\nFinn đã thu thập đủ 3 bánh răng\nvà tìm được lối ra khỏi rừng băng.\n\nHành trình tới hang băng sẽ tiếp tục ở màn 02."; }
        }
        public void PlayTone(float frequency, float duration)
        { if (audioSource != null && cue != null) { audioSource.pitch = frequency / 440; audioSource.PlayOneShot(cue, .5f); } }
        void OnDestroy()
        { if (Instance == this) { Instance = null; Time.timeScale = 1; } if (cue != null) Destroy(cue); }
    }
}
