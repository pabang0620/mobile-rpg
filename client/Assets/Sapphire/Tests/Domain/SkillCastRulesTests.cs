using NUnit.Framework;
using Sapphire.Domain.Combat;
using Sapphire.Domain.Skills;

namespace Sapphire.Domain.Tests.Skills
{
    public class SkillCastRulesTests
    {
        [Test]
        public void TryCommit_Accepted_ConsumesManaAndStartsCooldown()
        {
            var mana = new ManaComponent(50);
            var cooldowns = new SkillCooldownTracker();

            SkillCombatSpec spec;
            SkillCastResult result = SkillCastRules.TryCommit(SkillCombatCatalog.IceSpikeId, mana, cooldowns, 0.0, out spec);

            Assert.AreEqual(SkillCastResult.Accepted, result);
            Assert.AreEqual(50 - spec.ManaCost, mana.CurrentMp);
            Assert.IsFalse(cooldowns.IsReady(SkillCombatCatalog.IceSpikeId, 0.0));
            Assert.AreEqual(SkillCombatCatalog.IceSpikeId, spec.SkillId);
        }

        [Test]
        public void TryCommit_OnCooldown_RejectsAndLeavesManaUntouched()
        {
            var mana = new ManaComponent(50);
            var cooldowns = new SkillCooldownTracker();
            SkillCombatSpec first;
            SkillCastRules.TryCommit(SkillCombatCatalog.IceSpikeId, mana, cooldowns, 0.0, out first);
            int manaAfterFirstCast = mana.CurrentMp;

            SkillCombatSpec spec;
            SkillCastResult result = SkillCastRules.TryCommit(SkillCombatCatalog.IceSpikeId, mana, cooldowns, 0.1, out spec);

            Assert.AreEqual(SkillCastResult.RejectedOnCooldown, result);
            Assert.AreEqual(manaAfterFirstCast, mana.CurrentMp);
        }

        [Test]
        public void TryCommit_InsufficientMana_RejectsAndLeavesCooldownUntouched()
        {
            var mana = new ManaComponent(5);
            var cooldowns = new SkillCooldownTracker();

            SkillCombatSpec spec;
            SkillCastResult result = SkillCastRules.TryCommit(SkillCombatCatalog.LightningSpearId, mana, cooldowns, 0.0, out spec);

            Assert.AreEqual(SkillCastResult.RejectedInsufficientMana, result);
            Assert.AreEqual(5, mana.CurrentMp);
            Assert.IsTrue(cooldowns.IsReady(SkillCombatCatalog.LightningSpearId, 0.0));
        }

        [Test]
        public void TryCommit_UnknownSkill_Rejected()
        {
            var mana = new ManaComponent(50);
            var cooldowns = new SkillCooldownTracker();

            SkillCombatSpec spec;
            SkillCastResult result = SkillCastRules.TryCommit("skill.does_not_exist", mana, cooldowns, 0.0, out spec);

            Assert.AreEqual(SkillCastResult.RejectedUnknownSkill, result);
            Assert.AreEqual(50, mana.CurrentMp);
        }

        [Test]
        public void Check_NeverMutatesManaOrCooldown()
        {
            var mana = new ManaComponent(50);
            var cooldowns = new SkillCooldownTracker();

            SkillCombatSpec spec;
            SkillCastResult result = SkillCastRules.Check(SkillCombatCatalog.IceSpikeId, mana, cooldowns, 0.0, out spec);

            Assert.AreEqual(SkillCastResult.Accepted, result);
            Assert.AreEqual(50, mana.CurrentMp);
            Assert.IsTrue(cooldowns.IsReady(SkillCombatCatalog.IceSpikeId, 0.0));
        }

        [Test]
        public void ZeroManaCostSkill_CanBeCastAgainOnceItsShortCooldownElapses()
        {
            var mana = new ManaComponent(50);
            var cooldowns = new SkillCooldownTracker();

            SkillCombatSpec spec1;
            SkillCastResult first = SkillCastRules.TryCommit(SkillCombatCatalog.BasicAttackId, mana, cooldowns, 0.0, out spec1);
            SkillCombatSpec spec2;
            SkillCastResult tooSoon = SkillCastRules.TryCommit(SkillCombatCatalog.BasicAttackId, mana, cooldowns, 0.1, out spec2);
            SkillCombatSpec spec3;
            SkillCastResult second = SkillCastRules.TryCommit(SkillCombatCatalog.BasicAttackId, mana, cooldowns, 0.5, out spec3);

            Assert.AreEqual(SkillCastResult.Accepted, first);
            Assert.AreEqual(SkillCastResult.RejectedOnCooldown, tooSoon);
            Assert.AreEqual(SkillCastResult.Accepted, second);
            Assert.AreEqual(50, mana.CurrentMp);
        }
    }
}
