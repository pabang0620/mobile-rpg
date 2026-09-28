using System.Collections.Generic;

namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Single source of truth for skill string IDs and their combat tuning
    /// (mana cost / cooldown / damage multiplier / hit delay). Presentation's
    /// SkillCatalog (Presentation/Skills/SkillCatalog.cs) aliases its own
    /// public id constants to these so there is exactly one place that owns
    /// each literal skill id string.
    /// </summary>
    public static class SkillCombatCatalog
    {
        // Basic attack has no SkillCatalog/SkillDefinition entry (it is a
        // separate button, see WarriorCombatConstants), but it still needs a
        // stable id to key cooldowns/mana rules by.
        public const string BasicAttackId = "skill.basic_attack";

        // --- Mage ---
        public const string ShieldId = "skill.shield";
        public const string BlinkId = "skill.blink";
        public const string ThunderGridId = "skill.thunder_grid";
        public const string IceSpikeId = "skill.ice_spike";
        public const string LightningSpearId = "skill.lightning_spear";

        // --- Warrior ---
        public const string DashId = "skill.dash";
        public const string WhirlwindId = "skill.whirlwind";
        public const string ShieldBlockId = "skill.shield_block";
        public const string WarCryId = "skill.war_cry";
        public const string GroundSlamId = "skill.ground_slam";

        private static readonly Dictionary<string, SkillCombatSpec> Specs = new Dictionary<string, SkillCombatSpec>
        {
            // id, manaCost, cooldownSeconds, damageMultiplier, hitDelaySeconds[, hitStaggerPerTileSeconds]
            // Hit timings (2026-09-28) follow each VFX's brightest/impact frame:
            // hitDelay ~= animDuration * impactFrame / 8 (8-frame sheets, see the
            // Animate() durations in SkillVfxPlayer / WarriorSkillVfxPlayer).
            // Projectiles stagger per tile at their on-screen travel speed.
            { BasicAttackId, new SkillCombatSpec(BasicAttackId, 0, 0.45f, 1.0f, 0.12f) },

            // Mage: shield/blink are utility (0 damage); the 3 offensive
            // spells scale mana/cooldown with their damage multiplier.
            { ShieldId, new SkillCombatSpec(ShieldId, 15, 10f, 0f, 0f) },
            { BlinkId, new SkillCombatSpec(BlinkId, 8, 4f, 0f, 0f) },
            { ThunderGridId, new SkillCombatSpec(ThunderGridId, 18, 5f, 2.0f, 0.4f) },
            { IceSpikeId, new SkillCombatSpec(IceSpikeId, 12, 2.5f, 1.5f, 0.05f, 0.16f) },
            { LightningSpearId, new SkillCombatSpec(LightningSpearId, 25, 6f, 3.0f, 0.1f, 0.06f) },

            // Warrior: dash is pure movement (0 damage, unchanged from
            // before this refactor); shield_block/war_cry are defensive
            // buffs with long cooldowns; whirlwind/ground_slam are melee AoE.
            { DashId, new SkillCombatSpec(DashId, 10, 3f, 0f, 0f) },
            { WhirlwindId, new SkillCombatSpec(WhirlwindId, 20, 5f, 2.2f, 0.15f) },
            { ShieldBlockId, new SkillCombatSpec(ShieldBlockId, 15, 8f, 0f, 0f) },
            { WarCryId, new SkillCombatSpec(WarCryId, 20, 12f, 0f, 0f) },
            { GroundSlamId, new SkillCombatSpec(GroundSlamId, 22, 6f, 2.5f, 0.15f) },
        };

        public static bool TryGet(string id, out SkillCombatSpec spec)
        {
            if (id != null && Specs.TryGetValue(id, out spec))
            {
                return true;
            }

            spec = default(SkillCombatSpec);
            return false;
        }
    }
}
