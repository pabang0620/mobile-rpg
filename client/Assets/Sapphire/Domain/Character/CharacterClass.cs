namespace Sapphire.Domain.Character
{
    /// <summary>
    /// The set of playable character classes. Mage is the original class
    /// (see docs/planning/01_PRODUCT.md); Warrior is added by this slice.
    /// Adding a new class means adding a new enum value here plus a matching
    /// branch everywhere this enum is switched on (SkillCatalog.ForClass,
    /// SapphireSceneBuilder's per-class rig builder, art import) - there is
    /// no single central registry, by design, since each of those concerns
    /// (skills/art/scene wiring) has different per-class data.
    /// </summary>
    public enum CharacterClass
    {
        Mage,
        Warrior
    }
}
