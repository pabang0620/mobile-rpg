using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using Sapphire.EditorTools;
using System.IO;

public static class PreviewCapture
{
    public static void CaptureVillageHub()
    {
        // Force asset refresh so our new PNGs are imported
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ArtImportConfigurator.ConfigureArtImportSettings();
        
        // Build the scene
        SapphireSceneBuilder.BuildAll();
        
        // Open the scene to capture it
        var scene = EditorSceneManager.OpenScene("Assets/Sapphire/Scenes/VillageHub.unity", OpenSceneMode.Single);
        
        // Setup Camera
        Camera cam = new GameObject("CaptureCamera").AddComponent<Camera>();
        cam.orthographic = true;
        // Map is 32x32, centered around (16, 16)
        cam.orthographicSize = 16f; 
        cam.transform.position = new Vector3(16f, 16f, -10f);
        
        // Render
        RenderTexture rt = new RenderTexture(2048, 2048, 24);
        cam.targetTexture = rt;
        cam.Render();
        
        // Save
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 2048, 2048), 0, 0);
        tex.Apply();
        
        byte[] bytes = tex.EncodeToPNG();
        string path = "C:/Users/darac/.gemini/antigravity/brain/30c79ebb-9e0c-4432-9d21-cdbfd419c0cb/village_hub_preview_v4.png";
        File.WriteAllBytes(path, bytes);
        
        Debug.Log("VILLAGE_PREVIEW_SUCCESS: " + path);
        
        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}
