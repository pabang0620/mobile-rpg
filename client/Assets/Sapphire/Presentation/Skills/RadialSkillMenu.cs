using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;
using Sapphire.Presentation.Movement;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Right-side radial action menu: a center "기본공격"(basic attack) button
    /// surrounded by an arc of skill buttons (SkillCatalog entries, currently
    /// 5: arcane bolt / frost wave / blink / shield / 질주). Each button is
    /// clickable or triggered by a key - J for the basic attack, 1-5 for the
    /// skills. Replaces the old bottom horizontal skill bar (2026-09-14 UI
    /// overhaul, see docs/DECISIONS.md) - button *layout* (the arc geometry)
    /// is built in the Editor-only VillageHubUiBuilder, this component only
    /// owns input wiring, same split as the old SkillBarController had.
    ///
    /// This slice has no MP/cooldown resource and no monsters to hit yet, so
    /// both the basic attack and every skill only show the tile range
    /// indicator (skills that have one) and the cosmetic cast feedback - no
    /// damage calculation. docs/planning/01_PRODUCT.md describes a future
    /// 3-hit basic-attack combo; that is out of scope for this slice.
    /// </summary>
    public class RadialSkillMenu : MonoBehaviour
    {
        private const float HasteDurationSeconds = 10f;
        private const string BasicAttackDisplayName = "기본공격";

        [SerializeField] private Button basicAttackButton;
        [SerializeField] private Button[] skillButtons = new Button[5];
        [SerializeField] private PlayerGridController player;
        [SerializeField] private SkillRangeIndicator rangeIndicator;
        [SerializeField] private SkillCastFeedback castFeedback;

        private void Awake()
        {
            if (basicAttackButton != null)
            {
                basicAttackButton.onClick.AddListener(CastBasicAttack);
            }

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

            if (keyboard.jKey.wasPressedThisFrame)
            {
                CastBasicAttack();
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
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
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                CastSkill(4);
            }
        }

        /// <summary>
        /// Cosmetic-only basic attack (see class doc) - no damage/target
        /// resolution yet, just the same cast feedback every skill gets.
        /// </summary>
        public void CastBasicAttack()
        {
            castFeedback?.PlayCast(BasicAttackDisplayName);
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

            if (skill.Id == SkillCatalog.HasteSkillId)
            {
                player.MoveAnimator?.ActivateSpeedBoost(HasteDurationSeconds);
            }

            castFeedback?.PlayCast(skill.DisplayName);
        }
    }
}
