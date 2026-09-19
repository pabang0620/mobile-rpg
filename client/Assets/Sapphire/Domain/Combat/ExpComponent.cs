using System;

namespace Sapphire.Domain.Combat
{
    public class ExpComponent
    {
        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        
        public event Action<int> OnLevelUp;

        public ExpComponent(int startLevel = 1, int startExp = 0)
        {
            Level = startLevel;
            CurrentExp = startExp;
        }

        public int ExpToNextLevel => Level * 100;

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            
            CurrentExp += amount;
            bool leveledUp = false;
            
            while (CurrentExp >= ExpToNextLevel)
            {
                CurrentExp -= ExpToNextLevel;
                Level++;
                leveledUp = true;
            }
            
            if (leveledUp)
            {
                OnLevelUp?.Invoke(Level);
            }
        }
    }
}
