using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Small hand-authored pixel-art icons, nearest-neighbor upscaled so they
    // stay crisp at UI size — a more literal alternative to the analytic
    // line-glyphs in PlaceholderSprites for abilities worth a real drawing.
    public static class PixelArtIcons
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // Defined as the LEFT half only (mirrored to build the full row) so
        // the art stays symmetric and there's half as much to hand-type.
        // Row 0 is the top of the image.
        static readonly string[] GorillaShoutLeftHalf =
        {
            "........",
            "...FFFF.",
            "..FFFFFF",
            ".FFFFFFF",
            ".FFFFFFF",
            ".FBBFFFF",
            ".FBBBFFF",
            ".FSSSFFF",
            ".FSESFFF",
            ".FSSSFFF",
            ".FSSSFFF",
            ".FSMMMFF",
            ".FSMTMFF",
            ".FSMMMFF",
            "..FSMFFF",
            "...FFFF.",
        };

        static readonly Dictionary<char, Color32> GorillaColors = new Dictionary<char, Color32>
        {
            { 'F', new Color32(64, 45, 35, 255) },    // dark fur
            { 'B', new Color32(28, 18, 14, 255) },    // heavy brow
            { 'S', new Color32(150, 115, 95, 255) },  // face skin
            { 'E', new Color32(15, 15, 15, 255) },    // eye
            { 'M', new Color32(90, 20, 20, 255) },    // open-mouth interior
            { 'T', new Color32(240, 235, 225, 255) }, // teeth
        };

        public static Sprite GorillaShout(int pixelScale = 8)
        {
            string key = $"gorilla_shout_{pixelScale}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var sprite = BuildFromHalfRows(GorillaShoutLeftHalf, GorillaColors, pixelScale);
            Cache[key] = sprite;
            return sprite;
        }

        static Sprite BuildFromHalfRows(string[] leftHalfRows, Dictionary<char, Color32> colors, int pixelScale)
        {
            int halfWidth = leftHalfRows[0].Length;
            int width = halfWidth * 2;
            int height = leftHalfRows.Length;
            int texWidth = width * pixelScale;
            int texHeight = height * pixelScale;

            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[texWidth * texHeight];

            for (int row = 0; row < height; row++)
            {
                string half = leftHalfRows[row];
                // Flip vertically once here: row 0 in the art data is the
                // top of the drawing, but texture row 0 is its bottom.
                int texRow = height - 1 - row;

                for (int col = 0; col < width; col++)
                {
                    char c = col < halfWidth ? half[col] : half[width - 1 - col];
                    Color32 color = c != '.' && colors.TryGetValue(c, out var found) ? found : new Color32(0, 0, 0, 0);

                    for (int py = 0; py < pixelScale; py++)
                    {
                        int y = texRow * pixelScale + py;
                        for (int px = 0; px < pixelScale; px++)
                        {
                            int x = col * pixelScale + px;
                            pixels[y * texWidth + x] = color;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), texWidth);
        }
    }
}
