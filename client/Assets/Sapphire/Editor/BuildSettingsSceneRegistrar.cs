using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Registers one scene path into EditorBuildSettings.scenes without
    /// clobbering entries other builders own. Needed because
    /// SapphireSceneBuilder (VillageHub) and CharacterFlowSceneBuilder
    /// (Login/CharacterSelect/CharacterCreate) are two separate, independently
    /// re-runnable Editor tools that each rebuild one part of the scene list -
    /// the old "EditorBuildSettings.scenes = new[] { thisOneScene }" pattern
    /// would make re-running either one silently erase the other's scenes
    /// from Build Settings.
    /// </summary>
    internal static class BuildSettingsSceneRegistrar
    {
        internal static void Register(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            UpsertInPlace(scenes, scenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Registers scenePath like Register, then moves it to index 0 - for
        /// Login, which must be the first scene a player build launches into
        /// (docs/planning/01_PRODUCT.md scope's "첫 씬이 로그인" requirement).
        /// </summary>
        internal static void RegisterFirst(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            EditorBuildSettingsScene entry = UpsertInPlace(scenes, scenePath);
            scenes.Remove(entry);
            scenes.Insert(0, entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Forces EditorBuildSettings.scenes into exactly orderedPaths' order
        /// (2026-09-15, task requirement: "Build Settings 순서 Login,
        /// CharacterSelect, CharacterCreate, VillageHub 확인"). Needed because
        /// Register()/RegisterFirst() only insert-or-update in place - they
        /// never reorder an entry that already exists elsewhere in the list,
        /// so running the two independent scene builders (SapphireSceneBuilder
        /// for VillageHub, CharacterFlowSceneBuilder for the other three) in
        /// either order against a list that may already contain some of these
        /// scenes from a previous run cannot reliably produce this exact
        /// order on its own. Any entry not named in orderedPaths (there should
        /// be none in this project, but this is defensive) is kept, appended
        /// after the ordered ones, rather than silently dropped.
        /// </summary>
        internal static void ReorderScenes(string[] orderedPaths)
        {
            List<EditorBuildSettingsScene> current = EditorBuildSettings.scenes.ToList();
            var result = new List<EditorBuildSettingsScene>();

            foreach (string path in orderedPaths)
            {
                EditorBuildSettingsScene entry = current.FirstOrDefault(s => s.path == path);
                if (entry != null)
                {
                    result.Add(entry);
                    current.Remove(entry);
                }
            }

            result.AddRange(current);
            EditorBuildSettings.scenes = result.ToArray();
        }

        private static EditorBuildSettingsScene UpsertInPlace(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            int existingIndex = scenes.FindIndex(s => s.path == scenePath);
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (existingIndex >= 0)
            {
                scenes[existingIndex] = entry;
            }
            else
            {
                scenes.Add(entry);
            }

            return entry;
        }
    }
}
