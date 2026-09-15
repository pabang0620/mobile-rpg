using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Sapphire.Composition;
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
        internal const string GeneratedDir = "Assets/Sapphire/Generated";

        // 2026-09-14 player feedback: 14x10 felt cramped to walk around in.
        // Widened to 24x18 (same border/fence/path layout logic, new coords).
        internal const int MapWidth = 24;
        internal const int MapHeight = 18;
        internal const int SignX = 12;
        internal const int SignY = 11;
        internal const int SpawnX = 12;
        internal const int SpawnY = 6;

        public static void BuildAll()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("SapphireSceneBuilder.BuildAll()은 배치모드 전용입니다. 대화형 에디터에서 저장 안 된 씬을 날릴 수 있어 실행을 건너뜁니다.");
                return;
            }

            try
            {
                ArtImportConfigurator.ConfigureArtImportSettings();
                SkillVfxImporter.ConfigureLibrary();
                BuildVillageHubScene();
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
            (PlayerGridController playerController, PlayerInputReader playerInputReader, SkillCastFeedback castFeedback) = BuildPlayer();
            CameraFollowRig followRig = BuildCamera(playerController.transform.position, terrain.GroundTilemap);
            UiBuildResult ui = VillageHubUiBuilder.Build(playerController, playerInputReader, castFeedback);

            ComposeSceneRoot(terrain, playerController, playerInputReader, followRig, ui.MessagePanel);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new Exception("Scene save failed");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        // --- Player ---
        private static (PlayerGridController controller, PlayerInputReader inputReader, SkillCastFeedback castFeedback) BuildPlayer()
        {
            const string sheet = RootArtDir + "/MageTopdownGridSheet.png";

            Sprite idleUp = LoadNamedSprite(sheet, "Mage_Up_Idle");
            Sprite idleDown = LoadNamedSprite(sheet, "Mage_Down_Idle");
            Sprite idleLeft = LoadNamedSprite(sheet, "Mage_Left_Idle");
            Sprite idleRight = LoadNamedSprite(sheet, "Mage_Right_Idle");

            Sprite walkAUp = LoadNamedSprite(sheet, "Mage_Up_WalkA");
            Sprite walkADown = LoadNamedSprite(sheet, "Mage_Down_WalkA");
            Sprite walkALeft = LoadNamedSprite(sheet, "Mage_Left_WalkA");
            Sprite walkARight = LoadNamedSprite(sheet, "Mage_Right_WalkA");

            Sprite walkBUp = LoadNamedSprite(sheet, "Mage_Up_WalkB");
            Sprite walkBDown = LoadNamedSprite(sheet, "Mage_Down_WalkB");
            Sprite walkBLeft = LoadNamedSprite(sheet, "Mage_Left_WalkB");
            Sprite walkBRight = LoadNamedSprite(sheet, "Mage_Right_WalkB");

            var playerGo = new GameObject("Player", typeof(SpriteRenderer), typeof(PlayerInputReader), typeof(GridMoveAnimator), typeof(DirectionalSpriteAnimator), typeof(PlayerGridController));
            playerGo.transform.position = CellCenter(SpawnX, SpawnY);
            playerGo.GetComponent<SpriteRenderer>().sprite = idleDown;
            playerGo.GetComponent<SpriteRenderer>().sortingOrder = 0;

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
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
            cameraGo.transform.position = playerPosition + new Vector3(0f, 0f, -10f);

            var followRig = cameraGo.AddComponent<CameraFollowRig>();
            followRig.SetGroundTilemap(groundTilemap);
            return followRig;
        }

        // --- Composition root ---
        private static void ComposeSceneRoot(
            TerrainBuildResult terrain,
            PlayerGridController playerController,
            PlayerInputReader playerInputReader,
            CameraFollowRig followRig,
            SimpleMessagePanel messagePanel)
        {
            var systemsGo = new GameObject("Systems", typeof(InteractionTrigger), typeof(SceneComposer));
            var interactionTrigger = systemsGo.GetComponent<InteractionTrigger>();
            var sceneComposer = systemsGo.GetComponent<SceneComposer>();

            AssignField(sceneComposer, "gridMapBuilder", terrain.GridMapBuilder);
            AssignField(sceneComposer, "playerController", playerController);
            AssignField(sceneComposer, "playerInputReader", playerInputReader);
            AssignField(sceneComposer, "cameraFollowRig", followRig);
            AssignField(sceneComposer, "interactionTrigger", interactionTrigger);
            AssignField(sceneComposer, "messagePanel", messagePanel);
            AssignField(sceneComposer, "playerSpawnX", SpawnX);
            AssignField(sceneComposer, "playerSpawnY", SpawnY);
            AssignField(sceneComposer, "interactableZones", new System.Collections.Generic.List<InteractableZone> { terrain.SignpostZone });
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
