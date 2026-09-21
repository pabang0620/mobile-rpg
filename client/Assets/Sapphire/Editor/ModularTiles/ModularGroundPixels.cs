using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sapphire.EditorTools.ModularTiles
{
    public static partial class ModularGroundBuilder
    {
        public const string Root = "Assets/Sapphire/Art/World/Modular64";
        public const string Atlas = Root + "/TS01_Ground_Path_64.png";
        static string Verification => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../verification"));
        const int Size = 64, AtlasSize = 1024, MapSize = 30;
        static readonly int[] Dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] Dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
        static readonly Color32[] GrassPalette = {
            new Color32(43,77,25,255), new Color32(57,96,30,255), new Color32(71,116,36,255),
            new Color32(86,136,43,255), new Color32(101,151,50,255), new Color32(114,162,60,255),
            new Color32(129,176,70,255), new Color32(144,188,82,255), new Color32(163,201,100,255),
            new Color32(122,137,62,255), new Color32(160,162,87,255), new Color32(191,189,118,255) };
        static readonly Color32[] DirtPalette = {
            new Color32(105,73,34,255), new Color32(128,92,43,255), new Color32(151,111,53,255),
            new Color32(172,133,65,255), new Color32(190,151,78,255), new Color32(205,166,89,255),
            new Color32(218,178,103,255), new Color32(230,191,117,255), new Color32(241,205,134,255),
            new Color32(151,142,98,255), new Color32(183,169,116,255), new Color32(217,201,150,255) };
        sealed class Entry
        {
            public string Id, Semantic;
            public int Mask, Slot, Variant, Width;
            public bool Path, Alias;
            public Color32[] Pixels;
            public Tile Tile;
        }

        public static int NormalizeMask(int mask)
        {
            mask &= 255;
            for (int d = 1; d < 8; d += 2)
                if ((mask & (1 << (d - 1))) == 0 || (mask & (1 << ((d + 1) % 8))) == 0) mask &= ~(1 << d);
            return mask;
        }

        /// <summary>Reads terrain RGB material; tile geometry owns output alpha coverage.</summary>
        static Texture2D ReadSource(string name)
        {
            string file = Root + "/Sources/" + name + ".png";
            if (!File.Exists(file)) throw new FileNotFoundException("Provide the AI terrain material master with nonzero alpha at every pixel.", file);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(file))) throw new InvalidDataException("Cannot decode terrain material: " + file);
                var pixels = texture.GetPixels32();
                if (pixels.Any(c => c.a == 0)) throw new InvalidDataException(file + " contains fully transparent pixels whose material RGB cannot be trusted.");
                if (pixels.Any(c => c.a != 255))
                    Debug.LogWarning(file + ": source alpha is intentionally ignored; terrain RGB supplies material color and tile geometry owns binary output coverage. Source PNG is unchanged.");
                return texture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
        }

        static Rect SlotRect(int slot) => new Rect((slot % 16) * Size, (15 - slot / 16) * Size, Size, Size);

        static Entry MakeEntry(string id, int mask, int slot, int variant, bool path, Texture2D source, int width = 0)
        {
            var e = new Entry { Id = id, Semantic = variant > 0 ? "InteriorVariant" : "Blob", Mask = mask, Slot = slot, Variant = variant, Path = path, Width = width, Pixels = new Color32[Size * Size] };
            var colors = source.GetPixels32();
            int period = Math.Min(192, (Math.Min(source.width, source.height) - 1) / 2);
            if (period < 1) throw new InvalidDataException("Terrain source must be at least 3x3 pixels.");
            int ox = (source.width - 2 * period - 1) / 2, oy = (source.height - 2 * period - 1) / 2;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                if (!Visible(mask, x, y, path, width)) continue;
                // Tensor-product periodic conditioning, without mirrored motifs. At either
                // endpoint the sample is the same source coordinate ox/oy + period.
                int sx = x * period / 63, sy = y * period / 63;
                float wx = Mathf.SmoothStep(0, 1, x / 63f), wy = Mathf.SmoothStep(0, 1, y / 63f);
                Color lo = Color.Lerp(colors[(oy + sy) * source.width + ox + sx + period], colors[(oy + sy) * source.width + ox + sx], wx);
                Color hi = Color.Lerp(colors[(oy + sy + period) * source.width + ox + sx + period], colors[(oy + sy + period) * source.width + ox + sx], wx);
                Color c = Color.Lerp(hi, lo, wy);
                if (variant > 0)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float radius = 22 + 3 * Mathf.Sin((x + y + variant * 7) * .17f) + 2 * Mathf.Cos((x - y) * .21f);
                    float amount = Mathf.Clamp01((radius - Mathf.Sqrt(dx * dx + dy * dy)) / 7);
                    int vx = (variant * 73) % (source.width - period), vy = (variant * 97) % (source.height - period);
                    c = Color.Lerp(c, colors[(vy + sy) * source.width + vx + sx], amount);
                }
                e.Pixels[y * Size + x] = Quantize(c, path ? DirtPalette : GrassPalette);
            }
            return e;
        }

        static Color32 Quantize(Color32 color, Color32[] palette)
        {
            int best = int.MaxValue; Color32 result = palette[0];
            foreach (var candidate in palette)
            {
                int r = color.r - candidate.r, g = color.g - candidate.g, b = color.b - candidate.b;
                int distance = r * r + g * g + b * b;
                if (distance < best) { best = distance; result = candidate; }
            }
            return result;
        }

        static bool Visible(int mask, int x, int y, bool path, int width)
        {
            int[] dist = { 63 - y, 63 - x, y, x };
            for (int c = 0; c < 4; c++) if ((mask & (1 << (c * 2))) == 0)
            {
                int along = c % 2 == 0 ? x : y;
                int baseInset = path ? 16 + width : 8;
                int jitter = along <= baseInset + 3 || along >= 63 - baseInset - 3 ? 0 : ((along / 5 + along / 13) % 3) - 1;
                int inset = baseInset + jitter;
                if (dist[c] < inset) return false;
            }
            // A shared edge's endpoint uses the same three neighboring cells on both sides.
            for (int corner = 0; corner < 4; corner++)
            {
                int a = corner * 2, b = (a + 2) % 8, diagonal = a + 1;
                bool hasA = (mask & (1 << a)) != 0, hasB = (mask & (1 << b)) != 0;
                int u = dist[corner], v = dist[(corner + 1) % 4];
                int baseInset = path ? 16 + width : 8;
                if (hasA && hasB && (mask & (1 << diagonal)) == 0 && u * u + v * v < baseInset * baseInset) return false;
                if (!hasA && !hasB)
                {
                    int radius = path ? 6 : 8, center = baseInset + radius;
                    if (u < center && v < center && (u - center) * (u - center) + (v - center) * (v - center) > radius * radius) return false;
                }
            }
            return true;
        }

        static void WritePng(string file, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(file, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
