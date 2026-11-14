using UnityEngine;

namespace OooAdventure
{
    public class IceProjectile : MonoBehaviour
    {
        public float speed = 5;
        public int damage = 20;
        public Animator animator;
        public float impactDuration = .22f;
        bool impacted;
        public void Launch(Vector2 direction)
        {
            GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
            if (animator != null && direction.sqrMagnitude > 0)
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            Destroy(gameObject, 5);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (impacted || Level01Game.Instance == null || !Level01Game.Instance.IsPlaying) return;
            var player = other.GetComponent<FinnController>();
            if (player != null) { player.TakeDamage(damage, transform.position); Impact(); }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) Impact();
        }
        void Impact()
        {
            impacted = true;
            if (animator == null) { Destroy(gameObject); return; }
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.simulated = false;
            animator.SetTrigger("Hit");
            Destroy(gameObject, impactDuration);
        }
    }
}
