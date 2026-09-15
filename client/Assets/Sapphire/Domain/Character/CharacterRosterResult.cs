namespace Sapphire.Domain.Character
{
    /// <summary>Outcome of CharacterRoster.TryAddCharacter - one Accepted case, the rest are explicit rejections.</summary>
    public enum CharacterRosterResult
    {
        Added,
        RosterFull,
        NameTooShort,
        NameTooLong,
        NameInvalidCharacters,
        NameDuplicate
    }
}
