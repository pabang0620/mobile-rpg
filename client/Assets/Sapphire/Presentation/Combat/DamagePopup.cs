using System.Collections;
using UnityEngine;

namespace Sapphire.Presentation.Combat
{
    public class DamagePopup : MonoBehaviour
    {
        public static void Spawn(Vector3 worldPosition, int damage)
        {
            var go = new GameObject("DamagePopup", typeof(TextMesh), typeof(MeshRenderer));
            go.transform.position = worldPosition + Vector3.up * 0.5f;

            var tm = go.GetComponent<TextMesh>();
            tm.text = damage.ToString();
            tm.characterSize = 0.15f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.2f, 0.1f);
            tm.fontStyle = FontStyle.Bold;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 300;

            var popup = go.AddComponent<DamagePopup>();
            popup.StartCoroutine(popup.FloatAndFade());
        }

        private IEnumerator FloatAndFade()
        {
            float duration = 0.8f;
            float elapsed = 0f;
            Vector3 start = transform.position;
            var tm = GetComponent<TextMesh>();
            Color startColor = tm.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = start + Vector3.up * (t * 0.8f);
                float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
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
