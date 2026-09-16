using System.Collections;
using UnityEngine;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Minimal "something happened" feedback for a skill cast: a brief
    /// sprite color flash. Purely cosmetic - this slice has no
    /// damage/monsters, so there is nothing else to react to yet.
    ///
    /// 2026-09-16: removed the floating skill-name TextMesh label that used
    /// to spawn above the caster's head on every cast (user report: "스킬
    /// 쓸 때 위에 캐릭터 위에 텍스트 나오는거도 없애줘") - PlayCast's
    /// `skillName` parameter is now unused by this class but kept so
    /// RadialSkillMenu's call sites (castFeedback?.PlayCast(skill.DisplayName))
    /// don't need to change; the flash-only feedback is unaffected.
    /// </summary>
    public class SkillCastFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 1f, 0.6f, 1f);
        [SerializeField] private float flashDuration = 0.15f;

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
        }

        private IEnumerator FlashRoutine()
        {
            targetRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            targetRenderer.color = baseColor;
            flashRoutine = null;
        }
    }
}
