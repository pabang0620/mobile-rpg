using System.Collections.Generic;
using UnityEngine;

namespace Sapphire
{
    /// <summary>
    /// Runtime, asset-free pixel sprites shared by exploration views. Every texture is 16 pixels
    /// per world unit and uses point filtering so it stays crisp at any orthographic zoom.
    /// </summary>
    public static class PixelSpriteLibrary
    {
        const int PixelsPerUnit = 16;
        static readonly Dictionary<ExplorationTile, Sprite> tiles = new Dictionary<ExplorationTile, Sprite>();
        static readonly Dictionary<int, Sprite> walkers = new Dictionary<int, Sprite>();

        public static Sprite Tile(ExplorationTile tile)
        {
            Sprite sprite;
            if (tiles.TryGetValue(tile, out sprite)) return sprite;
            var pixels = NewPixels(16, 16, new Color32(75, 142, 81, 255));
            switch (tile)
            {
                case ExplorationTile.Path: DrawPath(pixels); break;
                case ExplorationTile.Water: DrawWater(pixels); break;
                case ExplorationTile.Sand: Fill(pixels, new Color32(202, 181, 107, 255)); Speckle(pixels, new Color32(174, 145, 75, 255), 2); break;
                case ExplorationTile.Flowers: DrawGrass(pixels); Dot(pixels, 3, 5, new Color32(255, 216, 113, 255)); Dot(pixels, 11, 10, new Color32(244, 137, 171, 255)); Dot(pixels, 7, 13, new Color32(255, 216, 113, 255)); break;
                case ExplorationTile.Tree: DrawTree(pixels); break;
                case ExplorationTile.Wall: DrawWall(pixels); break;
                case ExplorationTile.Roof: DrawRoof(pixels); break;
                case ExplorationTile.Door: DrawDoor(pixels); break;
                default: DrawGrass(pixels); break;
            }
            sprite = Make("Pixel tile " + tile, pixels, 16, 16, new Vector2(.5f, .5f));
            tiles.Add(tile, sprite);
            return sprite;
        }

        public static Sprite Walker(ExplorationFacing facing, int frame)
        {
            int key = ((int)facing * 2) + (frame & 1);
            Sprite sprite;
            if (walkers.TryGetValue(key, out sprite)) return sprite;
            var pixels = NewPixels(16, 24, new Color32(0, 0, 0, 0));
            DrawWalker(pixels, facing, frame & 1);
            sprite = Make("SD walker " + facing + " " + (frame & 1), pixels, 16, 24, new Vector2(.5f, 0));
            walkers.Add(key, sprite);
            return sprite;
        }

        static Sprite Make(string name, Color32[] pixels, int width, int height, Vector2 pivot)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name + " texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        static Color32[] NewPixels(int width, int height, Color32 color)
        {
            var pixels = new Color32[width * height];
            Fill(pixels, color);
            return pixels;
        }

        static void Fill(Color32[] pixels, Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        }

        static void Pixel(Color32[] p, int x, int y, Color32 c, int w = 16, int h = 16)
        {
            if (x >= 0 && x < w && y >= 0 && y < h) p[y * w + x] = c;
        }

        static void Rect(Color32[] p, int x, int y, int width, int height, Color32 c, int canvasWidth = 16, int canvasHeight = 16)
        {
            for (int yy = y; yy < y + height; yy++) for (int xx = x; xx < x + width; xx++) Pixel(p, xx, yy, c, canvasWidth, canvasHeight);
        }

        static void Dot(Color32[] p, int x, int y, Color32 c) { Rect(p, x, y, 2, 2, c); }

        static void DrawGrass(Color32[] p)
        {
            Fill(p, new Color32(75, 142, 81, 255));
            Pixel(p, 1, 2, new Color32(96, 167, 87, 255)); Pixel(p, 5, 12, new Color32(49, 112, 67, 255));
            Pixel(p, 9, 4, new Color32(96, 167, 87, 255)); Pixel(p, 13, 9, new Color32(50, 113, 67, 255));
            Pixel(p, 3, 8, new Color32(59, 126, 72, 255)); Pixel(p, 14, 1, new Color32(59, 126, 72, 255));
        }

        static void DrawPath(Color32[] p)
        {
            Fill(p, new Color32(174, 145, 93, 255));
            Rect(p, 0, 0, 16, 1, new Color32(132, 105, 70, 255));
            Dot(p, 3, 4, new Color32(203, 177, 117, 255)); Dot(p, 11, 9, new Color32(132, 105, 70, 255));
            Pixel(p, 7, 14, new Color32(203, 177, 117, 255)); Pixel(p, 14, 3, new Color32(132, 105, 70, 255));
        }

        static void DrawWater(Color32[] p)
        {
            Fill(p, new Color32(48, 113, 166, 255));
            for (int y = 2; y < 16; y += 5)
            {
                Rect(p, (y * 3) % 10, y, 5, 1, new Color32(100, 180, 206, 255));
                Rect(p, ((y * 3) + 7) % 13, y + 1, 3, 1, new Color32(72, 145, 187, 255));
            }
        }

        static void DrawTree(Color32[] p)
        {
            DrawGrass(p);
            Rect(p, 6, 0, 4, 7, new Color32(92, 62, 42, 255)); Rect(p, 7, 0, 2, 7, new Color32(141, 91, 50, 255));
            Rect(p, 2, 6, 12, 8, new Color32(35, 87, 57, 255)); Rect(p, 1, 8, 14, 4, new Color32(35, 87, 57, 255));
            Rect(p, 4, 12, 8, 3, new Color32(50, 121, 66, 255)); Rect(p, 3, 9, 10, 5, new Color32(50, 121, 66, 255));
            Rect(p, 5, 13, 5, 2, new Color32(75, 155, 77, 255)); Pixel(p, 12, 10, new Color32(75, 155, 77, 255)); Pixel(p, 4, 8, new Color32(75, 155, 77, 255));
        }

        static void DrawWall(Color32[] p)
        {
            Fill(p, new Color32(99, 103, 114, 255));
            Rect(p, 0, 0, 16, 1, new Color32(62, 67, 77, 255)); Rect(p, 0, 8, 16, 1, new Color32(68, 72, 81, 255));
            Rect(p, 4, 1, 1, 7, new Color32(68, 72, 81, 255)); Rect(p, 11, 9, 1, 7, new Color32(68, 72, 81, 255));
            Rect(p, 0, 15, 16, 1, new Color32(139, 143, 151, 255));
        }

        static void DrawRoof(Color32[] p)
        {
            Fill(p, new Color32(129, 56, 57, 255));
            for (int y = 1; y < 16; y += 4) Rect(p, 0, y, 16, 1, new Color32(84, 39, 47, 255));
            for (int x = 2; x < 16; x += 5) Rect(p, x, 0, 1, 16, new Color32(167, 69, 60, 255));
            Rect(p, 0, 15, 16, 1, new Color32(72, 36, 42, 255));
        }

        static void DrawDoor(Color32[] p)
        {
            DrawWall(p);
            Rect(p, 4, 0, 8, 12, new Color32(79, 48, 36, 255)); Rect(p, 5, 1, 6, 10, new Color32(124, 76, 45, 255));
            Rect(p, 5, 10, 6, 1, new Color32(76, 45, 34, 255)); Pixel(p, 9, 6, new Color32(231, 186, 81, 255));
        }

        static void Speckle(Color32[] p, Color32 color, int step)
        {
            for (int y = 1; y < 16; y += 5) for (int x = ((y / 5) * 4) + 1; x < 16; x += 7) Rect(p, x, y, step, 1, color);
        }

        static void DrawWalker(Color32[] p, ExplorationFacing facing, int frame)
        {
            Color32 outline = new Color32(30, 38, 57, 255);
            Color32 skin = new Color32(242, 191, 151, 255);
            Color32 hair = new Color32(64, 49, 78, 255);
            Color32 robe = new Color32(61, 112, 185, 255);
            Color32 robeLight = new Color32(99, 164, 224, 255);
            Color32 boot = new Color32(36, 48, 70, 255);
            int leftFoot = frame == 0 ? 5 : 7;
            int rightFoot = frame == 0 ? 10 : 8;

            // Boots and robe silhouette are common to all directions.
            Rect(p, leftFoot, 0, 3, 3, boot, 16, 24); Rect(p, rightFoot, 0, 3, 3, boot, 16, 24);
            Rect(p, 4, 3, 8, 8, outline, 16, 24); Rect(p, 5, 4, 6, 7, robe, 16, 24); Rect(p, 6, 5, 2, 5, robeLight, 16, 24);
            if (facing == ExplorationFacing.Up)
            {
                Rect(p, 4, 11, 8, 8, outline, 16, 24); Rect(p, 5, 12, 6, 6, hair, 16, 24); Rect(p, 6, 17, 4, 1, new Color32(91, 69, 102, 255), 16, 24);
                Rect(p, 3, 7 + frame, 2, 5, outline, 16, 24); Rect(p, 4, 8 + frame, 1, 3, robeLight, 16, 24);
            }
            else if (facing == ExplorationFacing.Left || facing == ExplorationFacing.Right)
            {
                bool right = facing == ExplorationFacing.Right;
                int faceX = right ? 5 : 6;
                Rect(p, 4, 11, 8, 8, outline, 16, 24); Rect(p, 5, 12, 6, 6, skin, 16, 24); Rect(p, 4, 17, 8, 3, hair, 16, 24);
                Pixel(p, faceX, 15, outline, 16, 24);
                int armX = right ? 11 : 3; Rect(p, armX, 7 + frame, 2, 5, outline, 16, 24); Rect(p, right ? 11 : 4, 8 + frame, 1, 3, robeLight, 16, 24);
            }
            else
            {
                Rect(p, 4, 11, 8, 8, outline, 16, 24); Rect(p, 5, 12, 6, 6, skin, 16, 24); Rect(p, 4, 17, 8, 3, hair, 16, 24);
                Rect(p, 5, 11, 6, 1, new Color32(214, 154, 126, 255), 16, 24); Pixel(p, 6, 15, outline, 16, 24); Pixel(p, 9, 15, outline, 16, 24);
                Rect(p, frame == 0 ? 2 : 11, 7, 2, 5, outline, 16, 24); Rect(p, frame == 0 ? 3 : 11, 8, 1, 3, robeLight, 16, 24);
            }
        }
    }
}
