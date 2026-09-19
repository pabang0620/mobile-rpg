using NUnit.Framework;
using Sapphire.Domain.Combat;

namespace Sapphire.Domain.Tests.Combat
{
    public class CombatEngineTests
    {
        [Test]
        public void CombatEngine_CalculatesDamage_SubtractsDefenseFromAttack()
        {
            var attacker = new CombatStats(100, 50, 20, 5);
            var defender = new CombatStats(100, 50, 5, 10);
            
            // 20 Atk * 1.0 - 10 Def = 10 Damage
            int damage = CombatEngine.CalculateDamage(attacker, defender);
            
            Assert.AreEqual(10, damage);
        }

        [Test]
        public void CombatEngine_ProcessAttack_ReducesDefenderHealth()
        {
            var attacker = new CombatStats(100, 50, 30, 5);
            var defender = new CombatStats(50, 50, 5, 5);
            var defenderHealth = new HealthComponent(50);
            
            // 30 Atk - 5 Def = 25 Damage
            bool result = CombatEngine.ProcessAttack(attacker, defender, defenderHealth);
            
            Assert.IsTrue(result);
            Assert.AreEqual(25, defenderHealth.CurrentHp);
        }

        [Test]
        public void HealthComponent_Dies_WhenHpReachesZero()
        {
            var health = new HealthComponent(20);
            bool died = false;
            health.OnDied += () => died = true;
            
            health.TakeDamage(25);
            
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0, health.CurrentHp);
            Assert.IsTrue(died);
        }

        [Test]
        public void ManaComponent_TryConsume_FailsIfNotEnoughMana()
        {
            var mana = new ManaComponent(10);
            
            bool result = mana.TryConsume(15);
            
            Assert.IsFalse(result);
            Assert.AreEqual(10, mana.CurrentMp);
        }

        [Test]
        public void ManaComponent_TryConsume_SucceedsIfEnoughMana()
        {
            var mana = new ManaComponent(20);
            
            bool result = mana.TryConsume(15);
            
            Assert.IsTrue(result);
            Assert.AreEqual(5, mana.CurrentMp);
        }
    }
}
