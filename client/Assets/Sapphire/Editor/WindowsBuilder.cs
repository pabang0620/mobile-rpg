using UnityEditor;
using UnityEngine;

public static class WindowsBuilder
{
    public static void Build()
    {
        string[] scenes = new string[] {
            "Assets/Sapphire/Scenes/Login.unity",
            "Assets/Sapphire/Scenes/CharacterSelect.unity",
            "Assets/Sapphire/Scenes/CharacterCreate.unity",
            "Assets/Sapphire/Scenes/VillageHub.unity",
            "Assets/Sapphire/Scenes/SlimeKingdom.unity"
        };
        
        BuildPipeline.BuildPlayer(scenes, "../builds/Windows/SapphireRPG.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("Windows Build Successful!");
    }
}
