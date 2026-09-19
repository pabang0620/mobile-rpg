namespace Sapphire.Domain.Combat
{
    public class CombatStats
    {
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int Attack { get; }
        public int Defense { get; }

        public CombatStats(int maxHp, int maxMp, int attack, int defense)
        {
            MaxHp = maxHp;
            MaxMp = maxMp;
            Attack = attack;
            Defense = defense;
        }
    }
}
