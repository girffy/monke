using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Small procedural-art primitives: fill ellipses/polygons onto a transparent
    // texture, then turn it into a Sprite. Used to build simple, readable
    // creature/icon shapes without needing external art files.
    public static class PixelArt
    {
        public static Texture2D NewCanvas(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color32[size * size];
            tex.SetPixels32(clear);
            return tex;
        }

        // Alpha-blended ellipse with a 1px soft edge. `erase` forces alpha toward
        // 0 instead of blending toward `color` — used to carve crescents etc.
        public static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color color, bool erase = false)
        {
            int w = tex.width, h = tex.height;
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (x + 0.5f - cx) / rx;
                    float ny = (y + 0.5f - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float coverage = Mathf.Clamp01(1f - (d - 1f) / 0.15f);
                    if (coverage <= 0f) continue;

                    var existing = tex.GetPixel(x, y);
                    Color result;
                    if (erase)
                    {
                        result = existing;
                        result.a = Mathf.Lerp(existing.a, 0f, coverage);
                    }
                    else
                    {
                        float effectiveAlpha = coverage * color.a;
                        result = new Color(
                            Mathf.Lerp(existing.r, color.r, effectiveAlpha),
                            Mathf.Lerp(existing.g, color.g, effectiveAlpha),
                            Mathf.Lerp(existing.b, color.b, effectiveAlpha),
                            existing.a + effectiveAlpha * (1f - existing.a));
                    }
                    tex.SetPixel(x, y, result);
                }
            }
        }

        public static void FillCircle(Texture2D tex, float cx, float cy, float r, Color color, bool erase = false)
        {
            FillEllipse(tex, cx, cy, r, r, color, erase);
        }

        // Hard-edged polygon fill (crossing-number test). Points in pixel space.
        public static void FillPolygon(Texture2D tex, IList<Vector2> points, Color color)
        {
            int w = tex.width, h = tex.height;
            float minXf = float.MaxValue, maxXf = float.MinValue, minYf = float.MaxValue, maxYf = float.MinValue;
            foreach (var p in points)
            {
                minXf = Mathf.Min(minXf, p.x); maxXf = Mathf.Max(maxXf, p.x);
                minYf = Mathf.Min(minYf, p.y); maxYf = Mathf.Max(maxYf, p.y);
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(minXf));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(maxXf));
            int minY = Mathf.Max(0, Mathf.FloorToInt(minYf));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(maxYf));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInPolygon(points, x + 0.5f, y + 0.5f))
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        static bool PointInPolygon(IList<Vector2> poly, float px, float py)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];
                bool intersects = ((pi.y > py) != (pj.y > py)) &&
                    (px < (pj.x - pi.x) * (py - pi.y) / (pj.y - pi.y) + pi.x);
                if (intersects) inside = !inside;
            }
            return inside;
        }

        public static Sprite ToSprite(Texture2D tex, int pixelsPerUnit)
        {
            return ToSprite(tex, pixelsPerUnit, new Vector2(0.5f, 0.5f));
        }

        public static Sprite ToSprite(Texture2D tex, int pixelsPerUnit, Vector2 pivot01)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot01, pixelsPerUnit);
        }
    }
}
