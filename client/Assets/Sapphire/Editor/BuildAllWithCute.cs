using UnityEditor;
using UnityEngine;

public static class BuildAllWithCute
{
    public static void Run()
    {
        Sapphire.EditorTools.ArtImportConfigurator.ConfigureArtImportSettings();
        
        // Rebuild scenes with the new terrain layout
        Sapphire.EditorTools.SapphireSceneBuilder.BuildAll();
        
        // Restore TilemapRenderers (undo any old baking)
        MapBaker.BakeAll();
        
        string[] scenes = new string[] {
            "Assets/Sapphire/Scenes/Login.unity",
            "Assets/Sapphire/Scenes/CharacterSelect.unity",
            "Assets/Sapphire/Scenes/CharacterCreate.unity",
            "Assets/Sapphire/Scenes/VillageHub.unity",
            "Assets/Sapphire/Scenes/SlimeKingdom.unity"
        };
        
        BuildPipeline.BuildPlayer(scenes, "../builds/Windows/SapphireRPG.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("ALL DONE!");
    }
}
