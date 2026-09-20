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
                        
                        float noise = Mathf.PerlinNoise(nx * 10f + phase * 10f, ny * 10f + dir * 10f) * 0.04f - 0.02f;
                        
                        // Distance field for curves
                        float landMask = 0f;
                        
                        // Straight edges (wavy)
                        if (dir == 0) { landMask = (ny < 0.5f + noise) ? 1f : 0f; } // South
                        if (dir == 1) { landMask = (ny > 0.5f + noise) ? 1f : 0f; } // North
                        if (dir == 2) { landMask = (nx > 0.5f + noise) ? 1f : 0f; } // East
                        if (dir == 3) { landMask = (nx < 0.5f + noise) ? 1f : 0f; } // West
                        
                        // Inner corners (rounded) - circle equation
                        float r = 0.5f;
                        if (dir == 4) { // SE (Center of circle at (1, 0))
                            float dx = nx - 1f; float dy = ny - 0f;
                            landMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                        }
                        if (dir == 5) { // SW (Center at (0, 0))
                            float dx = nx - 0f; float dy = ny - 0f;
                            landMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                        }
                        if (dir == 6) { // NE (Center at (1, 1))
                            float dx = nx - 1f; float dy = ny - 1f;
                            landMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                        }
                        if (dir == 7) { // NW (Center at (0, 1))
                            float dx = nx - 0f; float dy = ny - 1f;
                            landMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                        }

                        Color finalColor = Color.clear;

                        if (!isWater)
                        {
                            // Path
                            finalColor = landMask > 0.5f ? fgP[y * s + x] : bgP[y * s + x];
                        }
                        else
                        {
                            // Water with Depth
                            float cliffHeight = 0.2f; // 20% tile height for cliff
                            float nyShift = ny + cliffHeight;
                            
                            float waterMask = 0f;
                            if (dir == 0) { waterMask = (nyShift < 0.5f + noise) ? 1f : 0f; }
                            if (dir == 1) { waterMask = (nyShift > 0.5f + noise) ? 1f : 0f; }
                            if (dir == 2) { waterMask = (nx > 0.5f + noise) ? 1f : 0f; }
                            if (dir == 3) { waterMask = (nx < 0.5f + noise) ? 1f : 0f; }
                            
                            if (dir == 4) { 
                                float dx = nx - 1f; float dy = nyShift - 0f;
                                waterMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                            }
                            if (dir == 5) { 
                                float dx = nx - 0f; float dy = nyShift - 0f;
                                waterMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                            }
                            if (dir == 6) { 
                                float dx = nx - 1f; float dy = nyShift - 1f;
                                waterMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                            }
                            if (dir == 7) { 
                                float dx = nx - 0f; float dy = nyShift - 1f;
                                waterMask = (Mathf.Sqrt(dx*dx + dy*dy) > r + noise) ? 1f : 0f;
                            }

                            if (landMask < 0.5f)
                            {
                                finalColor = bgP[y * s + x]; // Grass
                                // Draw a slight border line on grass edge
                                float distToEdge = 999f;
                                if (dir == 0) distToEdge = (ny - 0.5f - noise);
                                if (dir == 1) distToEdge = (0.5f + noise - ny);
                                if (distToEdge > 0f && distToEdge < 0.02f) finalColor = Color.Lerp(finalColor, new Color(0.2f,0.4f,0.1f), 0.5f);
                            }
                            else
                            {
                                if (waterMask < 0.5f)
                                {
                                    // Cliff wall
                                    finalColor = Color.Lerp(cliffP[y * s + x], new Color(0.4f, 0.3f, 0.2f), 0.5f);
                                }
                                else
                                {
                                    // Water surface
                                    finalColor = fgP[y * s + x];
                                    
                                    // Drop shadow below cliff
                                    float distToCliff = 999f;
                                    if (dir == 1) distToCliff = (nyShift - 0.5f - noise);
                                    if (dir == 6) {
                                        float dx = nx - 1f; float dy = nyShift - 1f;
                                        distToCliff = (Mathf.Sqrt(dx*dx + dy*dy) - r - noise);
                                    }
                                    if (dir == 7) {
                                        float dx = nx - 0f; float dy = nyShift - 1f;
                                        distToCliff = (Mathf.Sqrt(dx*dx + dy*dy) - r - noise);
                                    }

                                    if (distToCliff > 0f && distToCliff < 0.1f)
                                    {
                                        finalColor = Color.Lerp(finalColor, new Color(0,0,0,0.5f), 1f - (distToCliff / 0.1f));
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
