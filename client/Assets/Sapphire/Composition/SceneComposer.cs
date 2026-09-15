using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sapphire.Domain.Character;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Interaction;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.Session;
using Sapphire.Presentation.World;
using Sapphire.Presentation.UI;

namespace Sapphire.Composition
{
    /// <summary>
    /// Thin composition root: wires Presentation components together on
    /// scene load. Reference connections and event subscriptions only - no
    /// conditional logic or state changes live here, EXCEPT the one
    /// class-activation branch below, which exists because VillageHub bakes
    /// both a Mage and a Warrior character rig (body sheet + skill fan + VFX
    /// player) into the same scene at Editor-build time (see
    /// SapphireSceneBuilder) - Editor-only AssetDatabase sprite loading can't
    /// happen at runtime, so there is no way to swap a single rig's sprites
    /// post-build. Instead both rigs exist, and this picks exactly one based
    /// on CharacterSessionService.Instance.SelectedCharacter.Class (defaults
    /// to Mage if nothing was selected, e.g. opening this scene directly in
    /// the Editor without going through Login/CharacterSelect first) and
    /// deactivates the other before doing the rest of its normal wiring.
    /// </summary>
    public class SceneComposer : MonoBehaviour
    {
        [SerializeField] private TilemapGridMapBuilder gridMapBuilder;
        [SerializeField] private PlayerGridController mageController;
        [SerializeField] private PlayerInputReader mageInputReader;
        [SerializeField] private GameObject mageSkillMenuRoot;
        [SerializeField] private PlayerGridController warriorController;
        [SerializeField] private PlayerInputReader warriorInputReader;
        [SerializeField] private GameObject warriorSkillMenuRoot;
        [SerializeField] private Presentation.Camera.CameraFollowRig cameraFollowRig;
        [SerializeField] private InteractionTrigger interactionTrigger;
        [SerializeField] private SimpleMessagePanel messagePanel;
        [SerializeField] private int playerSpawnX;
        [SerializeField] private int playerSpawnY;
        [SerializeField] private List<InteractableZone> interactableZones = new List<InteractableZone>();

        private PlayerGridController activePlayerController;
        private PlayerInputReader activePlayerInputReader;

        private void Awake()
        {
            GridMap map = gridMapBuilder.Build();
            var spawnCoord = new GridCoord(playerSpawnX, playerSpawnY);

            ActivateSelectedClassRig();

            activePlayerController.Initialize(map, spawnCoord);
            cameraFollowRig.SetTarget(activePlayerController.transform);

            var interactionMap = new InteractionMap();
            interactionTrigger.Initialize(interactionMap, interactableZones);

            foreach (InteractableZone zone in interactableZones)
            {
                zone.OnInteract.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(zone.DestinationScene))
                    {
                        SceneManager.LoadScene(zone.DestinationScene);
                        return;
                    }
                    messagePanel.SetText(zone.Message);
                    messagePanel.Show();
                });
            }

            activePlayerInputReader.InteractPressed += HandleInteractPressed;
        }

        private void ActivateSelectedClassRig()
        {
            CharacterSlot selected = CharacterSessionService.Instance.SelectedCharacter;
            CharacterClass activeClass = selected != null ? selected.Class : CharacterClass.Mage;

            bool useWarrior = activeClass == CharacterClass.Warrior;
            activePlayerController = useWarrior ? warriorController : mageController;
            activePlayerInputReader = useWarrior ? warriorInputReader : mageInputReader;
            GameObject activeSkillMenuRoot = useWarrior ? warriorSkillMenuRoot : mageSkillMenuRoot;
            GameObject inactiveRigGo = useWarrior ? mageController.gameObject : warriorController.gameObject;
            GameObject inactiveSkillMenuRoot = useWarrior ? mageSkillMenuRoot : warriorSkillMenuRoot;

            inactiveRigGo.SetActive(false);
            if (inactiveSkillMenuRoot != null)
            {
                inactiveSkillMenuRoot.SetActive(false);
            }

            activePlayerController.gameObject.SetActive(true);
            if (activeSkillMenuRoot != null)
            {
                activeSkillMenuRoot.SetActive(true);
            }
        }

        private void HandleInteractPressed()
        {
            interactionTrigger.TryInteract(activePlayerController.Mover.Position, activePlayerController.Mover.Facing);
        }

        private void OnDestroy()
        {
            if (activePlayerInputReader != null)
            {
                activePlayerInputReader.InteractPressed -= HandleInteractPressed;
            }
        }
    }
}
