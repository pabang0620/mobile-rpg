using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;

public static class VillageHubCapture
{
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Sapphire/Scenes/VillageHub.unity");
        Camera cam = GameObject.FindAnyObjectByType<Camera>();
        if (cam == null) {
            Debug.LogError("No camera found!");
            return;
        }
        
        RenderTexture rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        
        Texture2D screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        
        File.WriteAllBytes("C:/Users/darac/.gemini/antigravity/brain/30c79ebb-9e0c-4432-9d21-cdbfd419c0cb/villagehub_preview_current.png", screenShot.EncodeToPNG());
        Debug.Log("VillageHub preview captured!");
    }
}
