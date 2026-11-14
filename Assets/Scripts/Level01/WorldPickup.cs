using UnityEngine;
using UnityEngine.InputSystem;

namespace OooAdventure
{
    public class WorldPickup : MonoBehaviour
    {
        public enum Kind { ClockShard = 0, Healing = 1, Sign = 3, Exit = 4, Hazard = 5 }
        public Kind kind;
        public Transform visual;
        [TextArea] public string message;
        bool activated;
        Vector3 baseVisual;
        void Start() { if (visual != null) baseVisual = visual.localPosition; }
        void Update()
        {
            var game = Level01Game.Instance;
            if (game == null || !game.IsPlaying) return;
            if (visual != null && (kind == Kind.ClockShard || kind == Kind.Healing))
                visual.localPosition = baseVisual + Vector3.up * (Mathf.Sin(Time.time * 3 + transform.position.x) * .12f);
            if ((kind == Kind.Sign || kind == Kind.Exit) && Vector2.Distance(transform.position, game.player.transform.position) < 2.3f)
            {
                game.Prompt(kind == Kind.Exit ? "[E] Sửa cổng thời gian" : "[E] Đọc lời nhắn");
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (kind == Kind.Exit) game.TryFinish(); else game.Notify(message, 7);
                }
            }
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<FinnController>() == null || !Level01Game.Instance.IsPlaying) return;
            var game = Level01Game.Instance;
            if (kind == Kind.Hazard) { game.player.TakeDamage(100, transform.position); return; }
            if (activated) return;
            if (kind == Kind.ClockShard)
            { activated = true; game.CollectShard(); Destroy(gameObject); }
            else if (kind == Kind.Healing)  
            {
                if (game.player.Health == game.player.maxHealth) return;
                activated = true; game.player.Heal(25); game.PlayTone(800, .1f); Destroy(gameObject);
            }
        }
    }
}
