using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sapphire.Editor
{
    public static class BuildGame
    {
        const string Root="Assets/Sapphire";
        const string Scene=Root+"/Scenes/Boot.unity";
        public static void BuildAndTest()
        {
            ConfigureArt();
            Sapphire.Tests.CombatChecks.RunAll();Debug.Log("CombatChecks passed");
            Sapphire.Tests.WorldChecks.RunAll();Debug.Log("WorldChecks passed");
            Debug.Log(CampaignChecks.RunAll());
            CreateScene();
            BuildWindows();
            Debug.Log("SAPPHIRE_BUILD SUCCESS");
        }
        public static void TestOnly()
        {
            ConfigureArt();
            Sapphire.Tests.CombatChecks.RunAll();Debug.Log("CombatChecks passed");
            Sapphire.Tests.WorldChecks.RunAll();Debug.Log("WorldChecks passed");
            Debug.Log(CampaignChecks.RunAll());
            CreateScene();
            Debug.Log("SAPPHIRE_TESTS SUCCESS");
        }
        static void ConfigureArt()
        {
            foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Art"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.alphaIsTransparency=importer.DoesSourceTextureHaveAlpha();importer.SaveAndReimport();
            }
        }
        static void CreateScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"Sapphire/Scenes"));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var host=new GameObject("Sapphire RPG",typeof(GameApp),typeof(GamePresenter));
            var presenter=host.GetComponent<GamePresenter>();
            presenter.Art=AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Art"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(x=>!x.EndsWith("MageDirectional.png",StringComparison.OrdinalIgnoreCase)&&!x.EndsWith("-source.png",StringComparison.OrdinalIgnoreCase)).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).Where(x=>x!=null).ToArray();
            presenter.UiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKkr-Regular.otf");
            if(presenter.UiFont==null)throw new Exception("Korean UI font missing");
            if(!EditorSceneManager.SaveScene(scene,Scene))throw new Exception("Boot scene save failed");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();
        }
        static void BuildWindows()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../builds/Windows/SapphireRPG.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.productName="Sapphire: Moonlit Rift";PlayerSettings.companyName="Sapphire Studio";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result+" errors="+report.summary.totalErrors);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"BUILD-VERIFIED.txt"),DateTime.UtcNow.ToString("O")+"\nUnity "+Application.unityVersion+"\n"+report.summary.totalSize+" bytes\n");
        }
    }
}
