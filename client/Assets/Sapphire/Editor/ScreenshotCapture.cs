using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    public static class ScreenshotCapture 
    {
        public static void TakeScreenshot() 
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Sapphire/Scenes/Login.unity");
            
            Camera cam = Camera.main;
            if (cam == null)
            {
                var cameraGo = GameObject.Find("Main Camera");
                if (cameraGo != null) cam = cameraGo.GetComponent<Camera>();
            }
            
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes("login.png", bytes);
                Debug.Log("Screenshot saved to village_hub.png");
            }
            else
            {
                Debug.LogError("Main Camera not found!");
            }
        }
    }
}
