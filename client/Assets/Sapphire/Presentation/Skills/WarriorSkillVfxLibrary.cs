using UnityEngine;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Runtime frame data for the warrior's atlas-based VFX (see
    /// WarriorSkillVfxImporter for how this is populated at import time and
    /// WarriorSkillVfxPlayer for how it's played back). Frames is laid out in
    /// a FIXED row order (8 frames per row, 6 rows = 48) that matches neither
    /// the source atlas file's own row order (WarriorSkillVfxAtlas.png's rows
    /// are BasicAttackSlash/WhirlwindSlash/WarCryShockwave/ShieldBlockBarrier/
    /// DashStreak top-to-bottom) nor SkillCatalog.ForClass(Warrior)'s array
    /// order (Dash/Whirlwind/ShieldBlock/WarCry/GroundSlam, and it has no
    /// BasicAttack entry at all - see SkillCatalog's class doc) - the
    /// importer maps the atlas's row NAMES into these named row constants
    /// explicitly, never by raw index.
    /// </summary>
    public sealed class WarriorSkillVfxLibrary : ScriptableObject
    {
        public const int BasicAttackRow = 0;
        public const int DashRow = 1;
        public const int WhirlwindRow = 2;
        public const int ShieldBlockRow = 3;
        public const int WarCryRow = 4;
        public const int GroundSlamRow = 5;

        public Sprite[] Frames = new Sprite[48];
    }
}
