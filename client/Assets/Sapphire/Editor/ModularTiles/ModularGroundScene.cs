using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Sapphire.EditorTools.ModularTiles
{
    public static partial class ModularGroundBuilder
    {
        static int VariantAt(int x, int y)
        {
            unchecked
            {
                uint hash = (uint)x * 0x9E3779B9u ^ (uint)y * 0x85EBCA6Bu ^ 0xC2B2AE35u;
                hash ^= hash >> 16; hash *= 0x7FEB352Du;
                hash ^= hash >> 15; hash *= 0x846CA68Bu; hash ^= hash >> 16;
                return (int)(hash % 12);
            }
        }

        static void CheckSceneCreationAllowed()
        {
            if (Application.isBatchMode) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string scenePath = SceneManager.GetSceneAt(i).path;
                if (string.IsNullOrEmpty(scenePath))
                    throw new System.InvalidOperationException("Save or close untitled scenes before building ModularGroundTest. Existing Editor scenes have not been changed.");
                if (scenePath == "Assets/Sapphire/Scenes/ModularGroundTest.unity")
                    throw new System.InvalidOperationException("Close the loaded ModularGroundTest scene before rebuilding it, saving any edits you want to keep first. No assets or existing Editor scenes have been changed.");
            }
        }

        static int MaskAt(bool[,] map, int x, int y)
        {
            int mask = 0;
            for (int d = 0; d < 8; d++)
            { int xx = x + Dx[d], yy = y + Dy[d]; if (xx >= 0 && yy >= 0 && xx < MapSize && yy < MapSize && map[xx, yy]) mask |= 1 << d; }
            return NormalizeMask(mask);
        }

        static void BuildScene(List<Entry> entries)
        {
            var ground = new bool[MapSize, MapSize]; var path = new bool[MapSize, MapSize];
            // Lower field: full-tile variant adjacency stress, path T/cross and broad junctions.
            for (int x = 0; x < MapSize; x++) for (int y = 0; y < 11; y++) ground[x, y] = true;
            for (int x = 1; x < 8; x++) for (int y = 13; y < 19; y++) ground[x, y] = true;
            for (int x = 10; x < 17; x++) for (int y = 13; y < 19; y++) ground[x, y] = x < 12 || y < 15;
            for (int x = 20; x < 28; x++) for (int y = 13; y < 20; y++) ground[x, y] = x < 22 || x > 25 || y < 15;
            for (int x = 1; x < 9; x++) for (int y = 22; y < 29; y++) ground[x, y] = x == 4 || y == 25;
            for (int x = 11; x < 21; x++) for (int y = 22; y < 29; y++) ground[x, y] = (x - 15) * (x - 15) + (y - 25) * (y - 25) < 16 + (x * 3 + y) % 7;
            ground[24, 24] = ground[26, 26] = ground[27, 27] = true;
            for (int x = 2; x < 28; x++) path[x, 5] = true;
            for (int y = 1; y < 10; y++) path[8, y] = path[21, y] = true;
            for (int x = 13; x < 18; x++) for (int y = 2; y < 9; y++) path[x, y] = true;
            CheckSceneCreationAllowed();
            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var grid = new GameObject("Modular64 Diagnostic 30x30 - visual occupancy only", typeof(Grid));
                SceneManager.MoveGameObjectToScene(grid, scene);
                var groundMap = NewTilemap("Ground", grid.transform, 0); var pathMap = NewTilemap("Path", grid.transform, 1);
                var preview = new Color32[MapSize * Size * MapSize * Size];
                for (int layer = 0; layer < 2; layer++)
                {
                    var map = layer == 0 ? ground : path; var tilemap = layer == 0 ? groundMap : pathMap;
                    var lookup = entries.Where(e => e.Path == (layer == 1) && e.Variant == 0 && !e.Alias).ToDictionary(e => e.Mask);
                    var variants = entries.Where(e => e.Path == (layer == 1) && e.Variant > 0).ToArray();
                    for (int y = 0; y < MapSize; y++) for (int x = 0; x < MapSize; x++)
                    {
                        if (!map[x, y]) continue;
                        int mask = MaskAt(map, x, y); var e = lookup[mask];
                        if (mask == 255) e = variants[VariantAt(x, y)];
                        tilemap.SetTile(new Vector3Int(x, y, 0), e.Tile);
                        for (int py = 0; py < Size; py++) for (int px = 0; px < Size; px++)
                        {
                            var color = e.Pixels[py * Size + px];
                            if (color.a != 0) preview[(y * Size + py) * MapSize * Size + x * Size + px] = color;
                        }
                    }
                }
                var cameraObject = new GameObject("Diagnostic Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.transform.position = new Vector3(15, 15, -10);
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 16;
                camera.backgroundColor = new Color(.12f, .15f, .19f); camera.clearFlags = CameraClearFlags.SolidColor;
                Directory.CreateDirectory("Assets/Sapphire/Scenes");
                if (!EditorSceneManager.SaveScene(scene, "Assets/Sapphire/Scenes/ModularGroundTest.unity"))
                    throw new System.IO.IOException("Could not save ModularGroundTest.unity.");
                WritePng(Path.Combine(Verification, "modular-ground-test.png"), preview, MapSize * Size, MapSize * Size);
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    if (previousActiveScene.IsValid() && previousActiveScene.isLoaded) SceneManager.SetActiveScene(previousActiveScene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static Tilemap NewTilemap(string name, Transform parent, int order)
        {
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<TilemapRenderer>().sortingOrder = order; return obj.GetComponent<Tilemap>();
        }
    }
}
