using System;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// One-shot tool that configures the Login/CharacterSelect/CharacterCreate
    /// art import settings and builds all three scenes from scratch - the
    /// character-flow counterpart to <see cref="SapphireSceneBuilder"/>
    /// (VillageHub). Kept as a SEPARATE top-level entry point rather than
    /// folded into SapphireSceneBuilder.BuildAll, so every existing
    /// invocation of that method keeps working exactly as before (VillageHub
    /// only) - run this one (-executeMethod
    /// Sapphire.EditorTools.CharacterFlowSceneBuilder.BuildAll) once the
    /// Art/UI/Title/* files exist. Safe to re-run, same idempotent pattern as
    /// SapphireSceneBuilder.BuildAll.
    /// </summary>
    public static class CharacterFlowSceneBuilder
    {
        public static void BuildAll()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("CharacterFlowSceneBuilder.BuildAll()은 배치모드 전용입니다. 대화형 에디터에서 저장 안 된 씬을 날릴 수 있어 실행을 건너뜁니다.");
                return;
            }

            try
            {
                CharacterFlowArtImportConfigurator.ConfigureAll();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LoginSceneBuilder.Build();
                CharacterSelectSceneBuilder.Build();
                CharacterCreateSceneBuilder.Build();

                AssetDatabase.SaveAssets();
                Debug.Log("SAPPHIRE_CHARACTER_FLOW_BUILD SUCCESS");
            }
            catch (Exception e)
            {
                Debug.LogError("SAPPHIRE_CHARACTER_FLOW_BUILD FAILED: " + e);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
