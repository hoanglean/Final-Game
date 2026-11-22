using System.Collections;
using UnityEngine;

namespace OooAdventure
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class FrostBossController : MonoBehaviour, IDamageable
    {
        public int maxHealth = 630;
        public Animator animator;
        public SpriteRenderer image;
        public IceProjectile projectilePrefab;
        public IcePillarHazard pillarPrefab;
        public BoxCollider2D dashHitbox;
        public float leftLimit = -10.3f, rightLimit = 10.3f;
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;
        public int Phase => Health <= maxHealth / 2 ? 2 : 1;
        public string CurrentAttack { get; private set; } = "Rest";
        Rigidbody2D body;
        BossArenaGame game;
        float flashUntil;
        bool enraged;
        readonly Color frost = new Color(.7f,.91f,1);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); Health = maxHealth;
            dashHitbox.enabled = false;
        }
        IEnumerator Start()
        {
            game = (BossArenaGame)Level01Game.Instance;
            yield return new WaitForSeconds(2);
            int pattern = 0;
            while (!IsDead)
            {
                if (!game.IsPlaying || game.player.IsDead) { yield return null; continue; }
                if (pattern % 3 == 0) yield return Volley();
                else if (pattern % 3 == 1) yield return Pillars();
                else yield return Dash();
                if (IsDead) yield break;
                CurrentAttack = "Rest";
                body.linearVelocityX = 0; animator.SetBool("Run", false);
                image.color = frost;
                yield return new WaitForSeconds(Phase == 2 ? 1.1f : 1.6f);
                pattern++;
            }
        }
        bool CanAttack => !IsDead && game.IsPlaying && !game.player.IsDead;
        int FacePlayer()
        {
            int direction = game.player.transform.position.x < transform.position.x ? -1 : 1;
            image.flipX = direction > 0;
            return direction;
        }
        IEnumerator Volley()
        {
            CurrentAttack = "Volley"; FacePlayer();
            animator.SetTrigger("Atk"); image.color = new Color(.35f,1,1);
            game.Notify("BĂNG CẦU • Nhảy hoặc đổi hướng để né", 1.4f);
            yield return new WaitForSeconds(.65f);
            int count = Phase == 2 ? 5 : 3;
            for (int i = 0; i < count && CanAttack; i++)
            {
                int direction = FacePlayer();
                Vector2 origin = (Vector2)transform.position + new Vector2(direction * 1.1f, 1.4f);
                Vector2 aim = ((Vector2)game.player.transform.position + Vector2.up*.65f - origin).normalized;
                var shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
                shot.speed = Phase == 2 ? 8 : 6.5f;
                shot.Launch(aim);
                yield return new WaitForSeconds(Phase == 2 ? .18f : .28f);
            }
        }
        IEnumerator Pillars()
        {
            CurrentAttack = "Pillars"; FacePlayer(); animator.SetTrigger("Atk");
            image.color = new Color(.75f,.5f,1);
            game.Notify("CỘT BĂNG • Rời khỏi các vùng đỏ", 1.7f);
            yield return new WaitForSeconds(.25f);
            if (!CanAttack) yield break;
            float center = Mathf.Clamp(game.player.transform.position.x, -7.2f, 7.2f);
            int count = Phase == 2 ? 5 : 3;
            for (int i = 0; i < count; i++)
            {
                float x = center + (i - count/2)*2.6f;
                if (x < leftLimit || x > rightLimit) continue;
                var pillar = Instantiate(pillarPrefab, new Vector3(x,0,0), Quaternion.identity);
                pillar.warningSeconds = Phase == 2 ? .8f : 1.05f;
            }
            yield return new WaitForSeconds(1.85f);
        }
        IEnumerator Dash()
        {
            CurrentAttack = "Dash";
            int direction = FacePlayer();
            image.color = new Color(1,.4f,.5f); animator.SetTrigger("Atk");
            game.Notify("LAO TỚI • Nhảy qua boss!", 1.4f);
            yield return new WaitForSeconds(.75f);
            if (!CanAttack) yield break;
            dashHitbox.offset = new Vector2(direction * .65f, 1f);
            dashHitbox.enabled = true; animator.SetBool("Run", true);
            float end = Time.time + (Phase == 2 ? .85f : .7f);
            while (Time.time < end && CanAttack)
            {
                if ((direction < 0 && body.position.x <= leftLimit) || (direction > 0 && body.position.x >= rightLimit)) break;
                body.linearVelocityX = direction * (Phase == 2 ? 11 : 9);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocityX = 0; dashHitbox.enabled = false;
        }
        void Update()
        {
            if (IsDead) return;
            if (Time.time < flashUntil) image.color = Color.white;
            else image.color = CurrentAttack == "Dash" ? new Color(1,.4f,.5f)
                : CurrentAttack == "Volley" ? new Color(.35f,1,1)
                : CurrentAttack == "Pillars" ? new Color(.75f,.5f,1) : frost;
        }
        void OnTriggerEnter2D(Collider2D other) { CheckSword(other); }
        void OnTriggerStay2D(Collider2D other) { CheckSword(other); }
        void CheckSword(Collider2D other)
        {
            if (other.CompareTag("PlayerSword"))
                other.GetComponent<PlayerSwordHitbox>()?.TryHit(this);
        }
        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || game == null || !game.IsPlaying || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount); flashUntil = Time.time + .1f;
            if (IsDead)
            {
                StopAllCoroutines(); CurrentAttack = "Dead"; dashHitbox.enabled = false;
                body.linearVelocity = Vector2.zero; body.simulated = false; image.color = frost;
                animator.ResetTrigger("Atk"); animator.ResetTrigger("Hurt"); animator.SetTrigger("Die");
                game.BossDefeated();
            }
            else
            {
                if (CurrentAttack == "Rest") animator.SetTrigger("Hurt");
                if (Phase == 2 && !enraged)
                { enraged = true; game.Notify("GIAI ĐOẠN 2 • Băng dữ dội hơn!", 3); }
            }
            game.RefreshHud();
        }
    }
}
