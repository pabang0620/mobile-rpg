using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    /// <summary>
    /// Top-left HP bar (2026-09-15 gold-tier UI replacement, built from
    /// HealthBarFrameGold.png's 2 cells - see VillageHubUiBuilder.BuildHealthBar).
    /// This slice has no combat/damage system and no HP resource on the player
    /// yet, so the bar is a visual scaffold only: it is shown fixed at 100%
    /// fill and nothing currently calls <see cref="SetFillAmount"/>. When a
    /// future combat system introduces a real HP value, wire it to call
    /// SetFillAmount(currentHp / maxHp) - no other change to this component
    /// should be needed.
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private void Awake()
        {
            SetFillAmount(1f);
        }

        public void SetFillAmount(float normalizedAmount)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(normalizedAmount);
            }
        }
    }
}
