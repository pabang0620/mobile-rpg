namespace Sapphire.Domain.Skills
{
    public enum SkillCastResult
    {
        Accepted,
        RejectedUnknownSkill,
        RejectedOnCooldown,
        RejectedInsufficientMana,
        RejectedDead
    }
}
