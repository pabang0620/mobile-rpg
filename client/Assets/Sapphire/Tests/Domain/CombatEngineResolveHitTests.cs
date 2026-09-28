using NUnit.Framework;
using Sapphire.Domain.Combat;

namespace Sapphire.Domain.Tests.Combat
{
    public class CombatEngineResolveHitTests
    {
        private class FakeCombatRandom : ICombatRandom
        {
            private readonly double value;
            public FakeCombatRandom(double value) { this.value = value; }
            public double NextDouble() => value;
        }

        [Test]
        public void ResolveHit_BelowCritChance_AppliesCritMultiplier()
        {
            var attacker = new CombatStats(100, 50, 20, 5);
            var defender = new CombatStats(100, 50, 5, 10);
            var defenderHealth = new HealthComponent(100);
            var rng = new FakeCombatRandom(0.0); // 0.0 < CritChance -> crit

            HitResult hit = CombatEngine.ResolveHit(attacker, defender, defenderHealth, 1.0f, rng);

            // (20 * 1.0 * 1.5) - 10 = 20
            Assert.IsTrue(hit.IsCrit);
            Assert.AreEqual(20, hit.Damage);
            Assert.AreEqual(80, defenderHealth.CurrentHp);
        }

        [Test]
        public void ResolveHit_AtOrAboveCritChance_IsNotCrit()
        {
            var attacker = new CombatStats(100, 50, 20, 5);
            var defender = new CombatStats(100, 50, 5, 10);
            var defenderHealth = new HealthComponent(100);
            var rng = new FakeCombatRandom(0.99);

            HitResult hit = CombatEngine.ResolveHit(attacker, defender, defenderHealth, 1.0f, rng);

            // 20 * 1.0 - 10 = 10
            Assert.IsFalse(hit.IsCrit);
            Assert.AreEqual(10, hit.Damage);
            Assert.AreEqual(90, defenderHealth.CurrentHp);
        }

        [Test]
        public void ResolveHit_AlreadyDeadDefender_IsNoOp()
        {
            var attacker = new CombatStats(100, 50, 20, 5);
            var defender = new CombatStats(100, 50, 5, 10);
            var defenderHealth = new HealthComponent(10);
            defenderHealth.TakeDamage(10);
            Assert.IsTrue(defenderHealth.IsDead);

            var rng = new FakeCombatRandom(0.0);
            HitResult hit = CombatEngine.ResolveHit(attacker, defender, defenderHealth, 1.0f, rng);

            Assert.AreEqual(0, hit.Damage);
            Assert.IsFalse(hit.IsCrit);
            Assert.IsFalse(hit.Killed);
            Assert.AreEqual(0, defenderHealth.CurrentHp);
        }

        [Test]
        public void ResolveHit_LethalDamage_ReportsKilled()
        {
            var attacker = new CombatStats(100, 50, 50, 0);
            var defender = new CombatStats(20, 50, 0, 0);
            var defenderHealth = new HealthComponent(20);
            var rng = new FakeCombatRandom(0.99); // avoid crit for a predictable number

            HitResult hit = CombatEngine.ResolveHit(attacker, defender, defenderHealth, 1.0f, rng);

            Assert.IsTrue(hit.Killed);
            Assert.IsTrue(defenderHealth.IsDead);
        }

        [Test]
        public void ResolveHit_NullRng_NeverCrits()
        {
            var attacker = new CombatStats(100, 50, 20, 5);
            var defender = new CombatStats(100, 50, 5, 10);
            var defenderHealth = new HealthComponent(100);

            HitResult hit = CombatEngine.ResolveHit(attacker, defender, defenderHealth, 1.0f, null);

            Assert.IsFalse(hit.IsCrit);
            Assert.AreEqual(10, hit.Damage);
        }
    }
}
