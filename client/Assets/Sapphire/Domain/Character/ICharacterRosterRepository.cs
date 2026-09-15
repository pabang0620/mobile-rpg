using System.Collections.Generic;

namespace Sapphire.Domain.Character
{
    /// <summary>
    /// Domain-owned persistence contract for one account's character roster.
    /// Implemented in Infrastructure (file I/O + JSON, both Unity-dependent -
    /// see Sapphire.Infrastructure.Character.CharacterRosterFileRepository)
    /// so Domain itself stays free of UnityEngine/System.IO references
    /// (Sapphire.Domain.asmdef has noEngineReferences=true).
    /// </summary>
    public interface ICharacterRosterRepository
    {
        IReadOnlyList<CharacterSlot> Load(string accountId);

        void Save(string accountId, IReadOnlyList<CharacterSlot> slots);
    }
}
