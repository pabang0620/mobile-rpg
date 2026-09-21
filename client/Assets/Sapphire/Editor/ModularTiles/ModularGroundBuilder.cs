using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sapphire.EditorTools.ModularTiles
{
    public static partial class ModularGroundBuilder
    {
        static readonly int[] AliasMasks = { 68, 17, 65, 5, 80, 20, 69, 21, 84, 81, 85, 1, 4, 16, 64, 68, 17, 68, 17, 65, 5, 80, 20, 0 };
        static readonly string[] AliasNames = { "StraightH", "StraightV", "CornerTL", "CornerTR", "CornerBL", "CornerBR", "TUp", "TRight", "TDown", "TLeft", "Cross", "EndUp", "EndRight", "EndDown", "EndLeft", "NarrowH", "NarrowV", "WideH", "WideV", "WideCornerTL", "WideCornerTR", "WideCornerBL", "WideCornerBR", "BrokenEdge" };

        [MenuItem("Sapphire/Modular Tiles/Build Independent Ground Test")]
        public static void Build()
        {
            CheckSceneCreationAllowed();
            Texture2D grass = null, dirt = null;
            var masks = Enumerable.Range(0, 256).Select(NormalizeMask).Distinct().OrderBy(x => x).ToArray();
            if (masks.Length != 47) throw new InvalidOperationException("Blob normalization must produce 47 masks.");
            var entries = new List<Entry>();
            try
            {
                grass = ReadSource("GrassMaster"); dirt = ReadSource("DirtMaster");
                for (int i = 0; i < 47; i++) entries.Add(MakeEntry("G" + (i + 1).ToString("D3"), masks[i], i, 0, false, grass));
                for (int i = 0; i < 12; i++) entries.Add(MakeEntry("G" + (i + 48).ToString("D3"), 255, 48 + i, i + 1, false, grass));
                for (int i = 0; i < 47; i++) entries.Add(MakeEntry("PA" + (i + 1).ToString("D3"), masks[i], 96 + i, 0, true, dirt));
                for (int i = 0; i < 12; i++) entries.Add(MakeEntry("PV" + (i + 1).ToString("D3"), 255, 144 + i, i + 1, true, dirt));
                for (int i = 0; i < 24; i++)
                {
                    int width = i == 15 || i == 16 ? 5 : i >= 17 && i <= 22 ? -6 : 0;
                    var e = MakeEntry("P" + (i + 1).ToString("D3"), AliasMasks[i], 64 + i, 0, true, dirt, width);
                    e.Alias = true; e.Semantic = AliasNames[i]; entries.Add(e);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(grass); UnityEngine.Object.DestroyImmediate(dirt); }
            Directory.CreateDirectory(Root + "/Tiles"); Directory.CreateDirectory(Verification);
            Validate(entries, masks);
            var atlasPixels = new Color32[AtlasSize * AtlasSize];
            foreach (var e in entries)
            {
                var rect = SlotRect(e.Slot);
                for (int y = 0; y < Size; y++) Array.Copy(e.Pixels, y * Size, atlasPixels, ((int)rect.y + y) * AtlasSize + (int)rect.x, Size);
            }
            var occupiedSlots = new HashSet<int>(entries.Select(e => e.Slot));
            for (int slot = 0; slot < 256; slot++) if (!occupiedSlots.Contains(slot))
            {
                var rect = SlotRect(slot);
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                    if (!atlasPixels[((int)rect.y + y) * AtlasSize + (int)rect.x + x].Equals(new Color32()))
                        throw new InvalidOperationException("Reserved slot is not transparent: " + slot);
            }
            WritePng(Atlas, atlasPixels, AtlasSize, AtlasSize);
            File.AppendAllText(Path.Combine(Verification, "modular-ground-validation.txt"), "PASS: 1024x1024 atlas; 142 unique occupied slots; all 114 reserved slots zero RGBA.\n");
            AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false; importer.isReadable = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = AtlasSize; importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            foreach (var e in entries) SaveTile(e, atlas);
            AssetDatabase.SaveAssets();
            foreach (var e in entries)
            {
                string tilePath = Root + "/Tiles/" + e.Id + ".asset";
                AssetDatabase.ImportAsset(tilePath, ImportAssetOptions.ForceSynchronousImport);
                e.Tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                var sprite = e.Tile == null ? null : e.Tile.sprite;
                if (sprite == null || sprite.texture != atlas || sprite.rect != SlotRect(e.Slot) || sprite.pixelsPerUnit != Size)
                    throw new InvalidDataException("Saved tile sprite reference or geometry is invalid: " + e.Id);
            }
            var manifest = new StringBuilder("id,semantic,layer,normalized_mask,atlas_slot,atlas_x,atlas_y_bottom,variant,width_profile,asset\n");
            foreach (var e in entries.OrderBy(e => e.Slot))
            {
                var r = SlotRect(e.Slot);
                manifest.AppendLine($"{e.Id},{e.Semantic},{(e.Path ? "Path" : "Ground")},{e.Mask},{e.Slot},{r.x},{r.y},{e.Variant},{e.Width},{Root}/Tiles/{e.Id}.asset");
            }
            File.WriteAllText(Root + "/TS01_Ground_Path_64.csv", manifest.ToString());
            BuildScene(entries); AssetDatabase.Refresh();
            Debug.Log("Modular64: 142 tiles, 47 masks/layer, P aliases, independent 30x30 diagnostic Tilemap scene and PNG. Gameplay/build settings untouched.");
        }

        static void SaveTile(Entry e, Texture2D atlas)
        {
            string assetPath = Root + "/Tiles/" + e.Id + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, assetPath); }
            // Serialized Sprite subassets avoid requiring optional com.unity.2d.sprite.
            var sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
            var generated = Sprite.Create(atlas, SlotRect(e.Slot), new Vector2(.5f, .5f), Size, 0, SpriteMeshType.FullRect);
            generated.name = e.Id;
            if (sprite == null) { sprite = generated; AssetDatabase.AddObjectToAsset(sprite, tile); }
            else { EditorUtility.CopySerialized(generated, sprite); UnityEngine.Object.DestroyImmediate(generated); }
            tile.name = e.Id; tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None;
            tile.color = Color.white; tile.transform = Matrix4x4.identity;
            EditorUtility.SetDirty(sprite); EditorUtility.SetDirty(tile); e.Tile = tile;
        }
    }
}
