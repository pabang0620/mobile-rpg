using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MapBaker
{
    // Undo baking: re-enable TilemapRenderers and remove BakedMap objects
    public static void BakeAll()
    {
        RestoreScene("Assets/Sapphire/Scenes/VillageHub.unity");
        RestoreScene("Assets/Sapphire/Scenes/SlimeKingdom.unity");
    }

    private static void RestoreScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);
        Grid grid = GameObject.FindObjectOfType<Grid>();
        if (grid == null) return;

        // Re-enable all TilemapRenderers
        Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
        foreach (var tm in tilemaps)
        {
            var r = tm.GetComponent<TilemapRenderer>();
            if (r != null) r.enabled = true;
        }

        // Remove BakedMap object if it exists
        GameObject bakedGo = GameObject.Find("BakedMap");
        if (bakedGo != null) GameObject.DestroyImmediate(bakedGo);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Restored tilemaps for " + scenePath);
    }
}
