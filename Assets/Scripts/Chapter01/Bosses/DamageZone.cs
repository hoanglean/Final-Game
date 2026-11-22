using UnityEngine;

namespace OooAdventure
{
    public class DamageZone : MonoBehaviour
    {
        public int damage = 18;
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }
        void Hit(Collider2D other)
        {
            if (Level01Game.Instance == null || !Level01Game.Instance.IsPlaying) return;
            // Only Finn's body counts; touching AtkRange must not hurt its owner.
            var player = other.GetComponent<FinnController>();
            if (player != null) player.TakeDamage(damage, transform.position);
        }
    }
}
