using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    /// <summary>
    /// Generic horizontal-fill gauge (2026-09-16 Phase 2: generalized from the
    /// old HP-only HealthBarView so the same component drives both the HP bar
    /// and the new MP bar - see VillageHubUiBuilder.BuildGauge). This slice has
    /// no stat system yet (no real HP/MP values on the player), so every gauge
    /// is built fixed at 100% fill and nothing currently calls
    /// <see cref="SetFillAmount"/>. When a future combat/resource system
    /// introduces real values, wire it to call
    /// SetFillAmount(currentValue / maxValue) - no other change to this
    /// component should be needed.
    /// </summary>
    public class GaugeView : MonoBehaviour
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
