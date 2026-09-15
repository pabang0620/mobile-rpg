using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Common shape for a per-class cosmetic skill VFX player, so
    /// RadialSkillMenu can drive whichever class's player is attached to the
    /// active character without knowing its concrete type. row is the index
    /// into that class's SkillCatalog.ForClass(...) array (0-4), matching the
    /// 1-5 key bindings. Implemented by SkillVfxPlayer (mage) and
    /// WarriorSkillVfxPlayer (warrior) - both are atlas-frame based (2026-09-15:
    /// the warrior kit's real VFX art landed, replacing its earlier
    /// procedural-primitive placeholder implementation).
    /// </summary>
    public interface ISkillVfxPlayer
    {
        void Play(int row, GridCoord origin, GridCoord destination, GridDirection facing);
    }
}
