using UnityEngine;
using UnityEngine.InputSystem;

namespace OooAdventure
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class FinnController : MonoBehaviour
    {
        public float moveSpeed = 5, jumpSpeed = 13.5f;
        public int maxHealth = 100;
        public LayerMask groundMask, enemyMask;
        public Animator animator;
        public SpriteRenderer image;
        [InspectorName("Atk Range")] public Transform attackPoint;
        public PlayerSwordHitbox sword;
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;
        public bool Grounded { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public Rigidbody2D Body { get; private set; }
        float movement, jumpUntil, lastGrounded = float.NegativeInfinity, nextAttack, hurtUntil, invulnerableUntil;
        BoxCollider2D bodyCollider;
        internal BoxCollider2D BodyCollider => bodyCollider;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<BoxCollider2D>();
            Health = maxHealth;
            sword.Close();
        }
        void Update()
        {
            if (Level01Game.Instance == null || !Level01Game.Instance.IsPlaying || IsDead) return;
            movement = Input.GetAxisRaw("Horizontal");
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (movement == 0) movement = ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1 : 0)
                    - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1 : 0);
                if (keys.spaceKey.wasPressedThisFrame) jumpUntil = Time.time + .12f;
                if (keys.enterKey.wasPressedThisFrame || keys.jKey.wasPressedThisFrame) TryAttack();
            }
            if (movement != 0 && Time.time > hurtUntil)
            {
                FacingRight = movement > 0;
                image.flipX = !FacingRight;
                attackPoint.localPosition = new Vector3(FacingRight ? .8f : -.8f, .65f, 0);
            }
            image.color = Time.time < invulnerableUntil && Mathf.FloorToInt(Time.time * 16) % 2 == 0
                ? new Color(1, 1, 1, .35f) : Color.white;
            UpdateAnimatorParameters();
        }
        void FixedUpdate()
        {
            if (IsDead || Level01Game.Instance == null || !Level01Game.Instance.IsPlaying) return;
            var bounds = bodyCollider.bounds;
            Grounded = Body.linearVelocityY <= .1f && Physics2D.OverlapBox(
                new Vector2(bounds.center.x, bounds.min.y - .035f), new Vector2(bounds.size.x * .72f, .09f), 0, groundMask);
            if (Grounded) lastGrounded = Time.time;
            if (Time.time > hurtUntil) Body.linearVelocityX = movement * moveSpeed;
            if (jumpUntil > Time.time && Time.time - lastGrounded < .1f && Time.time > hurtUntil)
            {
                Body.linearVelocityY = jumpSpeed; jumpUntil = 0; lastGrounded = -10; Grounded = false;
            }
            if (transform.position.y < -6) TakeDamage(maxHealth, transform.position);
        }
        public void TryAttack()
        {
            if (IsDead || Time.time < nextAttack || Time.time < hurtUntil || !Level01Game.Instance.IsPlaying) return;
            nextAttack = Time.time + .4f;
            sword.BeginSwing(); animator.SetTrigger("Atk");
            Level01Game.Instance.PlayTone(600, .06f);
        }
        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || Time.time < invulnerableUntil || Level01Game.Instance == null || !Level01Game.Instance.IsPlaying) return;
            Health = Mathf.Max(0, Health - amount);
            sword.Close();
            invulnerableUntil = Time.time + 1f; hurtUntil = Time.time + .22f;
            Level01Game.Instance.RefreshHud(); Level01Game.Instance.PlayTone(180, .12f);
            if (IsDead)
            {
                Body.linearVelocity = Vector2.zero;
                Body.gravityScale = 0;
                Body.bodyType = RigidbodyType2D.Kinematic;
                bodyCollider.enabled = false;
                image.color = Color.white;
                animator.ResetTrigger("Atk"); animator.ResetTrigger("Hurt"); animator.SetTrigger("Die");
                Level01Game.Instance.PlayerDied();
            }
            else
            {
                Body.linearVelocity = new Vector2(transform.position.x >= from.x ? 4 : -4, 4);
                animator.ResetTrigger("Atk"); animator.SetTrigger("Hurt");
            }
        }
        public void Heal(int amount)
        { if (!IsDead) { Health = Mathf.Min(maxHealth, Health + amount); Level01Game.Instance.RefreshHud(); } }
        void UpdateAnimatorParameters()
        {
            animator.SetBool("Run", Mathf.Abs(Body.linearVelocityX) > .1f);
            animator.SetBool("Grounded", Grounded);
            animator.SetFloat("VerticalSpeed", Body.linearVelocityY);
        }
    }
}
