using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Interaction;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.World;
using Sapphire.Presentation.UI;

namespace Sapphire.Composition
{
    /// <summary>
    /// Thin composition root: wires Presentation components together on
    /// scene load. Reference connections and event subscriptions only - no
    /// conditional logic or state changes live here.
    /// </summary>
    public class SceneComposer : MonoBehaviour
    {
        [SerializeField] private TilemapGridMapBuilder gridMapBuilder;
        [SerializeField] private PlayerGridController playerController;
        [SerializeField] private PlayerInputReader playerInputReader;
        [SerializeField] private Presentation.Camera.CameraFollowRig cameraFollowRig;
        [SerializeField] private InteractionTrigger interactionTrigger;
        [SerializeField] private SimpleMessagePanel messagePanel;
        [SerializeField] private int playerSpawnX;
        [SerializeField] private int playerSpawnY;
        [SerializeField] private List<InteractableZone> interactableZones = new List<InteractableZone>();

        private void Awake()
        {
            GridMap map = gridMapBuilder.Build();
            var spawnCoord = new GridCoord(playerSpawnX, playerSpawnY);

            playerController.Initialize(map, spawnCoord);
            cameraFollowRig.SetTarget(playerController.transform);

            var interactionMap = new InteractionMap();
            interactionTrigger.Initialize(interactionMap, interactableZones);

            foreach (InteractableZone zone in interactableZones)
            {
                zone.OnInteract.AddListener(() =>
                {
                    messagePanel.SetText(zone.Message);
                    messagePanel.Show();
                });
            }

            playerInputReader.InteractPressed += HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            interactionTrigger.TryInteract(playerController.Mover.Position, playerController.Mover.Facing);
        }

        private void OnDestroy()
        {
            if (playerInputReader != null)
            {
                playerInputReader.InteractPressed -= HandleInteractPressed;
            }
        }
    }
}
