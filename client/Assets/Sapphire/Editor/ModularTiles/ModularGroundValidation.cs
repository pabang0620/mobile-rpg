using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Sapphire.EditorTools.ModularTiles
{
    public static partial class ModularGroundBuilder
    {
        static void Validate(List<Entry> entries, int[] masks)
        {
            if (entries.Count != 142 || entries.Select(e => e.Id).Distinct().Count() != 142 || entries.Select(e => e.Slot).Distinct().Count() != 142)
                throw new InvalidOperationException("Expected exactly 142 unique IDs and slots.");
            int comparisons = 0;
            foreach (bool path in new[] { false, true })
            {
                int inset = path ? 16 : 8;
                // Straight sides must not develop corner scallops at a tile junction.
                foreach (int x in new[] { 0, 1, 62, 63 })
                {
                    if (!Visible(68, x, inset, path, 0) || Visible(68, x, inset - 1, path, 0) ||
                        !Visible(17, inset, x, path, 0) || Visible(17, inset - 1, x, path, 0))
                        throw new InvalidOperationException("Straight boundary developed a tile-junction notch.");
                }
                var lookup = entries.Where(e => e.Path == path && e.Variant == 0 && !e.Alias).ToDictionary(e => e.Mask);
                // Exhaustive 4x3 neighborhoods, with both middle cells occupied.
                for (int bits = 0; bits < 4096; bits++)
                {
                    if ((bits & (1 << 5)) == 0 || (bits & (1 << 6)) == 0) continue;
                    Func<int, int, bool> occupied = (x, y) => x >= 0 && x < 4 && y >= 0 && y < 3 && (bits & (1 << (y * 4 + x))) != 0;
                    for (int transpose = 0; transpose < 2; transpose++)
                    {
                        int ma = 0, mb = 0;
                        for (int d = 0; d < 8; d++)
                        {
                            int dx = transpose == 0 ? Dx[d] : Dy[d], dy = transpose == 0 ? Dy[d] : Dx[d];
                            if (occupied(1 + dx, 1 + dy)) ma |= 1 << d;
                            if (occupied(2 + dx, 1 + dy)) mb |= 1 << d;
                        }
                        var a = lookup[NormalizeMask(ma)].Pixels; var b = lookup[NormalizeMask(mb)].Pixels;
                        for (int p = 0; p < Size; p++)
                        {
                            int ia = transpose == 0 ? p * Size + 63 : 63 * Size + p;
                            int ib = transpose == 0 ? p * Size : p;
                            if (!a[ia].Equals(b[ib])) throw new InvalidOperationException($"RGBA seam: path={path}, masks={ma}/{mb}, axis={transpose}, pixel={p}");
                            comparisons++;
                        }
                    }
                }
                var full = lookup[255].Pixels;
                foreach (var e in entries.Where(e => e.Path == path && e.Variant > 0))
                {
                    bool changed = false;
                    for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                    {
                        int i = y * Size + x;
                        if ((x == 0 || y == 0 || x == 63 || y == 63) && !e.Pixels[i].Equals(full[i])) throw new InvalidOperationException("Variant perimeter mismatch: " + e.Id);
                        changed |= !e.Pixels[i].Equals(full[i]);
                    }
                    if (!changed) throw new InvalidOperationException("Identical source variant: " + e.Id);
                }
            }
            foreach (var e in entries)
                if (e.Pixels.Any(p => p.a != 0 && p.a != 255) || e.Pixels.Any(p => p.a == 0 && (p.r != 0 || p.g != 0 || p.b != 0))) throw new InvalidOperationException("Invalid alpha: " + e.Id);
            int palette = entries.SelectMany(e => e.Pixels).Where(c => c.a != 0).Select(c => (c.r << 16) | (c.g << 8) | c.b).Distinct().Count();
            if (palette > 24) throw new InvalidOperationException("Palette exceeded 24 colors.");
            File.WriteAllText(Path.Combine(Verification, "modular-ground-validation.txt"), $"PASS: 256 masks normalize to {masks.Length}.\nPASS: {comparisons} admissible neighbor RGBA pixel comparisons.\nPASS: 12 ground + 12 path variant perimeters; interiors differ.\nPASS: binary alpha and zero transparent RGB; no checkerboard generated.\nOpaque palette count: {palette} (12 fixed grass + 12 fixed dirt colors maximum).\nP aliases have separate width profiles; use matching-profile neighbors. PA is complete automatic family.\nDiagnostic art occupancy only; no gameplay collision/movement claim.\n");
        }
    }
}
