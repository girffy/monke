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

        public enum IconShape { Fist, Claw, Chevron, Burst, Bolt }

        // Simple silhouette glyphs for the ability bar, drawn as a handful
        // of thick analytic line segments (or a couple of circles for Fist)
        // rather than needing real imported art.
        public static Sprite Icon(IconShape shape, Color color, int size = 48)
        {
            return GetOrCreate($"icon_{shape}_{ColorKey(color)}_{size}", () => BuildIcon(shape, color, size));
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

        static Sprite BuildIcon(IconShape shape, Color color, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float s = size;
            float thickness = s * 0.16f;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float coverage = ShapeCoverage(shape, p, s, thickness);
                    var c = color;
                    c.a *= coverage;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static float ShapeCoverage(IconShape shape, Vector2 p, float s, float thickness)
        {
            switch (shape)
            {
                case IconShape.Fist:
                {
                    var center = new Vector2(s * 0.52f, s * 0.44f);
                    float edge = CircleCoverage(p, center, s * 0.30f);
                    var thumb = new Vector2(s * 0.26f, s * 0.34f);
                    float thumbEdge = CircleCoverage(p, thumb, s * 0.13f);
                    return Mathf.Max(edge, thumbEdge);
                }
                case IconShape.Claw:
                {
                    float best = 0f;
                    for (int i = -1; i <= 1; i++)
                    {
                        float offset = i * s * 0.20f;
                        var a = new Vector2(s * 0.20f + offset, s * 0.80f);
                        var b = new Vector2(s * 0.80f + offset, s * 0.20f);
                        best = Mathf.Max(best, LineCoverage(p, a, b, thickness * 0.6f));
                    }
                    return best;
                }
                case IconShape.Chevron:
                {
                    var tip = new Vector2(s * 0.74f, s * 0.5f);
                    var top = new Vector2(s * 0.26f, s * 0.20f);
                    var bot = new Vector2(s * 0.26f, s * 0.80f);
                    return Mathf.Max(LineCoverage(p, top, tip, thickness), LineCoverage(p, tip, bot, thickness));
                }
                case IconShape.Burst:
                {
                    var center = new Vector2(s * 0.5f, s * 0.5f);
                    float best = 0f;
                    const int spikes = 6;
                    for (int i = 0; i < spikes; i++)
                    {
                        float angle = i * Mathf.PI * 2f / spikes;
                        var tip = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s * 0.40f;
                        best = Mathf.Max(best, LineCoverage(p, center, tip, thickness * 0.55f));
                    }
                    return best;
                }
                case IconShape.Bolt:
                {
                    var a = new Vector2(s * 0.58f, s * 0.08f);
                    var b = new Vector2(s * 0.30f, s * 0.52f);
                    var c = new Vector2(s * 0.50f, s * 0.52f);
                    var d = new Vector2(s * 0.24f, s * 0.92f);
                    float best = LineCoverage(p, a, b, thickness * 0.7f);
                    best = Mathf.Max(best, LineCoverage(p, b, c, thickness * 0.7f));
                    best = Mathf.Max(best, LineCoverage(p, c, d, thickness * 0.7f));
                    return best;
                }
            }
            return 0f;
        }

        static float CircleCoverage(Vector2 p, Vector2 center, float radius)
        {
            float dist = Vector2.Distance(p, center);
            return Mathf.Clamp01((radius - dist) / 1.5f);
        }

        static float LineCoverage(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            Vector2 proj = a + ab * t;
            float dist = Vector2.Distance(p, proj);
            return Mathf.Clamp01((thickness * 0.5f - dist) / 1.5f);
        }
    }
}
