using Sapphire.Domain.Combat;

namespace Sapphire.Presentation.Combat
{
    /// <summary>Thin ICombatRandom adapter over UnityEngine.Random for CombatEngine.ResolveHit.</summary>
    public class UnityCombatRandom : ICombatRandom
    {
        public double NextDouble()
        {
            return UnityEngine.Random.value;
        }
    }
}
