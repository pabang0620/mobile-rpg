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
                "MagePose02.png", "MagePose06.png", "MagePose09.png", "MagePose14.png",
                "Backgrounds/WinterTreesFar.png", "Backgrounds/WinterTreesMid.png", "Backgrounds/WinterTreesNear.png", "Backgrounds/CaveCrystalRidgeA.png", "Backgrounds/CaveCrystalRidgeB.png",
                "VFX/MagicMissile.png",
                "Enemies/GoblinPixelArtIdle.png", "Enemies/GoblinPixelArtRun.png", "Enemies/GoblinPixelArtAttack.png", "Enemies/GoblinPixelArtDeath.png", "Enemies/GoblinMonsterSpritesheet32.png", "Enemies/GoblinMonsterFrame.png",
                "UI/InventoryShopIcons.png", "UI/FantasyPanelBorder.png"
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

            var newAlphaAssets = new[]
            {
                "Backgrounds/WinterTreesFar.png", "Backgrounds/WinterTreesMid.png", "Backgrounds/WinterTreesNear.png", "Backgrounds/CaveCrystalRidgeA.png", "Backgrounds/CaveCrystalRidgeB.png",
                "VFX/MagicMissile.png",
                "Enemies/GoblinPixelArtIdle.png", "Enemies/GoblinPixelArtRun.png", "Enemies/GoblinPixelArtAttack.png", "Enemies/GoblinPixelArtDeath.png", "Enemies/GoblinMonsterSpritesheet32.png", "Enemies/GoblinMonsterFrame.png",
                "UI/InventoryShopIcons.png", "UI/FantasyPanelBorder.png"
            };
            foreach (var file in newAlphaAssets)
                Require(HasUsableAlpha(Root + "/Art/" + file), file + " must contain visible and transparent pixels (real alpha, not an RGB checkerboard).");
            Debug.Log("LIGHTHAVEN_2D_ASSET_CHECKS PASS 11+14 alpha assets, 16+14 imports");
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

            RunAutoHuntChecks(passed);

            Debug.Log("LIGHTHAVEN_2D_CHECKS PASS " + passed.Count + " " + string.Join(",", passed));
        }

        // M2b AUTOHUNT_DECISION_TECH_SPEC boundary checks: target priority, engage overriding
        // Return/Reposition, the self-lock regression, the 0.2s decision cache, and low-HP auto potion.
        static void RunAutoHuntChecks(List<string> passed)
        {
            var lowestHp = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            lowestHp.Enemies[0].Position = new Vector2(700, BattleSession.GroundTop + 70); lowestHp.Enemies[0].Hp = 40;
            lowestHp.Enemies[1].Position = new Vector2(660, BattleSession.GroundTop + 70); lowestHp.Enemies[1].Hp = 80;
            lowestHp.Enemies[2].Position = new Vector2(3000, BattleSession.GroundTop + 70);
            Step(lowestHp, 1f / 60f);
            Require(lowestHp.AutoDebug.TargetId == lowestHp.Enemies[0].Id, "Lowest-HP in-range enemy was not prioritized over a nearer, healthier one.");
            passed.Add("auto-target-lowest-hp-priority");

            var tieBreak = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            tieBreak.Enemies[0].Position = new Vector2(750, BattleSession.GroundTop + 70); tieBreak.Enemies[0].Hp = 80;
            tieBreak.Enemies[1].Position = new Vector2(660, BattleSession.GroundTop + 70); tieBreak.Enemies[1].Hp = 80;
            tieBreak.Enemies[2].Position = new Vector2(3000, BattleSession.GroundTop + 70);
            Step(tieBreak, 1f / 60f);
            Require(tieBreak.AutoDebug.TargetId == tieBreak.Enemies[1].Id, "Equal-HP tie-break did not select the nearest enemy.");
            passed.Add("auto-target-tie-break-nearest");

            var targeted = NewCloseSession();
            targeted.Enemies[1].Position = new Vector2(670, BattleSession.GroundTop + 70); targeted.Enemies[1].Spawn = targeted.Enemies[1].Position;
            Require(targeted.Attack(-1, false, targeted.Enemies[0].Id), "targetId-forced attack did not begin.");
            Step(targeted, .5f);
            Require(targeted.Enemies[0].Hp < targeted.Enemies[0].MaxHp && targeted.Enemies[1].Hp == targeted.Enemies[1].MaxHp,
                "Attack(targetId) hit the nearer enemy instead of the forced target.");
            passed.Add("attack-targetid-override");

            var fallback = NewCloseSession();
            fallback.Enemies[1].Position = new Vector2(700, BattleSession.GroundTop + 70); fallback.Enemies[1].Spawn = fallback.Enemies[1].Position;
            fallback.Enemies[0].Hp = 0;
            Require(fallback.Attack(-1, false, fallback.Enemies[0].Id), "Attack should fall back to Nearest() when the requested targetId is dead.");
            Step(fallback, .5f);
            Require(fallback.Enemies[1].Hp < fallback.Enemies[1].MaxHp, "Dead targetId did not fall back to Nearest().");
            passed.Add("attack-targetid-dead-fallback");

            var stuck = new BattleSession { Hero = new Vector2(BattleSession.MovableRangeMaxX, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            // Recover > 0 freezes each enemy's own chase AI so its position is never touched (and
            // never re-clamped into bounds by that unrelated code path) - isolates the hero being
            // pinned against the world edge as the only source of "no progress" in this fixture.
            stuck.Enemies[0].Position = new Vector2(BattleSession.MovableRangeMaxX + 500, BattleSession.GroundTop + 70); stuck.Enemies[0].Recover = 999f;
            stuck.Enemies[1].Position = new Vector2(BattleSession.MovableRangeMaxX + 600, BattleSession.GroundTop + 70); stuck.Enemies[1].Recover = 999f;
            stuck.Enemies[2].Position = new Vector2(BattleSession.MovableRangeMaxX + 700, BattleSession.GroundTop + 70); stuck.Enemies[2].Recover = 999f;
            Step(stuck, 2.5f);
            Require(stuck.AutoDebug.State == AutoHuntState.Reposition, "No progress against the world edge for 2.5s did not escalate to Reposition.");
            stuck.Enemies[0].Position = new Vector2(BattleSession.MovableRangeMaxX - 100, BattleSession.GroundTop + 70);
            stuck.Enemies[0].Hp = stuck.Enemies[0].MaxHp;
            Step(stuck, .25f);
            Require(stuck.AutoDebug.State == AutoHuntState.Engage, "Engage did not override Reposition once an enemy entered range.");
            passed.Add("auto-engage-overrides-reposition");

            var dodgeSelfLock = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            var dodgeManualBefore = dodgeSelfLock.ManualUntil;
            Require(dodgeSelfLock.Dodge(Vector2.left, false), "Auto dodge (manual:false) did not execute.");
            Require(Mathf.Approximately(dodgeSelfLock.ManualUntil, dodgeManualBefore), "Auto dodge triggered the self-lock bug (ManualUntil changed).");

            var jumpSelfLock = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            var jumpManualBefore = jumpSelfLock.ManualUntil;
            Require(jumpSelfLock.Jump(false), "Auto jump (manual:false) did not execute.");
            Require(Mathf.Approximately(jumpSelfLock.ManualUntil, jumpManualBefore), "Auto jump triggered the self-lock bug (ManualUntil changed).");

            var potionSelfLock = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            potionSelfLock.Hp = 50;
            var potionManualBefore = potionSelfLock.ManualUntil;
            Require(potionSelfLock.UseHpPotion(false), "Auto HP potion (manual:false) did not execute.");
            Require(Mathf.Approximately(potionSelfLock.ManualUntil, potionManualBefore), "Auto HP potion triggered the self-lock bug (ManualUntil changed).");
            passed.Add("auto-self-lock-regression");

            var autoPotion = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            autoPotion.Hp = 40;
            autoPotion.Enemies[0].Position = new Vector2(3000, BattleSession.GroundTop + 70);
            autoPotion.Enemies[1].Position = new Vector2(3200, BattleSession.GroundTop + 70);
            autoPotion.Enemies[2].Position = new Vector2(3400, BattleSession.GroundTop + 70);
            var autoPotionManualBefore = autoPotion.ManualUntil;
            var potionsBefore = autoPotion.HpPotions;
            Step(autoPotion, 1f / 60f);
            Require(autoPotion.Hp == 100, "Auto HP potion did not heal by the fixed 60 HP amount below the 35% threshold.");
            Require(autoPotion.HpPotions == potionsBefore - 1, "Auto HP potion did not consume exactly one potion.");
            Require(Mathf.Approximately(autoPotion.ManualUntil, autoPotionManualBefore), "Auto HP potion triggered the self-lock bug (ManualUntil changed).");
            passed.Add("auto-low-hp-potion");

            var noPotions = new BattleSession { Hero = new Vector2(650, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            noPotions.Hp = 10; noPotions.HpPotions = 0;
            noPotions.Enemies[0].Position = new Vector2(3000, BattleSession.GroundTop + 70);
            noPotions.Enemies[1].Position = new Vector2(3200, BattleSession.GroundTop + 70);
            noPotions.Enemies[2].Position = new Vector2(3400, BattleSession.GroundTop + 70);
            Step(noPotions, .5f);
            Require(!noPotions.Dead && noPotions.Hp == 10, "Depleted HP potions should be a no-op, not a crash or a phantom heal.");
            passed.Add("auto-no-potions-no-crash");

            var caching = new BattleSession { Hero = new Vector2(0, BattleSession.GroundTop + BattleSession.HeroHalfHeight) };
            caching.Enemies[0].Position = new Vector2(500, BattleSession.GroundTop + 70);
            caching.Enemies[1].Position = new Vector2(3000, BattleSession.GroundTop + 70);
            caching.Enemies[2].Position = new Vector2(3200, BattleSession.GroundTop + 70);
            Step(caching, 1f / 60f);
            var firstTargetId = caching.AutoDebug.TargetId;
            caching.Enemies[1].Position = new Vector2(10, BattleSession.GroundTop + 70);
            Step(caching, .05f);
            Require(caching.AutoDebug.TargetId == firstTargetId, "Cached intent changed target before the 0.2s decision boundary elapsed.");
            passed.Add("auto-decision-cache-window");
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
            screen.parallaxFarLayer = Load<Texture2D>(Root + "/Art/Backgrounds/WinterTreesFar.png");
            screen.parallaxNearLayer = Load<Texture2D>(Root + "/Art/Backgrounds/CaveCrystalRidgeB.png");
            screen.vfxMagicMissileSheet = Load<Texture2D>(Root + "/Art/VFX/MagicMissile.png");
            screen.goblinVariantPixelArt = Load<Texture2D>(Root + "/Art/Enemies/GoblinPixelArtIdle.png");
            screen.goblinVariantMonster = Load<Texture2D>(Root + "/Art/Enemies/GoblinMonsterFrame.png");
            screen.inventoryIconSheet = Load<Texture2D>(Root + "/Art/UI/InventoryShopIcons.png");
            screen.fantasyPanelBorder = Load<Texture2D>(Root + "/Art/UI/FantasyPanelBorder.png");

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
