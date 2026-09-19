using System.Collections;
using UnityEngine;

namespace Sapphire.Presentation.Combat
{
    public class DamagePopup : MonoBehaviour
    {
        private TextMesh mainTm;
        private TextMesh[] outlines;

        public static void Spawn(Vector3 worldPosition, int damage, string specialText = null)
        {
            var go = new GameObject("DamagePopup");
            float randomX = Random.Range(-0.3f, 0.3f);
            go.transform.position = worldPosition + new Vector3(randomX, 0.5f, 0f);

            var popup = go.AddComponent<DamagePopup>();
            popup.Init(specialText != null ? specialText : damage.ToString(), specialText != null);
        }

        private void Init(string text, bool isSpecial)
        {
            Color mainColor = isSpecial ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.2f, 0.1f);
            int fontSize = isSpecial ? 36 : 48;

            mainTm = CreateText("MainText", transform, Vector3.zero, mainColor, fontSize, 300);
            mainTm.text = text;

            outlines = new TextMesh[4];
            Vector3[] offsets = {
                new Vector3(-0.02f, 0, 0),
                new Vector3(0.02f, 0, 0),
                new Vector3(0, -0.02f, 0),
                new Vector3(0, 0.02f, 0)
            };

            for (int i = 0; i < 4; i++)
            {
                outlines[i] = CreateText("Outline" + i, transform, offsets[i], Color.black, fontSize, 299);
                outlines[i].text = text;
            }

            StartCoroutine(FloatAndFade(isSpecial));
        }

        private TextMesh CreateText(string name, Transform parent, Vector3 localPos, Color color, int fontSize, int order)
        {
            var go = new GameObject(name, typeof(TextMesh), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var tm = go.GetComponent<TextMesh>();
            tm.characterSize = 0.15f;
            tm.fontSize = fontSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = order;
            return tm;
        }

        private IEnumerator FloatAndFade(bool isSpecial)
        {
            float duration = isSpecial ? 1.5f : 0.7f;
            float elapsed = 0f;
            Vector3 start = transform.position;
            Color startColor = mainTm.color;
            float driftX = isSpecial ? 0f : Random.Range(-0.5f, 0.5f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float yOffset = isSpecial ? (t * 1.5f) : Mathf.Lerp(0f, 1.2f, Mathf.Sin(t * Mathf.PI / 2f));
                transform.position = start + new Vector3(driftX * t, yOffset, 0f);
                
                float alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                mainTm.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                foreach (var o in outlines) o.color = new Color(0f, 0f, 0f, alpha);
                
                float punch = t < 0.15f ? Mathf.Lerp(1f, 1.4f, t / 0.15f) : Mathf.Lerp(1.4f, 1f, (t - 0.15f) / 0.35f);
                if (t > 0.5f) punch = Mathf.Lerp(1f, 0.8f, (t - 0.5f) / 0.5f);
                transform.localScale = new Vector3(punch, punch, 1f);
                
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
