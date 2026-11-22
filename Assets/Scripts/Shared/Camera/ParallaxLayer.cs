using UnityEngine;

namespace OooAdventure
{
    public class ParallaxLayer : MonoBehaviour
    {
        public float factor = .25f;
        Vector3 origin, cameraOrigin;
        Transform view;
        void Start() { origin = transform.position; view = Camera.main.transform; cameraOrigin = view.position; }
        void LateUpdate()
        { if (view != null) transform.position = origin + new Vector3((view.position.x - cameraOrigin.x) * factor, 0, 0); }
    }
}
