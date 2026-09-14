using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Interaction;

namespace Sapphire.Presentation.World
{
    /// <summary>
    /// On interact input, looks up the grid cell the player currently faces
    /// in the Domain InteractionMap and fires the matching zone's UnityEvent.
    /// </summary>
    public class InteractionTrigger : MonoBehaviour
    {
        private InteractionMap interactionMap;
        private readonly Dictionary<InteractableId, InteractableZone> zonesById = new Dictionary<InteractableId, InteractableZone>();

        public void Initialize(InteractionMap map, IEnumerable<InteractableZone> zones)
        {
            interactionMap = map;
            zonesById.Clear();

            if (zones == null)
            {
                return;
            }

            foreach (InteractableZone zone in zones)
            {
                if (zone == null || string.IsNullOrEmpty(zone.InteractableIdValue))
                {
                    continue;
                }

                zone.RegisterWith(interactionMap);
                zonesById[new InteractableId(zone.InteractableIdValue)] = zone;
            }
        }

        public void TryInteract(GridCoord playerPosition, GridDirection playerFacing)
        {
            if (interactionMap == null)
            {
                return;
            }

            GridCoord facedCoord = playerPosition + playerFacing.ToOffset();

            if (!interactionMap.TryGet(facedCoord, out InteractableId id) || id == InteractableId.None)
            {
                return;
            }

            if (zonesById.TryGetValue(id, out InteractableZone zone))
            {
                zone.Fire();
            }
        }
    }
}
