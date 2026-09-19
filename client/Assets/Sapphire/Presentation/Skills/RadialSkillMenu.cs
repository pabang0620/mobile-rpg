using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sapphire.Domain.Character;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;
using Sapphire.Presentation.Movement;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Right-side radial action menu: a center "기본공격"(basic attack) button
    /// surrounded by skill buttons (SkillCatalog.ForClass(characterClass)
    /// entries, 5 per class - mage: 마력쉴드/텔레포트/낙뢰/고드름/번개창;
    /// warrior: 돌진/회오리베기/방패막기/전쟁함성/대지강타). One
    /// RadialSkillMenu instance drives exactly one class (characterClass is
    /// set once at scene-build time); the VillageHub scene builds one
    /// instance per class and activates only the one matching the selected
    /// character (see SceneComposer). Each button is clickable or triggered
    /// by a key - J for the basic attack, 1-5 for the skills.
    /// Replaces the old bottom horizontal skill bar (2026-09-14 UI overhaul,
    /// see docs/DECISIONS.md) - button *layout* (2026-09-16: exactly 4 of the
    /// 5 buttons sit in a verified-non-overlapping fan, the 5th sits just
    /// outside it - see VillageHubUiBuilder.BuildRadialSkillMenu for the
    /// geometry and why) is built in the Editor-only VillageHubUiBuilder, this
    /// component only owns input wiring, same split as the old
    /// SkillBarController had.
    ///
    /// This slice has no MP/cooldown resource and no monsters to hit yet, so
    /// both the basic attack and every skill only show the tile range
    /// indicator (skills that have one) and the cosmetic cast feedback - no
    /// damage calculation. docs/planning/01_PRODUCT.md describes a future
    /// 3-hit basic-attack combo; that is out of scope for this slice.
    /// </summary>
    public class RadialSkillMenu : MonoBehaviour
    {
        private const string BasicAttackDisplayName = "기본공격";

        [SerializeField] private Button basicAttackButton;
        [SerializeField] private Button[] skillButtons = new Button[5];
        [SerializeField] private PlayerGridController player;
        [SerializeField] private SkillRangeIndicator rangeIndicator;
        [SerializeField] private SkillCastFeedback castFeedback;
        // Which class's SkillCatalog.ForClass(...) array + ISkillVfxPlayer
        // implementation this fan drives. Set once at scene-build time
        // (VillageHubSkillMenuBuilder.Build) via reflection, same convention
        // every other field here uses - defaults to Mage so any pre-existing
        // wiring that never sets this keeps behaving exactly as before this
        // field was added.
        [SerializeField] private CharacterClass characterClass = CharacterClass.Mage;
        private ISkillVfxPlayer skillVfx;
        // 2026-09-16 (attack-motion slice): resolved once from `player`
        // (SapphireSceneBuilder.BuildPlayer adds one SkillMotionPlayer per
        // player rig, sprites pre-wired from that class's *AttackGridSheet.png)
        // rather than serialized here - same reasoning ResolveSkillVfx uses
        // AddComponent/GetComponent lazily instead of a wired field, except
        // this one always exists on the player rig so a plain GetComponent in
        // Awake is enough.
        private SkillMotionPlayer motionPlayer;

        private SkillDefinition[] Skills => SkillCatalog.ForClass(characterClass);

        private void Awake()
        {
            if (player != null)
            {
                skillVfx = ResolveSkillVfx();
                motionPlayer = player.GetComponent<SkillMotionPlayer>();
            }

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
        /// Warrior additionally plays a real VFX slash (see
        /// WarriorSkillVfxPlayer.PlayBasicAttack) - mage has no basic-attack
        /// VFX method, so the `as` cast below is simply null and a no-op for
        /// mage, leaving mage's basic attack behavior completely unchanged.
        /// </summary>
        public void CastBasicAttack()
        {
            if (player != null && player.Mover != null && !player.Mover.IsMoving)
            {
                skillVfx = ResolveSkillVfx();
                (skillVfx as WarriorSkillVfxPlayer)?.PlayBasicAttack(player.Mover.Facing);
                motionPlayer?.PlayAttack(player.Mover.Facing);
                
                var combat = player.GetComponent<Sapphire.Presentation.Combat.PlayerCombatController>();
                if (combat != null)
                {
                    GridCoord target = player.Mover.Position + player.Mover.Facing.ToOffset();
                    combat.AttackTarget(target.X, target.Y, 1.0f);
                }
            }

            castFeedback?.PlayCast(BasicAttackDisplayName);
        }

        public void CastSkill(int index)
        {
            SkillDefinition[] skills = Skills;
            if (index < 0 || index >= skills.Length)
            {
                return;
            }

            if (player == null || player.Mover == null || player.Mover.IsMoving)
            {
                return;
            }

            SkillDefinition skill = skills[index];
            GridCoord origin = player.Mover.Position;
            GridDirection facing = player.Mover.Facing;

            bool isMovementSkill = skill.Id == SkillCatalog.BlinkSkillId || skill.Id == SkillCatalog.DashSkillId;
            if (isMovementSkill && !player.TryBlink(skill.RangeTiles))
            {
                castFeedback?.PlayCast("이동할 공간이 없습니다");
                return;
            }

            var combat = player.GetComponent<Sapphire.Presentation.Combat.PlayerCombatController>();
            
            // Mana Check (Arbitrary 10 MP per skill for now)
            if (combat != null && !isMovementSkill)
            {
                if (!combat.Mana.TryConsume(10))
                {
                    castFeedback?.PlayCast("마나가 부족합니다!");
                    return;
                }
            }

            skillVfx = ResolveSkillVfx();
            skillVfx.Play(index, origin, player.Mover.Position, facing);
            motionPlayer?.PlayAttack(facing);
            
            if (combat != null)
            {
                if (!isMovementSkill && skill.RangeTiles > 0)
                {
                    // Deal damage to all tiles in skill shape/range
                    // For simplicity, just attack the first tile in range for now
                    GridCoord target = player.Mover.Position + player.Mover.Facing.ToOffset();
                    combat.AttackTarget(target.X, target.Y, 2.0f); // Skill does 2x damage
                }
            }

            castFeedback?.PlayCast(skill.DisplayName);
        }

        private ISkillVfxPlayer ResolveSkillVfx()
        {
            if (skillVfx != null)
            {
                return skillVfx;
            }

            if (player == null)
            {
                return null;
            }

            if (characterClass == CharacterClass.Warrior)
            {
                return player.GetComponent<WarriorSkillVfxPlayer>() ?? player.gameObject.AddComponent<WarriorSkillVfxPlayer>();
            }

            return player.GetComponent<SkillVfxPlayer>() ?? player.gameObject.AddComponent<SkillVfxPlayer>();
        }
    }
}
