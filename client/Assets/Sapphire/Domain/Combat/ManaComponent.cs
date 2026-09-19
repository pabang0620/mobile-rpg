using System;

namespace Sapphire.Domain.Combat
{
    public class ManaComponent
    {
        public int CurrentMp { get; private set; }
        public int MaxMp { get; private set; }

        public event Action<int> OnManaConsumed;

        public ManaComponent(int maxMp)
        {
            MaxMp = maxMp;
            CurrentMp = maxMp;
        }

        public bool TryConsume(int amount)
        {
            if (amount < 0) return false;
            if (CurrentMp < amount) return false;

            CurrentMp -= amount;
            OnManaConsumed?.Invoke(amount);
            return true;
        }

        public void Restore(int amount)
        {
            if (amount <= 0) return;
            CurrentMp += amount;
            if (CurrentMp > MaxMp) CurrentMp = MaxMp;
        }
    }
}
