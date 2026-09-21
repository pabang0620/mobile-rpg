using System.Collections;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Sapphire.Presentation.Combat
{
    public class ItemDrop : MonoBehaviour
    {
        private static Sprite dropSprite;

        public static void Spawn(Vector3 worldPosition)
        {
            if (dropSprite == null)
            {
#if UNITY_EDITOR
                dropSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sapphire/Art/UI/LevelBadgeHex.png");
#endif
            }

            var go = new GameObject("ItemDrop", typeof(SpriteRenderer), typeof(CircleCollider2D));
            go.transform.position = worldPosition;
            go.transform.localScale = Vector3.one * 0.12f;
            
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = dropSprite;
            sr.color = new Color(1f, 0.8f, 0.2f); // Gold coin
            sr.sortingOrder = -100; // On the floor
            
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = 2f;
            col.isTrigger = true;
            
            var drop = go.AddComponent<ItemDrop>();
            drop.StartCoroutine(drop.BounceRoutine(worldPosition));
        }

        private IEnumerator BounceRoutine(Vector3 start)
        {
            Vector3 target = start + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, -0.2f), 0);
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float jump = Mathf.Sin(t * Mathf.PI) * 0.6f;
                transform.position = Vector3.Lerp(start, target, t) + new Vector3(0, jump, 0);
                
                yield return null;
            }
            transform.position = target;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatController>();
            if (player != null)
            {
                DamagePopup.Spawn(transform.position, 0, "+10 Gold");
                Destroy(gameObject);
            }
        }
    }
}

