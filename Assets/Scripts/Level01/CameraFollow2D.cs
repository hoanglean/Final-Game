using UnityEngine;

namespace OooAdventure
{
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float left = -5, right = 115, bottom = -2, top = 12;
        public bool fitWholeArena;
        public float arenaPadding = .5f;
        Vector3 velocity;
        void LateUpdate()
        {
            if (fitWholeArena) { FrameArena(); return; }
            if (target == null) return;
            var camera = GetComponent<Camera>();
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            float x = Mathf.Clamp(target.position.x + 2, left + halfWidth, right - halfWidth);
            float y = Mathf.Clamp(target.position.y + 3.2f, bottom + halfHeight, top - halfHeight);
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(x, y, -10), ref velocity, .16f, 100, Time.unscaledDeltaTime);
        }
        void FrameArena()
        {
            var camera = GetComponent<Camera>();
            camera.orthographicSize = Mathf.Max((top-bottom)*.5f, (right-left)*.5f/camera.aspect) + arenaPadding;
            transform.position = new Vector3((left+right)*.5f, (bottom+top)*.5f, -10);
        }
        public void Snap()
        { if (fitWholeArena) { FrameArena(); return; } velocity = Vector3.zero; transform.position = new Vector3(target.position.x, target.position.y + 3, -10); }
    }
}
