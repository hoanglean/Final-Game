using System.Collections;
using UnityEngine;

namespace OooAdventure
{
    public class IcePillarHazard : MonoBehaviour
    {
        public GameObject warning, ice;
        public Collider2D damageCollider;
        public float warningSeconds = .95f, activeSeconds = .7f;
        public bool IsActive => damageCollider.enabled;

        IEnumerator Start()
        {
            damageCollider.enabled = false; ice.SetActive(false); warning.SetActive(true);
            yield return new WaitForSeconds(warningSeconds);
            warning.SetActive(false); ice.SetActive(true); damageCollider.enabled = true;
            yield return new WaitForSeconds(activeSeconds);
            damageCollider.enabled = false;
            Destroy(gameObject);
        }
    }
}
