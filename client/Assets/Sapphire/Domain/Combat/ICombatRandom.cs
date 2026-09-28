namespace Sapphire.Domain.Combat
{
    /// <summary>
    /// Domain-side randomness seam so CombatEngine never references
    /// UnityEngine.Random directly. Presentation supplies a real
    /// implementation (see Presentation/Combat/UnityCombatRandom.cs); tests
    /// supply a deterministic fake.
    /// </summary>
    public interface ICombatRandom
    {
        /// <summary>A value in [0.0, 1.0).</summary>
        double NextDouble();
    }
}
