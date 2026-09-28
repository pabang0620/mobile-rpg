using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Sapphire.EditorTools.ModularTiles
{
    internal static class ShoreMaterialsV3
    {
        internal enum Material { Grass, Water, Rock }
        const string Source = "Assets/Sapphire/Art/World/Modular64/Sources/ShoreMaterialsV3.png";
        const string Digest = "6AF59F54F6D67BCE004563F44243552B67A8742ECAF5573420156FADBF538BFF";
        internal static Texture2D Load(Material material)
        {
            byte[] bytes = File.ReadAllBytes(Source);
            using (var hash = SHA256.Create())
                if (string.Concat(hash.ComputeHash(bytes).Select(b => b.ToString("X2"))) != Digest)
                    throw new InvalidDataException("Shore material source changed without updating provenance.");
            var master = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!master.LoadImage(bytes) || master.width != 1536 || master.height != 1024)
                    throw new InvalidDataException("Shore material master must have three 512x1024 panels.");
                var all = master.GetPixels32(); var panel = new Color32[512 * 1024];
                for (int y = 0; y < 1024; y++) Array.Copy(all, y * 1536 + (int)material * 512, panel, y * 512, 512);
                if (panel.Any(p => p.a != 255)) throw new InvalidDataException("Shore material must be opaque.");
                var result = new Texture2D(512, 1024, TextureFormat.RGBA32, false);
                result.SetPixels32(panel); result.Apply(); return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(master); }
        }

        internal static Color32[] Palette(Material material, int count)
        {
            var source = Load(material);
            try
            {
                var pixels = source.GetPixels32().OrderBy(p => p.r * 3 + p.g * 5 + p.b * 2).ToArray();
                return Enumerable.Range(1, count).Select(i => pixels[i * pixels.Length / (count + 1)]).ToArray();
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }
    }
}
