using System.Collections;
using UnityEngine;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Minimal "something happened" feedback for a skill cast: a brief
    /// sprite color flash plus a floating skill-name label that rises and
    /// fades. Purely cosmetic - this slice has no damage/monsters, so there
    /// is nothing else to react to yet.
    /// </summary>
    public class SkillCastFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 1f, 0.6f, 1f);
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float labelRiseDistance = 0.6f;
        [SerializeField] private float labelDuration = 0.7f;

        private Color baseColor;
        private Coroutine flashRoutine;

        private void Awake()
        {
            if (targetRenderer != null)
            {
                baseColor = targetRenderer.color;
            }
        }

        public void PlayCast(string skillName)
        {
            if (targetRenderer != null)
            {
                if (flashRoutine != null)
                {
                    StopCoroutine(flashRoutine);
                    targetRenderer.color = baseColor;
                }

                flashRoutine = StartCoroutine(FlashRoutine());
            }

            SpawnLabel(skillName);
        }

        private IEnumerator FlashRoutine()
        {
            targetRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            targetRenderer.color = baseColor;
            flashRoutine = null;
        }

        private void SpawnLabel(string text)
        {
            var labelGo = new GameObject("SkillCastLabel", typeof(TextMesh));
            labelGo.transform.position = transform.position + new Vector3(0f, 0.9f, 0f);

            var mesh = labelGo.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = 0.05f;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;

            StartCoroutine(RiseAndFade(labelGo));
        }

        private IEnumerator RiseAndFade(GameObject label)
        {
            var mesh = label.GetComponent<TextMesh>();
            Vector3 start = label.transform.position;
            Vector3 end = start + new Vector3(0f, labelRiseDistance, 0f);
            float elapsed = 0f;

            while (elapsed < labelDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / labelDuration);
                label.transform.position = Vector3.Lerp(start, end, t);

                Color color = mesh.color;
                color.a = 1f - t;
                mesh.color = color;

                yield return null;
            }

            Destroy(label);
        }
    }
}
