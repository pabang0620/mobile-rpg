using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Common shape for a per-class cosmetic skill VFX player, so
    /// RadialSkillMenu can drive whichever class's player is attached to the
    /// active character without knowing its concrete type. row is the index
    /// into that class's SkillCatalog.ForClass(...) array (0-4), matching the
    /// 1-5 key bindings. Implemented by SkillVfxPlayer (mage, atlas-frame
    /// based - unchanged by this interface's introduction) and
    /// WarriorSkillVfxPlayer (procedural primitives, no atlas art available
    /// for the warrior kit yet).
    /// </summary>
    public interface ISkillVfxPlayer
    {
        void Play(int row, GridCoord origin, GridCoord destination, GridDirection facing);
    }
}
