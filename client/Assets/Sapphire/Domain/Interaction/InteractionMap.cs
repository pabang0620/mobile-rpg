using System.Collections.Generic;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Interaction
{
    /// <summary>
    /// Pure lookup from grid coordinate to interactable identifier.
    /// </summary>
    public class InteractionMap
    {
        private readonly Dictionary<GridCoord, InteractableId> entries = new Dictionary<GridCoord, InteractableId>();

        public void Register(GridCoord coord, InteractableId id)
        {
            entries[coord] = id;
        }

        public bool TryGet(GridCoord coord, out InteractableId id)
        {
            return entries.TryGetValue(coord, out id);
        }
    }
}
