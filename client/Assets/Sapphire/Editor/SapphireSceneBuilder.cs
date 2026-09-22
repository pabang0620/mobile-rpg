using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Sapphire.Composition;
using Sapphire.Domain.Character;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.Camera;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.Skills;
using Sapphire.Presentation.UI;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// One-shot tool that configures art import settings and builds the
    /// VillageHub scene from scratch. Intended to be run via
    /// -batchmode -executeMethod, and safe to re-run (idempotent: it
    /// rebuilds the generated tile/sprite assets and scene content).
    ///
    /// This class is a thin orchestrator: art import lives in
    /// <see cref="ArtImportConfigurator"/>, ground/fence/signpost terrain
    /// generation lives in <see cref="VillageHubTerrainBuilder"/>, and UI
    /// (message panel + skill bar) lives in <see cref="VillageHubUiBuilder"/>.
    /// This file builds the player + camera (the piece that sits between the
    /// two, since the skill bar needs the player controller) and wires the
    /// composition root.
    /// </summary>
    public static class SapphireSceneBuilder
    {
        internal const string RootArtDir = "Assets/Sapphire/Art";
        internal const string WorldArtDir = "Assets/Sapphire/Art/World";
        internal const string UiArtDir = "Assets/Sapphire/Art/UI";
        private const string ScenePath = "Assets/Sapphire/Scenes/VillageHub.unity";
        private const string SlimeKingdomScenePath = "Assets/Sapphire/Scenes/SlimeKingdom.unity";
        internal const string GeneratedDir = "Assets/Sapphire/Generated";

        // 2026-09-20 player feedback: The massive Town Hall completely blocked the top gate on a 24x18 map.
        // Widened to 32x32 to accommodate the new 10x10 town hall and allow walking around it.
        internal const int MapWidth = 32;
        internal const int MapHeight = 32;
        internal const int SignX = 16;
        internal const int SignY = 13;
        internal const int SpawnX = 16;
        internal const int SpawnY = 6;

        public static void BuildAll()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("SapphireSceneBuilder.BuildAll()은 씬을 덮어쓰기 때문에 에디터에서만 실행해야 합니다.");
                return;
            }

            try
            {
                ArtImportConfigurator.ConfigureArtImportSettings();
                SkillVfxImporter.ConfigureLibrary();
                WarriorSkillVfxImporter.ConfigureLibrary();
                BuildVillageHubScene();
                BuildSlimeKingdomScene();
                Debug.Log("SAPPHIRE_BUILD SUCCESS");
            }
            catch (Exception e)
            {
                Debug.LogError("SAPPHIRE_BUILD FAILED: " + e);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                else
                {
                    throw;
                }
            }
        }

        // 2026-09-15: single -executeMethod entry point that rebuilds every
        // scene this project has (VillageHub via BuildAll, then
        // Login/CharacterSelect/CharacterCreate via
        // CharacterFlowSceneBuilder.BuildAll) - added so a full rebuild no
        // longer needs two separate Unity CLI invocations. BuildAll() above
        // is left untouched (still callable on its own, and still the one
        // every existing doc/script references) - this just chains it with
        // the character-flow builder. Order matters: VillageHub first, since
        // CharacterFlowSceneBuilder's scenes reference
        // SapphireSceneBuilder.UiArtDir sprites that ArtImportConfigurator
        // (called from this BuildAll) configures.
        public static void BuildEverything()
        {
            BuildAll();
            CharacterFlowSceneBuilder.BuildAll();

            // Register()/RegisterFirst() alone can't guarantee this exact
            // final order across two independent builders re-run against a
            // possibly-already-populated scene list - see
            // BuildSettingsSceneRegistrar.ReorderScenes's doc comment. Order
            // matches the task requirement: Login, CharacterSelect,
            // CharacterCreate, VillageHub.
            BuildSettingsSceneRegistrar.ReorderScenes(new[]
            {
                "Assets/Sapphire/Scenes/Login.unity",
                "Assets/Sapphire/Scenes/CharacterSelect.unity",
                "Assets/Sapphire/Scenes/CharacterCreate.unity",
                ScenePath,
                SlimeKingdomScenePath,
            });
        }

        // ------------------------------------------------------------
        // Scene construction
        // ------------------------------------------------------------

        private static void BuildVillageHubScene()
        {
            // Always start from a brand-new empty scene rather than opening the
            // previously-built file in place: this method re-creates every
            // GameObject unconditionally (Grid, Player, Main Camera, UI, ...), so
            // opening the existing content and adding to it would duplicate the
            // whole hierarchy on every re-run. Saving over ScenePath at the end
            // replaces the file, which is what "idempotent, rebuilds ... scene
            // content" (see class doc) actually requires.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            TerrainBuildResult terrain = VillageHubTerrainBuilder.Build();

            // Both classes' full player rigs are baked into this one scene -
            // see SceneComposer's class doc for why (Editor-only AssetDatabase
            // sprite loading can't happen at runtime, so there is no way to
            // swap a single rig's sprites post-build; instead both exist and
            // SceneComposer activates exactly one at runtime).
            (PlayerGridController mageController, PlayerInputReader mageInputReader, SkillCastFeedback mageCastFeedback) = BuildPlayer(CharacterClass.Mage, SpawnX, SpawnY);
            (PlayerGridController warriorController, PlayerInputReader warriorInputReader, SkillCastFeedback warriorCastFeedback) = BuildPlayer(CharacterClass.Warrior, SpawnX, SpawnY);

            CameraFollowRig followRig = BuildCamera(mageController.transform.position, terrain.GroundTilemap);
            UiBuildResult ui = VillageHubUiBuilder.Build(
                mageController, mageInputReader, mageCastFeedback,
                warriorController, warriorInputReader, warriorCastFeedback);

            ComposeSceneRoot(terrain, mageController, mageInputReader, ui.MageSkillMenuRoot, warriorController, warriorInputReader, ui.WarriorSkillMenuRoot, followRig, ui.MessagePanel);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new Exception("Scene save failed");
            }

            // 2026-09 character-flow slice: was
            // "EditorBuildSettings.scenes = new[] { thisOneScene }", which
            // would erase Login/CharacterSelect/CharacterCreate from Build
            // Settings every time BuildAll re-ran (see
            // BuildSettingsSceneRegistrar's class doc). Registers/updates
            // just this scene's entry instead.
            BuildSettingsSceneRegistrar.Register(ScenePath);
            AssetDatabase.SaveAssets();
        }

        public static void RebuildSlimeKingdom()
        {
            BuildSlimeKingdomScene();
            SlimeKingdomSceneVerification.VerifyAndCapture();
            Debug.Log("SLIME_KINGDOM_REBUILD SUCCESS");
        }

        private static void BuildSlimeKingdomScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TerrainBuildResult terrain = SlimeKingdomTerrainBuilder.Build();

            (PlayerGridController mageController, PlayerInputReader mageInputReader, SkillCastFeedback mageCastFeedback) =
                BuildPlayer(CharacterClass.Mage, SlimeKingdomTerrainBuilder.SpawnX, SlimeKingdomTerrainBuilder.SpawnY);
            (PlayerGridController warriorController, PlayerInputReader warriorInputReader, SkillCastFeedback warriorCastFeedback) =
                BuildPlayer(CharacterClass.Warrior, SlimeKingdomTerrainBuilder.SpawnX, SlimeKingdomTerrainBuilder.SpawnY);

            CameraFollowRig followRig = BuildCamera(mageController.transform.position, terrain.GroundTilemap);
            UiBuildResult ui = VillageHubUiBuilder.Build(
                mageController, mageInputReader, mageCastFeedback,
                warriorController, warriorInputReader, warriorCastFeedback,
                "슬라임 왕국");

            ComposeSceneRoot(
                terrain, mageController, mageInputReader, ui.MageSkillMenuRoot,
                warriorController, warriorInputReader, ui.WarriorSkillMenuRoot,
                followRig, ui.MessagePanel,
                SlimeKingdomTerrainBuilder.SpawnX, SlimeKingdomTerrainBuilder.SpawnY);

            if (!EditorSceneManager.SaveScene(scene, SlimeKingdomScenePath))
                throw new Exception("SlimeKingdom scene save failed");

            BuildSettingsSceneRegistrar.Register(SlimeKingdomScenePath);
            AssetDatabase.SaveAssets();
        }

        // --- Player ---
        // 2026-09 character-flow slice: generalized from a Mage-only hardcoded
        // sheet path/sprite-name prefix to any CharacterClass. Warrior's sheet
        // (WarriorTopdownGridSheet.png) uses the exact same grid/cell layout
        // and naming convention as Mage's (task spec: "동일한 격자/셀 크기/
        // 행열 순서"), just with "Warrior_" instead of "Mage_" as the sprite
        // name prefix - see WarriorArtImportConfigurator for the import side.
        // Calling this with CharacterClass.Mage loads the exact same sheet/
        // sprite names as before this method took a parameter, so Mage's
        // built rig is byte-for-byte unchanged.
        private static (PlayerGridController controller, PlayerInputReader inputReader, SkillCastFeedback castFeedback) BuildPlayer(CharacterClass characterClass, int spawnX, int spawnY)
        {
            string className = characterClass.ToString();
            string sheet = RootArtDir + "/" + className + "TopdownGridSheet.png";

            Sprite idleUp = LoadNamedSprite(sheet, className + "_Up_Idle");
            Sprite idleDown = LoadNamedSprite(sheet, className + "_Down_Idle");
            Sprite idleLeft = LoadNamedSprite(sheet, className + "_Left_Idle");
            Sprite idleRight = LoadNamedSprite(sheet, className + "_Right_Idle");

            Sprite walkAUp = LoadNamedSprite(sheet, className + "_Up_WalkA");
            Sprite walkADown = LoadNamedSprite(sheet, className + "_Down_WalkA");
            Sprite walkALeft = LoadNamedSprite(sheet, className + "_Left_WalkA");
            Sprite walkARight = LoadNamedSprite(sheet, className + "_Right_WalkA");

            Sprite walkBUp = LoadNamedSprite(sheet, className + "_Up_WalkB");
            Sprite walkBDown = LoadNamedSprite(sheet, className + "_Down_WalkB");
            Sprite walkBLeft = LoadNamedSprite(sheet, className + "_Left_WalkB");
            Sprite walkBRight = LoadNamedSprite(sheet, className + "_Right_WalkB");

            // 2026-09-16 (attack-motion slice): *AttackGridSheet.png, same
            // grid/naming convention as the walk sheet above but Windup/Apex/
            // Recovery columns (see ArtImportConfigurator.ConfigureMageAttackSheet /
            // WarriorArtImportConfigurator.ConfigureWarriorAttackSheet).
            string attackSheet = RootArtDir + "/" + className + "AttackGridSheet.png";
            Sprite windupUp = LoadNamedSprite(attackSheet, className + "_Up_Windup");
            Sprite windupDown = LoadNamedSprite(attackSheet, className + "_Down_Windup");
            Sprite windupLeft = LoadNamedSprite(attackSheet, className + "_Left_Windup");
            Sprite windupRight = LoadNamedSprite(attackSheet, className + "_Right_Windup");

            Sprite apexUp = LoadNamedSprite(attackSheet, className + "_Up_Apex");
            Sprite apexDown = LoadNamedSprite(attackSheet, className + "_Down_Apex");
            Sprite apexLeft = LoadNamedSprite(attackSheet, className + "_Left_Apex");
            Sprite apexRight = LoadNamedSprite(attackSheet, className + "_Right_Apex");

            Sprite recoveryUp = LoadNamedSprite(attackSheet, className + "_Up_Recovery");
            Sprite recoveryDown = LoadNamedSprite(attackSheet, className + "_Down_Recovery");
            Sprite recoveryLeft = LoadNamedSprite(attackSheet, className + "_Left_Recovery");
            Sprite recoveryRight = LoadNamedSprite(attackSheet, className + "_Right_Recovery");

            var playerGo = new GameObject("Player_" + className, typeof(SpriteRenderer), typeof(PlayerInputReader), typeof(GridMoveAnimator), typeof(DirectionalSpriteAnimator), typeof(SkillMotionPlayer), typeof(PlayerGridController), typeof(Sapphire.Presentation.Combat.PlayerCombatController));
            playerGo.transform.position = CellCenter(spawnX, spawnY);
            playerGo.GetComponent<SpriteRenderer>().sprite = idleDown;
            var spriteRenderer = playerGo.GetComponent<SpriteRenderer>();
            spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
            var dynSort = playerGo.AddComponent<Sapphire.Presentation.World.DynamicYSort>();
            dynSort.OrderOffset = 20000; // Always render player on top of props/buildings (User request)

            var spriteAnimator = playerGo.GetComponent<DirectionalSpriteAnimator>();
            AssignField(spriteAnimator, "idleUp", idleUp);
            AssignField(spriteAnimator, "idleDown", idleDown);
            AssignField(spriteAnimator, "idleLeft", idleLeft);
            AssignField(spriteAnimator, "idleRight", idleRight);
            AssignField(spriteAnimator, "walkAUp", walkAUp);
            AssignField(spriteAnimator, "walkADown", walkADown);
            AssignField(spriteAnimator, "walkALeft", walkALeft);
            AssignField(spriteAnimator, "walkARight", walkARight);
            AssignField(spriteAnimator, "walkBUp", walkBUp);
            AssignField(spriteAnimator, "walkBDown", walkBDown);
            AssignField(spriteAnimator, "walkBLeft", walkBLeft);
            AssignField(spriteAnimator, "walkBRight", walkBRight);

            var motionPlayer = playerGo.GetComponent<SkillMotionPlayer>();
            AssignField(motionPlayer, "windupUp", windupUp);
            AssignField(motionPlayer, "windupDown", windupDown);
            AssignField(motionPlayer, "windupLeft", windupLeft);
            AssignField(motionPlayer, "windupRight", windupRight);
            AssignField(motionPlayer, "apexUp", apexUp);
            AssignField(motionPlayer, "apexDown", apexDown);
            AssignField(motionPlayer, "apexLeft", apexLeft);
            AssignField(motionPlayer, "apexRight", apexRight);
            AssignField(motionPlayer, "recoveryUp", recoveryUp);
            AssignField(motionPlayer, "recoveryDown", recoveryDown);
            AssignField(motionPlayer, "recoveryLeft", recoveryLeft);
            AssignField(motionPlayer, "recoveryRight", recoveryRight);
            AssignField(motionPlayer, "spriteAnimator", spriteAnimator);

            var playerController = playerGo.GetComponent<PlayerGridController>();
            AssignField(playerController, "moveAnimator", playerGo.GetComponent<GridMoveAnimator>());
            AssignField(playerController, "spriteAnimator", spriteAnimator);

            var skillCastFeedback = playerGo.AddComponent<SkillCastFeedback>();
            AssignField(skillCastFeedback, "targetRenderer", playerGo.GetComponent<SpriteRenderer>());

            return (playerController, playerGo.GetComponent<PlayerInputReader>(), skillCastFeedback);
        }

        // --- Camera ---
        // 2026-09-15 (Phase 1, REMEDIATION_PLAN.md D1(b)): replaced the
        // com.unity.2d.pixel-perfect PixelPerfectCamera with a plain
        // orthographic camera. That component only allows INTEGER zoom, so
        // the number of tiles visible on screen jumped around per window size
        // (24/16/10 tiles depending on resolution - see REMEDIATION_PLAN.md
        // P1/P2) and it painted an on-screen dev-build warning overlay
        // whenever the window didn't exactly match its reference resolution.
        // Neither cost buys anything here - this is painterly AI art (already
        // running with pixelSnapping=false), not blocky pixel art that needs
        // integer-pixel alignment.
        //
        // Fixed vertical tile count instead: orthographicSize is set so that
        // exactly VerticalTilesVisible tiles are visible top-to-bottom on ANY
        // window size or aspect ratio (orthographicSize is a half-height in
        // world units, and GridWorldConversion.CellSize is 1 world unit per
        // tile, so half-height = tiles/2). This replaces "N tiles visible"
        // with a value that no longer depends on screen resolution, unlike
        // the old assetsPPU-based approach. CameraFollowRig then clamps the
        // camera's visible rect to the map's own bounds (read from the Ground
        // tilemap at runtime, not hardcoded) so widening the aspect ratio
        // reveals more map instead of empty space outside it.
        private const float VerticalTilesVisible = 9f;

        private static CameraFollowRig BuildCamera(Vector3 playerPosition, Tilemap groundTilemap)
        {
            var cameraGo = new GameObject("Main Camera", typeof(UnityEngine.Camera));
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = VerticalTilesVisible * 0.5f * GridWorldConversion.CellSize;
            // 2026-09-16 (F2): solid dark background instead of the default
            // skybox. A skybox visually papers over any ground-tile gap (see
            // the F1 flip-matrix fix) by making the "hole" look like distant
            // sky instead of an obvious rendering defect - this flat color
            // makes any future hole immediately, unambiguously visible.
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f); // Bright sky blue
            cameraGo.transform.position = playerPosition + new Vector3(0f, 0f, -10f);

            var followRig = cameraGo.AddComponent<CameraFollowRig>();
            cameraGo.AddComponent<Sapphire.Presentation.Combat.CameraShake>();
            followRig.SetGroundTilemap(groundTilemap);
            return followRig;
        }

        // --- Composition root ---
        private static void ComposeSceneRoot(
            TerrainBuildResult terrain,
            PlayerGridController mageController,
            PlayerInputReader mageInputReader,
            GameObject mageSkillMenuRoot,
            PlayerGridController warriorController,
            PlayerInputReader warriorInputReader,
            GameObject warriorSkillMenuRoot,
            CameraFollowRig followRig,
            SimpleMessagePanel messagePanel,
            int spawnX = SpawnX,
            int spawnY = SpawnY)
        {
            var systemsGo = new GameObject("Systems", typeof(InteractionTrigger), typeof(SceneComposer));
            var interactionTrigger = systemsGo.GetComponent<InteractionTrigger>();
            var sceneComposer = systemsGo.GetComponent<SceneComposer>();

            AssignField(sceneComposer, "gridMapBuilder", terrain.GridMapBuilder);
            AssignField(sceneComposer, "mageController", mageController);
            AssignField(sceneComposer, "mageInputReader", mageInputReader);
            AssignField(sceneComposer, "mageSkillMenuRoot", mageSkillMenuRoot);
            AssignField(sceneComposer, "warriorController", warriorController);
            AssignField(sceneComposer, "warriorInputReader", warriorInputReader);
            AssignField(sceneComposer, "warriorSkillMenuRoot", warriorSkillMenuRoot);
            AssignField(sceneComposer, "cameraFollowRig", followRig);
            AssignField(sceneComposer, "interactionTrigger", interactionTrigger);
            AssignField(sceneComposer, "messagePanel", messagePanel);
            AssignField(sceneComposer, "playerSpawnX", spawnX);
            AssignField(sceneComposer, "playerSpawnY", spawnY);
            AssignField(sceneComposer, "interactableZones", new System.Collections.Generic.List<InteractableZone>(terrain.InteractableZones));
        }

        internal static Vector3 CellCenter(int x, int y)
        {
            // Delegates to the single conversion source (GridWorldConversion)
            // instead of re-deriving corner vs. center math here. Runtime
            // player position is overwritten by PlayerGridController.Initialize
            // (also via GridWorldConversion) and the camera converges onto the
            // player every frame, so this only affected the editor-preview
            // (pre-Play) position - fixed for consistency, not a runtime bug.
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            return new Vector3(world.X, world.Y, 0f);
        }

        private static Sprite LoadNamedSprite(string path, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null)
            {
                throw new Exception($"Sprite '{name}' not found at {path}");
            }

            return sprite;
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found on {type.Name}");
            }

            field.SetValue(target, value);
        }
    }
}
