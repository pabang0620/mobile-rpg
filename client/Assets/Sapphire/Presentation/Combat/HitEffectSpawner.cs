using System.Collections;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Sapphire.Presentation.Combat
{
    public class HitEffectSpawner : MonoBehaviour
    {
        private static Sprite sparkSprite;

        public static void Spawn(Vector3 worldPosition)
        {
            if (sparkSprite == null)
            {
#if UNITY_EDITOR
                sparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sapphire/Art/UI/HitSpark.png");
#endif
            }

            if (sparkSprite == null) return;

            var go = new GameObject("HitSpark", typeof(SpriteRenderer));
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.sortingOrder = 3000;

            var spawner = go.AddComponent<HitEffectSpawner>();
            spawner.StartCoroutine(spawner.AnimateSpark(sr));
        }

        private IEnumerator AnimateSpark(SpriteRenderer sr)
        {
            float duration = 0.15f;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one * 0.2f;
            Vector3 endScale = Vector3.one * 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sqrt(t));
                sr.color = new Color(1f, 1f, 1f, 1f - t);
                
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
