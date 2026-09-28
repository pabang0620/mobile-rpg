using System.Collections.Generic;

namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Per-actor cooldown clock. Time is passed in explicitly (no
    /// UnityEngine.Time reference here - Domain stays engine-free); callers
    /// pass Time.time (or any monotonically increasing seconds value).
    /// </summary>
    public class SkillCooldownTracker
    {
        private readonly Dictionary<string, double> readyAtSeconds = new Dictionary<string, double>();

        public bool IsReady(string skillId, double nowSeconds)
        {
            double readyAt;
            if (!readyAtSeconds.TryGetValue(skillId, out readyAt))
            {
                return true;
            }

            return nowSeconds >= readyAt;
        }

        public double RemainingSeconds(string skillId, double nowSeconds)
        {
            double readyAt;
            if (!readyAtSeconds.TryGetValue(skillId, out readyAt))
            {
                return 0.0;
            }

            double remaining = readyAt - nowSeconds;
            return remaining > 0.0 ? remaining : 0.0;
        }

        public void Start(string skillId, double cooldownSeconds, double nowSeconds)
        {
            if (cooldownSeconds <= 0.0)
            {
                readyAtSeconds.Remove(skillId);
                return;
            }

            readyAtSeconds[skillId] = nowSeconds + cooldownSeconds;
        }
    }
}
