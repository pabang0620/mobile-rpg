using UnityEditor;
using UnityEngine;
using System.IO;

public static class TileSmoother
{
    [MenuItem("Tools/Generate Smooth Edges V5")]
    public static void GenerateEdges()
    {
        string root = "Assets/Sapphire/Art/World/SlimeKingdom/SeamlessV5/";
        Texture2D grass = LoadTex(root + "Grass0.png");
        Texture2D dirt = LoadTex(root + "Dirt0.png");
        Texture2D water = LoadTex(root + "Water0.png");
        
        GenerateSet(root, "DirtEdge", dirt, grass, null, false);
        GenerateSet(root, "Shore", water, grass, dirt, true);
        
        AssetDatabase.Refresh();
        Debug.Log("Sharp RPG-Maker style edges generated!");
    }

    private static Texture2D LoadTex(string path)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        return tex;
    }

    private static void GenerateSet(string root, string prefix, Texture2D fg, Texture2D bg, Texture2D cliffTex, bool isWater)
    {
        int s = 512;
        Color[] fgP = fg.GetPixels();
        Color[] bgP = bg.GetPixels();
        Color[] cliffP = cliffTex != null ? cliffTex.GetPixels() : null;

        for (int phase = 0; phase < 4; phase++)
        {
            for (int dir = 0; dir < 8; dir++)
            {
                int index = phase * 8 + dir;
                Texture2D res = new Texture2D(s, s, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[s * s];
                
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float nx = x / (float)s;
                        float ny = y / (float)s;
                        
                        // Pixelated noise for edges
                        float noise = Mathf.PerlinNoise(nx * 40f + phase * 10f, ny * 40f + dir * 10f) * 0.06f - 0.03f;
                        
                        // Base land mask (1 = FG/Water, 0 = BG/Grass)
                        float landMask = 0f;
                        
                        // dir: 0=S, 1=N, 2=E, 3=W, 4=SE, 5=SW, 6=NE, 7=NW
                        if (dir == 0) { landMask = (ny < 0.5f + noise) ? 1f : 0f; }
                        if (dir == 1) { landMask = (ny > 0.5f + noise) ? 1f : 0f; }
                        if (dir == 2) { landMask = (nx > 0.5f + noise) ? 1f : 0f; }
                        if (dir == 3) { landMask = (nx < 0.5f + noise) ? 1f : 0f; }
                        
                        // Corners
                        if (dir == 4) { landMask = (nx > 0.5f + noise && ny < 0.5f + noise) ? 1f : 0f; } // SE
                        if (dir == 5) { landMask = (nx < 0.5f + noise && ny < 0.5f + noise) ? 1f : 0f; } // SW
                        if (dir == 6) { landMask = (nx > 0.5f + noise && ny > 0.5f + noise) ? 1f : 0f; } // NE
                        if (dir == 7) { landMask = (nx < 0.5f + noise && ny > 0.5f + noise) ? 1f : 0f; } // NW

                        Color finalColor = Color.clear;

                        if (!isWater)
                        {
                            // Just sharp blend between Dirt (fg) and Grass (bg)
                            finalColor = landMask > 0.5f ? fgP[y * s + x] : bgP[y * s + x];
                        }
                        else
                        {
                            // Water with Depth!
                            // If it's grass (landMask == 0), it's at elevation 1.
                            // If it's water, it's at elevation 0.
                            // We shift the water region DOWN by a cliff height (e.g. 0.2 units).
                            float cliffHeight = 0.2f;
                            
                            // Re-evaluate landMask shifted up (which means checking ny + cliffHeight)
                            // to see if we are in the "cliff" zone.
                            float nyShift = ny + cliffHeight;
                            float shiftedNoise = Mathf.PerlinNoise(nx * 40f + phase * 10f, nyShift * 40f + dir * 10f) * 0.06f - 0.03f;
                            
                            float waterMask = 0f;
                            if (dir == 0) { waterMask = (nyShift < 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 1) { waterMask = (nyShift > 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 2) { waterMask = (nx > 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 3) { waterMask = (nx < 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 4) { waterMask = (nx > 0.5f + shiftedNoise && nyShift < 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 5) { waterMask = (nx < 0.5f + shiftedNoise && nyShift < 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 6) { waterMask = (nx > 0.5f + shiftedNoise && nyShift > 0.5f + shiftedNoise) ? 1f : 0f; }
                            if (dir == 7) { waterMask = (nx < 0.5f + shiftedNoise && nyShift > 0.5f + shiftedNoise) ? 1f : 0f; }

                            // If landMask == 0, we are on Grass (High).
                            if (landMask < 0.5f)
                            {
                                finalColor = bgP[y * s + x];
                                // Add a tiny dirt rim on the very edge of grass
                                // If it's very close to the cliff...
                            }
                            else
                            {
                                // We are below the grass.
                                // Are we on the cliff or in the water?
                                if (waterMask < 0.5f)
                                {
                                    // Cliff! (Dirt texture, slightly darkened to look like a wall)
                                    finalColor = Color.Lerp(cliffP[y * s + x], Color.black, 0.2f);
                                }
                                else
                                {
                                    // Water!
                                    finalColor = fgP[y * s + x];
                                    
                                    // Draw a shadow right below the cliff
                                    // Check if we are very close to the cliff boundary
                                    float shadowNoise = Mathf.PerlinNoise(nx * 20f, ny * 20f) * 0.02f;
                                    float distToCliff = 0f;
                                    if (dir == 1 || dir == 6 || dir == 7) distToCliff = (nyShift - 0.5f - shiftedNoise);
                                    if (distToCliff > 0f && distToCliff < 0.08f + shadowNoise)
                                    {
                                        finalColor = Color.Lerp(finalColor, Color.black, 0.35f);
                                    }
                                }
                            }
                        }

                        pixels[y * s + x] = finalColor;
                    }
                }
                res.SetPixels(pixels);
                res.Apply();
                File.WriteAllBytes(root + prefix + index + ".png", res.EncodeToPNG());
            }
        }
    }
}
