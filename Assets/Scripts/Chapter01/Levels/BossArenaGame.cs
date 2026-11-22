using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OooAdventure
{
    public class BossArenaGame : Level01Game
    {
        public FrostBossController boss;
        public Image bossHealthFill;
        public TMP_Text bossHealthText, bossPhaseText;

        protected override void Start()
        {
            base.Start();
            Notify("Né vùng đỏ • Nhảy qua đòn lao • Áp sát và nhấn Enter để đánh", 6);
        }

        public override void RefreshHud()
        {
            base.RefreshHud();
            objectiveText.text = "Đánh bại Kẻ Giữ Băng";
            if (boss == null || bossHealthFill == null) return;
            bossHealthFill.fillAmount = (float)boss.Health / boss.maxHealth;
            bossHealthText.text = boss.Health + " / " + boss.maxHealth;
            bossPhaseText.text = boss.IsDead ? "ĐÃ ĐÁNH BẠI" : "GIAI ĐOẠN " + boss.Phase;
            bossHealthFill.color = boss.Phase == 2 ? new Color(.9f,.35f,.65f) : new Color(.3f,.85f,1);
        }

        public void BossDefeated()
        {
            foreach (var shot in FindObjectsByType<IceProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            foreach (var pillar in FindObjectsByType<IcePillarHazard>(FindObjectsSortMode.None)) Destroy(pillar.gameObject);
            RefreshHud();
            StartCoroutine(Victory());
        }

        IEnumerator Victory()
        {
            yield return new WaitForSeconds(1.8f);
            if (!player.IsDead && IsPlaying) ShowPanel(Mode.Won);
        }

        protected override void ShowPanel(Mode mode)
        {
            base.ShowPanel(mode);
            if (mode == Mode.Paused) return;
            panelTitle.text = mode == Mode.Dead ? "FINN ĐÃ GỤC NGÃ" : "KẺ GIỮ BĂNG ĐÃ BỊ ĐÁNH BẠI!";
            panelBody.text = mode == Mode.Dead
                ? "Tránh vùng đỏ trước khi cột băng xuất hiện.\nNhảy qua đòn lao và áp sát khi boss nghỉ.\n\nThử lại sẽ hồi đầy máu cho cả Finn và boss."
                : "ĐẤU TRƯỜNG BĂNG • HOÀN THÀNH\n\nFinn đã vượt qua hai giai đoạn của Kẻ Giữ Băng.\n\nĐây là màn boss thử nghiệm riêng.";
        }
    }
}
