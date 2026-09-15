using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sapphire.EditorTools
{
    public static class SapphireBuildPlayer
    {
        /// <summary>
        /// Playtest build entry point (default, e.g. -executeMethod
        /// Sapphire.EditorTools.SapphireBuildPlayer.BuildWindows). Not a
        /// Development build - see REMEDIATION_PLAN.md Phase 1 item 4:
        /// Development builds draw an on-screen diagnostics overlay
        /// (PixelPerfectCamera's warning banner was one instance of this,
        /// see docs/HANDOFF.md's 2026-09-16 entry #4) that has no business
        /// being visible to a real player. Use
        /// <see cref="BuildWindowsDevelopment"/> when a Development build is
        /// actually needed (profiler, script debugging, dev console).
        /// </summary>
        public static void BuildWindows()
        {
            Build(BuildOptions.None);
        }

        /// <summary>
        /// Development build entry point - kept as a separate, explicit
        /// method rather than a flag so a plain -executeMethod invocation
        /// can't accidentally produce a dev build again.
        /// </summary>
        public static void BuildWindowsDevelopment()
        {
            Build(BuildOptions.Development);
        }

        private static void Build(BuildOptions options)
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../builds/Windows/SapphireRPG.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            PlayerSettings.productName = "Sapphire RPG";
            // 2026-09-15 (Phase 1): landscape default window, matching the
            // CanvasScaler/camera fix below and docs/planning/01_PRODUCT.md's
            // "가로 화면, 1280x720 기준" - was 720x1280 (portrait), the same
            // stale value the canvas/camera carried.
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            // 2026-09-15 (Phase 1 addendum): "Default Is Native Resolution" was
            // left at its Unity-template default (true). In Windowed mode that
            // makes Unity size the FIRST launch's window to the desktop's full
            // native resolution, ignoring defaultScreenWidth/Height above
            // entirely - the actual mechanism behind the "실행하면 창이 너무
            // 크다" complaint (a plain double-click launch with no saved
            // registry prefs and no -screen-width/-height args). Turned off so
            // a fresh launch honors the 1280x720 default above.
            PlayerSettings.defaultIsNativeResolution = false;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Sapphire/Scenes/VillageHub.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("SAPPHIRE_PLAYER_BUILD FAILED: " + report.summary.result + " errors=" + report.summary.totalErrors);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("SAPPHIRE_PLAYER_BUILD SUCCESS size=" + report.summary.totalSize);
        }
    }
}
