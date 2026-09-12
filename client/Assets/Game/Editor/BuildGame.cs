using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lighthaven2D.Editor
{
    public static class BuildGame
    {
        const string Root = "Assets/Game";
        const string ScenePath = Root + "/Scenes/Battle.unity";

        public static void BuildAndTest()
        {
            try
            {
                ConfigureTextures();
                RunDomainChecks();
                CreateBattleScene();
                BuildWindowsPlayer();
                Debug.Log("LIGHTHAVEN_2D_BUILD SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LIGHTHAVEN_2D_BUILD FAILED");
                throw;
            }
        }

        static void ConfigureTextures()
        {
            var files = new[]
            {
                "MoonCourtyard.png", "SapphireTown.png", "SapphirePlatformMap.png", "MageSkills.png", "MainMenuIcons.png", "HudControls.png", "UiChrome.png", "WideButton.png", "MenuPanel.png", "MagePortrait.png", "Goblin.png", "MageIdle.png",
                "MagePose02.png", "MagePose06.png", "MagePose09.png", "MagePose14.png"
            };
            foreach (var file in files)
            {
                var path = Root + "/Art/" + file;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, "Missing texture importer: " + path);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                var hasSourceAlpha = importer.DoesSourceTextureHaveAlpha();
                importer.alphaIsTransparency = hasSourceAlpha;
                importer.isReadable = hasSourceAlpha;
                importer.SaveAndReimport();
            }

            Require(HasUsableAlpha(Root + "/Art/MageIdle.png"), "MageIdle must contain visible and transparent pixels.");
            Require(HasUsableAlpha(Root + "/Art/Goblin.png"), "Goblin must contain visible and transparent pixels.");
            for (var i = 0; i < 4; i++)
                Require(HasUsableAlpha(Root + "/Art/MagePose" + new[] { "02", "06", "09", "14" }[i] + ".png"), "Mage pose must contain visible and transparent pixels.");
            Require(HasUsableAlpha(Root + "/Art/MenuPanel.png"), "MenuPanel must contain a real transparent exterior.");
            Require(HasUsableAlpha(Root + "/Art/MagePortrait.png"), "MagePortrait must contain a real transparent exterior.");
            Require(HasUsableAlpha(Root + "/Art/HudControls.png"), "HudControls must contain real transparent glyph exteriors.");
            Require(HasUsableAlpha(Root + "/Art/UiChrome.png"), "UiChrome must contain transparent reusable frames.");
            Require(HasUsableAlpha(Root + "/Art/WideButton.png"), "WideButton must contain transparent exterior.");
            Debug.Log("LIGHTHAVEN_2D_ASSET_CHECKS PASS 11 alpha assets, 16 imports");
        }

        static bool HasUsableAlpha(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.DoesSourceTextureHaveAlpha()) return false;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || !texture.isReadable) return false;
            var transparent = false;
            var visible = false;
            foreach (var pixel in texture.GetPixels32())
            {
                transparent |= pixel.a == 0;
                visible |= pixel.a > 0;
                if (transparent && visible) return true;
            }
            return false;
        }

        static void RunDomainChecks()
        {
            var passed = new List<string>();

            var idle = new BattleSession();
            idle.ToggleAuto();
            var idlePosition = idle.Hero;
            Step(idle, 1f);
            Require(Vector2.Distance(idlePosition, idle.Hero) < .01f, "AUTO OFF moved the hero.");
            passed.Add("auto-off-idle");

            var manual = new BattleSession();
            manual.Tick(.02f, Vector2.left);
            Require(!manual.AutoActing && manual.ManualUntil > manual.Time, "Manual input did not pause auto hunt.");
            passed.Add("manual-auto-pause");

            var jump = new BattleSession();
            Require(jump.Jump(), "Grounded hero could not jump.");
            Step(jump, .2f);
            Require(!jump.Grounded && jump.Hero.y > BattleSession.GroundTop + BattleSession.HeroHalfHeight, "Jump did not gain height.");
            Step(jump, 1.5f);
            Require(jump.Grounded && Mathf.Abs(jump.SupportTop - jump.Platforms[0].Top) < .1f, "Hero did not land on the left platform.");
            passed.Add("jump-platform-landing");

            var noVerticalWalk = new BattleSession();
            noVerticalWalk.ToggleAuto();
            var walkY = noVerticalWalk.Hero.y;
            Step(noVerticalWalk, .5f, Vector2.up);
            Require(Mathf.Abs(noVerticalWalk.Hero.y - walkY) < .01f, "Vertical movement input changed platform height.");
            passed.Add("horizontal-only-walk");

            var autoSkill = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            autoSkill.Enemies[0].Position = new Vector2(800, BattleSession.GroundTop + 70);
            autoSkill.Enemies[1].Position = new Vector2(930, BattleSession.GroundTop + 70);
            Step(autoSkill, .5f);
            Require(autoSkill.Mp < 100 && autoSkill.Enemies[0].Hp < autoSkill.Enemies[0].MaxHp,
                "Auto hunt did not select and resolve an available skill.");
            passed.Add("auto-skill-priority");

            var hit = NewCloseSession();
            var hitEnemy = hit.Enemies[0];
            Require(hit.Attack(-1, false), "Basic attack did not begin.");
            Step(hit, .5f);
            var hpAfterImpact = hitEnemy.Hp;
            Require(hpAfterImpact == hitEnemy.MaxHp - 32, "Basic attack fixture damage changed.");
            Step(hit, .35f);
            Require(hitEnemy.Hp == hpAfterImpact, "One attack applied damage more than once.");
            passed.Add("single-hit-window");

            var skill = NewCloseSession();
            Require(skill.Attack(0, false), "Skill 1 did not begin.");
            Require(Mathf.Approximately(skill.Mp, 88) && Mathf.Approximately(skill.Cooldowns[0], 2.8f), "Skill cost/cooldown mismatch.");
            Require(!skill.Attack(0, false) && Mathf.Approximately(skill.Mp, 88), "Rejected skill spent MP.");
            passed.Add("skill-transaction");

            var dodge = NewCloseSession();
            dodge.Enemies[0].Position = new Vector2(650, BattleSession.GroundTop + 70);
            dodge.Enemies[0].Windup = .1f;
            dodge.Enemies[0].Facing = Vector2.up;
            var beforeDodge = dodge.Hp;
            Require(dodge.Dodge(Vector2.left), "Dodge did not begin.");
            Step(dodge, .2f);
            Require(dodge.Hp == beforeDodge, "Dodge invulnerability failed.");
            passed.Add("dodge-invulnerability");

            var potion = NewCloseSession();
            potion.Hp = 70; potion.Mp = 20;
            Require(potion.UseHpPotion() && potion.Hp == 130 && potion.HpPotions == 9, "HP potion transaction failed.");
            Require(potion.UseMpPotion() && Mathf.Approximately(potion.Mp, 65) && potion.MpPotions == 9, "MP potion transaction failed.");
            passed.Add("potion-transactions");

            var reward = NewCloseSession();
            Require(reward.Attack(0, false), "First reward attack did not begin.");
            Step(reward, 3f);
            Require(reward.Attack(0, false), "Second reward attack did not begin.");
            Step(reward, .5f);
            Require(reward.Kills == 1 && reward.Xp == 25 && reward.Gold == 12, "Kill reward was not granted exactly once.");
            Step(reward, 1f);
            Require(reward.Kills == 1 && reward.Xp == 25 && reward.Gold == 12, "Dead enemy rewarded more than once.");
            passed.Add("single-kill-reward");

            var closed = NewCloseSession();
            closed.Close();
            var closedPosition = closed.Hero;
            closed.Tick(1f, Vector2.right);
            Require(closed.Closed && Vector2.Distance(closedPosition, closed.Hero) < .01f && !closed.Attacking, "Closed session continued simulation.");
            passed.Add("session-close");

            Debug.Log("LIGHTHAVEN_2D_CHECKS PASS " + passed.Count + " " + string.Join(",", passed));
        }

        static BattleSession NewCloseSession()
        {
            var session = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            session.ToggleAuto();
            session.Enemies[0].Position = new Vector2(800, BattleSession.GroundTop + 70);
            session.Enemies[0].Spawn = session.Enemies[0].Position;
            return session;
        }

        static void Step(BattleSession session, float seconds, Vector2 input = default)
        {
            var remaining = seconds;
            while (remaining > 0)
            {
                var delta = Mathf.Min(1f / 60f, remaining);
                session.Tick(delta, input);
                remaining -= delta;
            }
        }

        static void CreateBattleScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Game/Scenes"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var host = new GameObject("Front Flow", typeof(FrontFlowScreen));
            var screen = host.GetComponent<FrontFlowScreen>();
            screen.town = Load<Texture2D>(Root + "/Art/SapphireTown.png");
            screen.uiChromeSheet = Load<Texture2D>(Root + "/Art/UiChrome.png");
            screen.wideButtonTexture = Load<Texture2D>(Root + "/Art/WideButton.png");
            screen.uiFont = Load<Font>(Root + "/Fonts/NotoSansCJKkr-Regular.otf");
            screen.background = Load<Texture2D>(Root + "/Art/SapphirePlatformMap.png");
            screen.mageIdle = Load<Texture2D>(Root + "/Art/MageIdle.png");
            screen.magePortrait = Load<Texture2D>(Root + "/Art/MagePortrait.png");
            screen.goblin = Load<Texture2D>(Root + "/Art/Goblin.png");
            screen.skillSheet = Load<Texture2D>(Root + "/Art/MageSkills.png");
            screen.menuIconSheet = Load<Texture2D>(Root + "/Art/MainMenuIcons.png");
            screen.hudControlSheet = Load<Texture2D>(Root + "/Art/HudControls.png");
            screen.uiChromeSheet = Load<Texture2D>(Root + "/Art/UiChrome.png");
            screen.menuPanelTexture = Load<Texture2D>(Root + "/Art/MenuPanel.png");
            screen.mageAttackPoses = new[]
            {
                Load<Texture2D>(Root + "/Art/MagePose02.png"),
                Load<Texture2D>(Root + "/Art/MagePose06.png"),
                Load<Texture2D>(Root + "/Art/MagePose09.png"),
                Load<Texture2D>(Root + "/Art/MagePose14.png")
            };

            Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save battle scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, "Missing asset: " + path);
            return asset;
        }

        static void BuildWindowsPlayer()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../builds/Windows/Lighthaven2D.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.productName = "Lighthaven 2D";
            PlayerSettings.companyName = "Lighthaven";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Require(report.summary.result == BuildResult.Succeeded,
                "Player build failed: " + report.summary.result + " / " + report.summary.totalErrors + " errors");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
