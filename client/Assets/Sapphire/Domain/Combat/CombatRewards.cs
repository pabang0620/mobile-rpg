namespace Sapphire.Domain.Combat
{
    /// <summary>EXP reward table for a kill. Values unchanged from the
    /// inline 30/100 constants PlayerCombatController.ExecuteAttack used to
    /// hardcode.</summary>
    public static class CombatRewards
    {
        public const int NormalMonsterExp = 30;
        public const int BossMonsterExp = 100;

        public static int ExpFor(bool isBoss)
        {
            return isBoss ? BossMonsterExp : NormalMonsterExp;
        }
    }
}
