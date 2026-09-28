using NUnit.Framework;
using Sapphire.Domain.Combat;

namespace Sapphire.Domain.Tests.Combat
{
    public class CombatRewardsTests
    {
        [Test]
        public void ExpFor_NormalMonster_Returns30()
        {
            Assert.AreEqual(30, CombatRewards.ExpFor(false));
        }

        [Test]
        public void ExpFor_Boss_Returns100()
        {
            Assert.AreEqual(100, CombatRewards.ExpFor(true));
        }
    }
}
