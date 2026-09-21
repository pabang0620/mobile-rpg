using System;

namespace Sapphire.Domain.Combat
{
    public class HealthComponent
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public event Action<int> OnDamageTaken;
        public event Action OnDied;

        public HealthComponent(int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;

            CurrentHp -= damage;
            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
            }

            OnDamageTaken?.Invoke(damage);

            if (IsDead)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;

            CurrentHp += amount;
            if (CurrentHp > MaxHp)
            {
                CurrentHp = MaxHp;
            }
        }
    }
}
