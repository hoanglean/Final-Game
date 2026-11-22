using UnityEngine;

namespace OooAdventure
{
    // Visibility messages belong on Visual, alongside its SpriteRenderer.
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyVisibility : MonoBehaviour
    {
        public EnemyController enemy;

        void Awake()
        { if (enemy == null) enemy = GetComponentInParent<EnemyController>(); }

        void OnEnable()
        { if (enemy != null) enemy.SetVisible(GetComponent<SpriteRenderer>().isVisible); }

        void OnBecameVisible()
        { if (enemy != null) enemy.SetVisible(true); }

        void OnBecameInvisible()
        { if (enemy != null) enemy.SetVisible(false); }

        void OnDisable()
        { if (enemy != null) enemy.SetVisible(false); }
    }
}
