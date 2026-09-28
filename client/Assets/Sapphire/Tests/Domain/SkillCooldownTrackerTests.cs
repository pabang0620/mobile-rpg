using NUnit.Framework;
using Sapphire.Domain.Skills;

namespace Sapphire.Domain.Tests.Skills
{
    public class SkillCooldownTrackerTests
    {
        [Test]
        public void IsReady_TrueBeforeAnyCastStarted()
        {
            var tracker = new SkillCooldownTracker();

            Assert.IsTrue(tracker.IsReady("skill.x", 0.0));
        }

        [Test]
        public void Start_MakesSkillNotReadyUntilCooldownElapses()
        {
            var tracker = new SkillCooldownTracker();

            tracker.Start("skill.x", 5.0, 10.0);

            Assert.IsFalse(tracker.IsReady("skill.x", 12.0));
            Assert.IsTrue(tracker.IsReady("skill.x", 15.0));
        }

        [Test]
        public void RemainingSeconds_CountsDownToZeroAndNeverGoesNegative()
        {
            var tracker = new SkillCooldownTracker();

            tracker.Start("skill.x", 5.0, 10.0);

            Assert.AreEqual(3.0, tracker.RemainingSeconds("skill.x", 12.0), 0.0001);
            Assert.AreEqual(0.0, tracker.RemainingSeconds("skill.x", 20.0), 0.0001);
        }

        [Test]
        public void RemainingSeconds_ZeroForSkillNeverStarted()
        {
            var tracker = new SkillCooldownTracker();

            Assert.AreEqual(0.0, tracker.RemainingSeconds("skill.never_cast", 5.0), 0.0001);
        }

        [Test]
        public void Start_WithZeroCooldown_LeavesSkillReady()
        {
            var tracker = new SkillCooldownTracker();

            tracker.Start("skill.x", 0.0, 10.0);

            Assert.IsTrue(tracker.IsReady("skill.x", 10.0));
        }

        [Test]
        public void SkillsTrackIndependently()
        {
            var tracker = new SkillCooldownTracker();

            tracker.Start("skill.a", 10.0, 0.0);

            Assert.IsFalse(tracker.IsReady("skill.a", 5.0));
            Assert.IsTrue(tracker.IsReady("skill.b", 5.0));
        }
    }
}
