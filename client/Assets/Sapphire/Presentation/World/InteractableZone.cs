using UnityEngine;
using UnityEngine.Events;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Interaction;

namespace Sapphire.Presentation.World
{
    /// <summary>
    /// Registers its grid coordinate with the Domain InteractionMap and
    /// exposes a UnityEvent that InteractionTrigger fires when a player
    /// interacts with it.
    /// </summary>
    public class InteractableZone : MonoBehaviour
    {
        [SerializeField] private string interactableId = "signpost";
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;
        [TextArea]
        [SerializeField] private string message = "A weathered signpost.";
        [SerializeField] private string destinationScene;
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        public string InteractableIdValue => interactableId;
        public string Message => message;
        public string DestinationScene => destinationScene;
        public GridCoord Coord => new GridCoord(gridX, gridY);
        public UnityEvent OnInteract => onInteract;

        public void RegisterWith(InteractionMap map)
        {
            if (map == null || string.IsNullOrEmpty(interactableId))
            {
                return;
            }

            map.Register(Coord, new InteractableId(interactableId));
        }

        public void Fire()
        {
            onInteract.Invoke();
        }
    }
}
