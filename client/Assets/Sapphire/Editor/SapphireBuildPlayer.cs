using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sapphire.EditorTools
{
    public static class SapphireBuildPlayer
    {
        public static void BuildWindows()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../builds/Windows/SapphireRPG.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            PlayerSettings.productName = "Sapphire RPG";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Sapphire/Scenes/VillageHub.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
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
