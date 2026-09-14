using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;
using Sapphire.Presentation.Movement;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Wires the 4 skill bar buttons (mouse click or 1-4 keys) to
    /// SkillCatalog entries. This slice has no MP/cooldown resource and no
    /// monsters to hit - casting only shows the tile range indicator (for
    /// skills that have one) and plays the cosmetic cast feedback.
    /// </summary>
    public class SkillBarController : MonoBehaviour
    {
        [SerializeField] private Button[] skillButtons = new Button[4];
        [SerializeField] private PlayerGridController player;
        [SerializeField] private SkillRangeIndicator rangeIndicator;
        [SerializeField] private SkillCastFeedback castFeedback;

        private void Awake()
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null)
                {
                    continue;
                }

                int skillIndex = i;
                skillButtons[i].onClick.AddListener(() => CastSkill(skillIndex));
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                CastSkill(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                CastSkill(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                CastSkill(2);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                CastSkill(3);
            }
        }

        public void CastSkill(int index)
        {
            if (index < 0 || index >= SkillCatalog.All.Length)
            {
                return;
            }

            if (player == null || player.Mover == null)
            {
                return;
            }

            SkillDefinition skill = SkillCatalog.All[index];
            GridCoord origin = player.Mover.Position;

            if (rangeIndicator != null && skill.RangeShape != SkillRangeShape.None)
            {
                var tiles = skill.RangeShape == SkillRangeShape.Radius
                    ? SkillRangeCalculator.TilesInRadius(origin, skill.RangeTiles)
                    : SkillRangeCalculator.TilesInLine(origin, player.Mover.Facing, skill.RangeTiles);

                rangeIndicator.Show(tiles);
            }

            castFeedback?.PlayCast(skill.DisplayName);
        }
    }
}
