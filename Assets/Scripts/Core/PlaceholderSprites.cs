using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Generates simple placeholder circle/square sprites at runtime so the game is
    // playable before any real art exists. Swap these out per-GameObject later by
    // assigning real sprites to the SpriteRenderers this creates.
    public static class PlaceholderSprites
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Circle(Color color, int diameter = 32)
        {
            return GetOrCreate($"circle_{ColorKey(color)}_{diameter}", () => BuildCircle(color, diameter));
        }

        public static Sprite Square(Color color, int size = 32)
        {
            return GetOrCreate($"square_{ColorKey(color)}_{size}", () => BuildSquare(color, size));
        }

        static Sprite GetOrCreate(string key, System.Func<Sprite> factory)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = factory();
            Cache[key] = sprite;
            return sprite;
        }

        static string ColorKey(Color c) => $"{c.r:F2}_{c.g:F2}_{c.b:F2}_{c.a:F2}";

        static Sprite BuildCircle(Color color, int diameter)
        {
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = diameter / 2f;
            var center = new Vector2(radius, radius);
            var pixels = new Color32[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist);
                    var c = color;
                    c.a *= alpha > 0.5f ? 1f : alpha * 2f;
                    pixels[y * diameter + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), diameter);
        }

        static Sprite BuildSquare(Color color, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
