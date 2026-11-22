using UnityEngine;

namespace OooAdventure
{
    public interface IDamageable
    {
        void TakeDamage(int amount, Vector2 from);
    }
}
