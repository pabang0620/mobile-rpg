namespace Sapphire.Domain.Character
{
    /// <summary>Outcome of validating a proposed character name (see CharacterNameValidator).</summary>
    public enum CharacterNameValidationResult
    {
        Valid,
        TooShort,
        TooLong,
        InvalidCharacters,
        Duplicate
    }
}
