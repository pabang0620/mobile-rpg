using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    // Imports AI-created continuous materials as independent, edge-compatible tiles.
    public static class SlimeSeamlessTileBaker
    {
        private const int Size = 512;
        private const string Root = "Assets/Sapphire/Art/World/SlimeKingdom/SeamlessV4";
        private static readonly string[] Materials = { "Grass", "Dirt", "Water", "Stone" };

        public static void BakeAndBuild()
        {
            Directory.CreateDirectory(Root);
            var materials = new Color[4][][];
            for (int m = 0; m < Materials.Length; m++)
            {
                var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!source.LoadImage(File.ReadAllBytes(Root + "/Sources/" + Materials[m] + ".png")))
                    throw new Exception("Cannot load material: " + Materials[m]);
                source.wrapMode = TextureWrapMode.Repeat;
                materials[m] = new Color[4][];
                for (int variant = 0; variant < 4; variant++)
                {
                    var pixels = new Color[Size * Size];
                    for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float u = ((variant % 2) * Size + x + .5f) / (Size * 2f);
                        float v = ((variant / 2) * Size + y + .5f) / (Size * 2f);
                        pixels[y * Size + x] = source.GetPixelBilinear(u, v);
                    }
                    Write(Materials[m] + variant, pixels);
                    materials[m][variant] = pixels;
                }
                UnityEngine.Object.DestroyImmediate(source);
            }
            // Shores are composited from exactly the same grass, sand and water
            // samples. Adjacent edge positions are shared, not AI atlas dividers.
            for (int phase = 0; phase < 4; phase++)
            for (int shape = 0; shape < 8; shape++)
            {
                var pixels = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)(Size - 1), v = y / (float)(Size - 1);
                    float d;
                    switch (shape)
                    {
                        case 0: d = v - .5f; break;
                        case 1: d = .5f - v; break;
                        case 2: d = .5f - u; break;
                        case 3: d = u - .5f; break;
                        case 4: d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(0, 1)); break;
                        case 5: d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(1, 1)); break;
                        case 6: d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(0, 0)); break;
                        default: d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(1, 0)); break;
                    }
                    // Perturb the interior only; all joining edge midpoints stay fixed.
                    d += .012f * Mathf.Sin(u * Mathf.PI * 8) * Mathf.Sin(v * Mathf.PI * 8);
                    int p = y * Size + x;
                    Color shore = Color.Lerp(materials[0][phase][p], materials[1][phase][p], Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.065f, -.025f, d)));
                    pixels[p] = Color.Lerp(shore, materials[2][phase][p], Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.005f, .03f, d)));
                }
                Write("Shore" + (phase * 8 + shape), pixels);
            }
            WritePreview(materials);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SapphireSceneBuilder.BuildEverything();
            Debug.Log("SLIME_CONTINUOUS_SUCCESS: 16 continuous ground crops + 32 matching shore crops; exact 1x1 geometry, no overlap.");
        }

        private static void WritePreview(Color[][][] materials)
        {
            const int cell = 96, panel = 384;
            var preview = new Texture2D(panel * 2, panel * 2, TextureFormat.RGB24, false);
            for (int m = 0; m < 4; m++)
            for (int y = 0; y < panel; y++)
            for (int x = 0; x < panel; x++)
            {
                int phase = ((x / cell) & 1) + 2 * ((y / cell) & 1);
                int sx = (x % cell) * Size / cell, sy = (y % cell) * Size / cell;
                preview.SetPixel((m % 2) * panel + x, (m / 2) * panel + y, materials[m][phase][sy * Size + sx]);
            }
            preview.Apply();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification"));
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "slime-continuous-tiles.png"), preview.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(preview);
        }

        private static void Write(string name, Color[] pixels)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(Root + "/" + name + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
