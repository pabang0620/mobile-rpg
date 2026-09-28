namespace Sapphire.Domain.Combat
{
    /// <summary>Outcome of CombatEngine.ResolveHit - what to show/react to, no domain mutation left implicit.</summary>
    public readonly struct HitResult
    {
        public readonly int Damage;
        public readonly bool IsCrit;
        public readonly bool Killed;

        public HitResult(int damage, bool isCrit, bool killed)
        {
            Damage = damage;
            IsCrit = isCrit;
            Killed = killed;
        }
    }
}
