using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Presentation.Combat;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    /// <summary>Checks the persisted playable scene and captures its real renderers.</summary>
    public static class SlimeKingdomSceneVerification
    {
        const string ScenePath = "Assets/Sapphire/Scenes/SlimeKingdom.unity";
        static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification"));

        public static void VerifyAndCapture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var builder = UnityEngine.Object.FindAnyObjectByType<TilemapGridMapBuilder>();
            if (builder == null) throw new InvalidOperationException("Saved scene has no grid builder.");
            builder.Build();
            var spawn = new Sapphire.Domain.Grid.GridCoord(SlimeKingdomTerrainBuilder.SpawnX, SlimeKingdomTerrainBuilder.SpawnY);
            // The terrain builder validates reachability; this pass checks persistence and rendering references.
            var ground = GameObject.Find("Ground").GetComponent<Tilemap>();
            if (!ground.HasTile(builder.ToCell(spawn))) throw new InvalidOperationException("Spawn lost its ground after reload.");
            var monsters = UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);
            if (monsters.Length < 4) throw new InvalidOperationException("Missing saved monster encounters.");
            var zones = UnityEngine.Object.FindObjectsByType<InteractableZone>(FindObjectsSortMode.None);
            if (!zones.Any(z => z.InteractableIdValue == "return_gate" && z.DestinationScene == "VillageHub"))
                throw new InvalidOperationException("Village return link is missing.");
            foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            foreach (TileBase asset in tilemap.GetTilesBlock(tilemap.cellBounds).Distinct())
            {
                var tile = asset as Tile;
                if (tile == null || tile.sprite == null) continue;
                string path = AssetDatabase.GetAssetPath(tile.sprite.texture);
                if (!path.Contains("/Modular64/")) throw new InvalidOperationException("Legacy terrain remains: " + path);
                if (tile.sprite.pixelsPerUnit != 64) throw new InvalidOperationException("Non-64 PPU terrain: " + tile.name);
            }
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "slime-kingdom-saved-scene.txt"),
                "PASS: saved scene reloaded; Modular64 terrain references at 64 PPU; grid and spawn retained.\n" +
                "PASS: VillageHub return link retained.\nMonsters: " + monsters.Length + "\nInteractables: " + zones.Length + "\n");

            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.enabled = false;
            // Only one class rig is displayed in the editor capture; runtime selection is handled by SceneComposer.
            var rigs = UnityEngine.Object.FindObjectsByType<PlayerGridController>(FindObjectsSortMode.None);
            foreach (var rig in rigs.Skip(1)) rig.gameObject.SetActive(false);
            var preview = new GameObject("Temporary verification camera", typeof(Camera)).GetComponent<Camera>();
            preview.orthographic = true;
            preview.clearFlags = CameraClearFlags.SolidColor;
            preview.backgroundColor = new Color(.09f, .16f, .13f);
            preview.transparencySortMode = TransparencySortMode.CustomAxis;
            preview.transparencySortAxis = Vector3.up;
            try
            {
                preview.transform.position = new Vector3(SlimeKingdomTerrainBuilder.Width / 2f, SlimeKingdomTerrainBuilder.Height / 2f, -10);
                preview.orthographicSize = SlimeKingdomTerrainBuilder.Height / 2f;
                Capture(preview, SlimeKingdomTerrainBuilder.Width * 48, SlimeKingdomTerrainBuilder.Height * 48, "slime-kingdom-overview.png");
                preview.transform.position = new Vector3(SlimeKingdomTerrainBuilder.SpawnX + .5f, 4.5f, -10);
                preview.orthographicSize = 4.5f;
                Capture(preview, 1280, 720, "slime-kingdom-entrance.png");
            }
            finally { UnityEngine.Object.DestroyImmediate(preview.gameObject); }
            // Discard capture-only changes and leave the serialized scene as the source of truth.
            EditorSceneManager.OpenScene(ScenePath);
        }

        static void Capture(Camera camera, int width, int height, string filename)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.aspect = (float)width / height;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
