using System.Collections;
using UnityEngine;

namespace Sapphire.Presentation.Combat
{
    public class DamagePopup : MonoBehaviour
    {
        public static void Spawn(Vector3 worldPosition, int damage, string specialText = null)
        {
            var go = new GameObject("DamagePopup", typeof(TextMesh), typeof(MeshRenderer));
            
            float randomX = Random.Range(-0.3f, 0.3f);
            go.transform.position = worldPosition + new Vector3(randomX, 0.5f, 0f);

            var tm = go.GetComponent<TextMesh>();
            tm.text = specialText != null ? specialText : damage.ToString();
            tm.characterSize = 0.15f;
            tm.fontSize = specialText != null ? 36 : 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = specialText != null ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.2f, 0.1f);
            tm.fontStyle = FontStyle.Bold;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 300;

            var popup = go.AddComponent<DamagePopup>();
            popup.StartCoroutine(popup.FloatAndFade(specialText != null));
        }

        private IEnumerator FloatAndFade(bool isSpecial)
        {
            float duration = isSpecial ? 1.5f : 0.7f;
            float elapsed = 0f;
            Vector3 start = transform.position;
            var tm = GetComponent<TextMesh>();
            Color startColor = tm.color;
            
            float driftX = isSpecial ? 0f : Random.Range(-0.5f, 0.5f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float yOffset = isSpecial ? (t * 1.5f) : Mathf.Lerp(0f, 1.2f, Mathf.Sin(t * Mathf.PI / 2f));
                
                transform.position = start + new Vector3(driftX * t, yOffset, 0f);
                
                float alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                tm.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                
                float punch = t < 0.15f ? Mathf.Lerp(1f, 1.4f, t / 0.15f) : Mathf.Lerp(1.4f, 1f, (t - 0.15f) / 0.35f);
                if (t > 0.5f) punch = Mathf.Lerp(1f, 0.8f, (t - 0.5f) / 0.5f);
                transform.localScale = new Vector3(punch, punch, 1f);
                
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
