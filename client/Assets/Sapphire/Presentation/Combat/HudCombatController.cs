using UnityEngine;
using Sapphire.Presentation.UI;

namespace Sapphire.Presentation.Combat
{
    public class HudCombatController : MonoBehaviour
    {
        [SerializeField] private GaugeView hpGauge;
        [SerializeField] private GaugeView mpGauge;

        private PlayerCombatController activePlayer;

        private void Update()
        {
            if (activePlayer == null || !activePlayer.gameObject.activeInHierarchy)
            {
                FindActivePlayer();
            }

            if (activePlayer != null)
            {
                if (hpGauge != null && activePlayer.Health != null)
                {
                    hpGauge.SetFillAmount((float)activePlayer.Health.CurrentHp / activePlayer.Health.MaxHp);
                }
                
                if (mpGauge != null && activePlayer.Mana != null)
                {
                    mpGauge.SetFillAmount((float)activePlayer.Mana.CurrentMp / activePlayer.Mana.MaxMp);
                }
            }
        }

        private void FindActivePlayer()
        {
            var players = FindObjectsOfType<PlayerCombatController>();
            foreach (var p in players)
            {
                if (p.gameObject.activeInHierarchy)
                {
                    activePlayer = p;
                    return;
                }
            }
        }
    }
}
