using System.Collections;
using UnityEngine;

namespace OooAdventure
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
        public enum Kind { Mushroom, Goblin, FlyingEye, Guardian, GroundPatrol, ExplodingDrone, CombatBoss }
        public Kind kind;
        public float patrolRange = 3, speed = 1.4f;
        public int maxHealth = 60;
        public Collider2D attackRange;
        [Min(1)] public int attackDamage = 10;
        [Min(.9f)] public float attackCooldown = 1.2f;
        public float shotCooldown = 1.8f;
        public Transform shotOrigin;
        public LayerMask groundMask;
        public Animator animator;
        public SpriteRenderer image;
        public EnemyProjectile projectilePrefab;
        public WorldPickup shardPrefab;
        public bool artworkFacesLeft;
        [Min(0)] public float meleeWindup = .4f, shotWindup = .3f;
        public UnityEngine.Events.UnityEvent onDefeated;
        bool Flies => kind == Kind.FlyingEye || kind == Kind.ExplodingDrone;
        public bool IsDead => health <= 0;
        public int Health => health;
        public bool Active { get; private set; }
        Rigidbody2D body;
        Collider2D bodyCollider;
        int health, direction = -1;
        Vector2 origin;
        float nextAttack, actionUntil, hurtUntil;

        void Awake()
        { body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<Collider2D>(); health = maxHealth; origin = body.position; SetVisible(image != null && image.isVisible); }
        public void SetVisible(bool visible)
        {
            Active = visible && !IsDead;
            if (Active || body == null) return;
            StopAllCoroutines(); actionUntil = 0;
            body.linearVelocityX = 0;
            if (Flies) body.linearVelocityY = 0;
        }
        void Update()
        { animator.SetBool("Run", Mathf.Abs(body.linearVelocityX) > .1f); }
        void FixedUpdate()
        {
            var game = Level01Game.Instance;
            if (IsDead || (game != null && !game.IsPlaying)) return;
            var player = game != null ? game.player : null;
            if (!Active) { body.linearVelocityX = 0; return; }
            if (Time.time < hurtUntil) return;
            if (Time.time < actionUntil)
            {
                body.linearVelocityX = 0;
                if (Flies) body.linearVelocityY = 0;
                return;
            }
            if (player != null && kind != Kind.Guardian && Time.time >= nextAttack && IsInAttackRange(player))
            {
                FacePlayer(player);
                StartCoroutine(MeleeAttack(player));
                return;
            }
            if (player != null && !player.IsDead && projectilePrefab != null && shotOrigin != null && Time.time >= nextAttack)
            {
                Vector2 distance = (Vector2)player.transform.position - body.position;
                if (Mathf.Abs(distance.x) < 8 && Mathf.Abs(distance.y) < 2)
                { FacePlayer(player); StartCoroutine(Fire()); return; }
            }
            if (kind == Kind.Guardian) { body.linearVelocityX = 0; return; }
            if (body.position.x < origin.x - patrolRange) direction = 1;
            if (body.position.x > origin.x + patrolRange) direction = -1;
            if (Flies)
            { body.linearVelocity = new Vector2(direction * speed, (origin.y + Mathf.Sin(Time.time * 2) * .4f - body.position.y) * 3); }
            else
            {
                float probeDistance = Mathf.Max(.65f, bodyCollider.bounds.extents.x + .2f);
                bool floor = Physics2D.Raycast(body.position + new Vector2(direction * probeDistance, .3f), Vector2.down, .75f, groundMask);
                bool wall = Physics2D.Raycast(body.position + Vector2.up * .5f, Vector2.right * direction, probeDistance, groundMask);
                if (!floor || wall) direction *= -1;
                body.linearVelocityX = direction * speed;
            }
            image.flipX = (direction < 0) != artworkFacesLeft;
        }
        bool IsInAttackRange(FinnController player)
        {
            return attackRange != null && attackRange.isActiveAndEnabled && !player.IsDead
                && attackRange.Distance(player.BodyCollider).isOverlapped;
        }

        void FacePlayer(FinnController player)
        {
            direction = player.transform.position.x >= body.position.x ? 1 : -1;
            image.flipX = (direction < 0) != artworkFacesLeft;
        }

        IEnumerator MeleeAttack(FinnController player)
        {
            const float attackDuration = .9f;
            nextAttack = Time.time + Mathf.Max(attackDuration, attackCooldown);
            actionUntil = Time.time + attackDuration;
            body.linearVelocityX = 0;
            if (Flies) body.linearVelocityY = 0;
            animator.SetTrigger("Atk");
            yield return new WaitForSeconds(meleeWindup);

            var game = Level01Game.Instance;
            if (!isActiveAndEnabled || IsDead || !Active || game == null || !game.IsPlaying) yield break;
            if (kind == Kind.ExplodingDrone)
            {
                if (IsInAttackRange(player)) player.TakeDamage(attackDamage, attackRange.bounds.center);
                TakeDamage(maxHealth, transform.position);
                yield break;
            }
            if (!IsInAttackRange(player)) yield break;
            float targetX = player.transform.position.x - body.position.x;
            if (targetX * direction >= 0) player.TakeDamage(attackDamage, attackRange.bounds.center);
        }

        IEnumerator Fire()
        {
            nextAttack = Time.time + shotCooldown;
            actionUntil = Time.time + Mathf.Max(.7f, shotWindup + .2f); body.linearVelocityX = 0;
            animator.SetTrigger(kind == Kind.CombatBoss ? "Shoot" : "Atk");
            yield return new WaitForSeconds(shotWindup);
            if (IsDead || !Active || Level01Game.Instance == null || !Level01Game.Instance.IsPlaying) yield break;
            var player = Level01Game.Instance.player;
            var muzzle = shotOrigin.localPosition;
            muzzle.x = direction * Mathf.Abs(muzzle.x);
            shotOrigin.localPosition = muzzle;
            var shot = Instantiate(projectilePrefab, shotOrigin.position, Quaternion.identity);
            Vector2 aim = (Vector2)player.transform.position + Vector2.up * .6f - (Vector2)shotOrigin.position;
            shot.Launch(aim.normalized);
        }
        void OnTriggerEnter2D(Collider2D other) { CheckSword(other); }
        void OnTriggerStay2D(Collider2D other) { CheckSword(other); }
        void CheckSword(Collider2D other)
        {
            if (other.CompareTag("PlayerSword") && bodyCollider.Distance(other).isOverlapped)
                other.GetComponent<PlayerSwordHitbox>()?.TryHit(this);
        }
        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || Time.time < hurtUntil) return;
            health = Mathf.Max(0, health - amount); StopAllCoroutines();
            hurtUntil = Time.time + .18f; actionUntil = hurtUntil;
            body.linearVelocityX = transform.position.x > from.x ? 3 : -3;
            if (IsDead)
            {
                Active = false;
                body.linearVelocity = Vector2.zero; body.simulated = false;
                animator.ResetTrigger("Atk"); animator.ResetTrigger("Hurt"); animator.SetTrigger("Die");
                if (kind == Kind.Guardian && shardPrefab != null)
                {
                    Instantiate(shardPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
                    Level01Game.Instance.Notify("Kẻ giữ cổng đã bị hạ! Nhặt bánh răng cuối cùng.");
                }
                onDefeated?.Invoke();
                Destroy(gameObject, 1.2f);
            }
            else { animator.ResetTrigger("Atk"); animator.SetTrigger("Hurt"); }
        }
        void OnDrawGizmosSelected()
        {
            if (attackRange != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(attackRange.bounds.center, attackRange.bounds.size);
            }
            Gizmos.color = Color.cyan;
            var center = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.DrawLine(center + Vector3.left * patrolRange, center + Vector3.right * patrolRange);
        }
    }
}
