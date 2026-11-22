using System.Collections.Generic;
using UnityEngine;

namespace OooAdventure
{
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class PlayerSwordHitbox : MonoBehaviour
    {
        public FinnController owner;
        public int damage = 35;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        BoxCollider2D hitbox;

        void Awake()
        {
            hitbox = GetComponent<BoxCollider2D>();
            if (owner == null) owner = GetComponentInParent<FinnController>();
            Close();
        }

        public void BeginSwing()
        { Close(); hitThisSwing.Clear(); }

        public void Close()
        {
            if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
            hitbox.enabled = false;
        }

        // The receiver checks PlayerSword. Animation controls the active hit frames.
        public void TryHit(IDamageable target)
        {
            var game = Level01Game.Instance;
            if (!isActiveAndEnabled || hitbox == null || !hitbox.enabled || owner == null || owner.IsDead || game == null || !game.IsPlaying) return;
            var component = target as Component;
            if (component == null || (owner.enemyMask.value & (1 << component.gameObject.layer)) == 0 || !hitThisSwing.Add(target)) return;
            target.TakeDamage(damage, owner.transform.position);
        }

        void OnDisable() { Close(); }
    }
}
