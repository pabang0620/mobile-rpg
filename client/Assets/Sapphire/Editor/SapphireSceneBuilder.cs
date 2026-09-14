using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sapphire.Composition;
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
            CameraFollowRig followRig = BuildCamera(playerController.transform.position);
            UiBuildResult ui = VillageHubUiBuilder.Build(playerController, castFeedback);

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
            Sprite[] idleUp = { LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Up_0"), LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Up_1") };
            Sprite[] idleDown = { LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Down_0"), LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Down_1") };
            Sprite[] idleLeft = { LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Left_0"), LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Left_1") };
            Sprite[] idleRight = { LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Right_0"), LoadNamedSprite(RootArtDir + "/MageIdleDirectional.png", "MageIdle_Right_1") };

            Sprite[] walkUp = { LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Up_0"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Up_1"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Up_2") };
            Sprite[] walkDown = { LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Down_0"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Down_1"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Down_2") };
            Sprite[] walkLeft = { LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Left_0"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Left_1"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Left_2") };
            Sprite[] walkRight = { LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Right_0"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Right_1"), LoadNamedSprite(RootArtDir + "/MageWalk4x3-v2.png", "MageWalk_Right_2") };

            var playerGo = new GameObject("Player", typeof(SpriteRenderer), typeof(PlayerInputReader), typeof(GridMoveAnimator), typeof(DirectionalSpriteAnimator), typeof(PlayerGridController));
            playerGo.transform.position = CellCenter(SpawnX, SpawnY);
            playerGo.GetComponent<SpriteRenderer>().sprite = idleDown[0];
            playerGo.GetComponent<SpriteRenderer>().sortingOrder = 0;

            var spriteAnimator = playerGo.GetComponent<DirectionalSpriteAnimator>();
            AssignField(spriteAnimator, "idleUp", idleUp);
            AssignField(spriteAnimator, "idleDown", idleDown);
            AssignField(spriteAnimator, "idleLeft", idleLeft);
            AssignField(spriteAnimator, "idleRight", idleRight);
            AssignField(spriteAnimator, "walkUp", walkUp);
            AssignField(spriteAnimator, "walkDown", walkDown);
            AssignField(spriteAnimator, "walkLeft", walkLeft);
            AssignField(spriteAnimator, "walkRight", walkRight);

            var playerController = playerGo.GetComponent<PlayerGridController>();
            AssignField(playerController, "moveAnimator", playerGo.GetComponent<GridMoveAnimator>());
            AssignField(playerController, "spriteAnimator", spriteAnimator);

            var skillCastFeedback = playerGo.AddComponent<SkillCastFeedback>();
            AssignField(skillCastFeedback, "targetRenderer", playerGo.GetComponent<SpriteRenderer>());

            return (playerController, playerGo.GetComponent<PlayerInputReader>(), skillCastFeedback);
        }

        // --- Camera ---
        // 2026-09-14 player feedback: "화면이 너무 작다" - characters/tiles read as
        // tiny. Traced to PixelPerfectCameraInternal.CalculateCameraProperties
        // (com.unity.2d.pixel-perfect source): with no cropFrame/upscaleRT, it
        // computes orthoSize = screenHeight / (2 * zoom * assetsPPU), i.e. each
        // world unit ends up drawn at (zoom * assetsPPU) screen pixels. At the
        // project's default 720x1280 window (== refResolutionX/Y, so zoom==1),
        // assetsPPU=20 meant 1 tile = 20 screen px and the ~1.2-unit-tall player
        // sprite rendered ~24px tall on a 1280px-tall screen - the actual bug.
        // Raising assetsPPU (not lowering it - lowering shrinks things further,
        // since it's the divisor above) zooms in: assetsPPU=100 -> 100px/tile,
        // ~10 tiles tall / ~7.2 tiles wide visible, ~120px-tall player. Sprite
        // import PPUs (1024/512/320/302, see ArtImportConfigurator) are untouched -
        // they only fix each sprite's *world* size, independent of this
        // camera-side screen scale.
        private static CameraFollowRig BuildCamera(Vector3 playerPosition)
        {
            var cameraGo = new GameObject("Main Camera", typeof(UnityEngine.Camera));
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.4f; // editor-preview only; matches runtime orthoSize = 1280 / (2 * 100).
            cameraGo.transform.position = playerPosition + new Vector3(0f, 0f, -10f);

            var pixelPerfect = cameraGo.AddComponent<UnityEngine.U2D.PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 100;
            pixelPerfect.refResolutionX = 720;
            pixelPerfect.refResolutionY = 1280;
            pixelPerfect.upscaleRT = false;
            pixelPerfect.pixelSnapping = false; // painterly AI art, not blocky pixel art - avoid snap jitter during grid tween.
            pixelPerfect.cropFrameX = false;
            pixelPerfect.cropFrameY = false;
            pixelPerfect.stretchFill = false;

            return cameraGo.AddComponent<CameraFollowRig>();
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
            return new Vector3(x, y, 0f);
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
