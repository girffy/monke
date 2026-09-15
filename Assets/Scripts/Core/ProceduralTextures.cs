using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Runtime-generated tiling textures. The project ships no art assets, so
    // the ground/terrain detail is synthesized from layered Perlin noise the
    // same way the sound effects are synthesized from tones and noise.
    public static class ProceduralTextures
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Grass(int size = 256)
        {
            return GetOrCreate($"grass_{size}", () => BuildGrass(size));
        }

        static Texture2D GetOrCreate(string key, System.Func<Texture2D> factory)
        {
            if (Cache.TryGetValue(key, out var tex) && tex != null) return tex;
            tex = factory();
            Cache[key] = tex;
            return tex;
        }

        static Texture2D BuildGrass(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };

            var deepGrass = new Color(0.20f, 0.33f, 0.16f);
            var midGrass = new Color(0.28f, 0.43f, 0.21f);
            var paleGrass = new Color(0.38f, 0.52f, 0.26f);
            var dirt = new Color(0.34f, 0.28f, 0.19f);

            var pixels = new Color32[size * size];
            var rng = new System.Random(20260915);

            // Offsets keep the three noise octaves from lining up into an
            // obvious repeating blob.
            float o1 = (float)rng.NextDouble() * 100f;
            float o2 = (float)rng.NextDouble() * 100f;
            float o3 = (float)rng.NextDouble() * 100f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Sampling the noise on a torus (sin/cos of the angle
                    // around each axis) is what makes the result tile
                    // seamlessly instead of showing a hard grid edge every
                    // few metres of ground.
                    float u = x / (float)size;
                    float v = y / (float)size;

                    float broad = TileableNoise(u, v, 3f, o1);
                    float detail = TileableNoise(u, v, 9f, o2);
                    float speckle = TileableNoise(u, v, 26f, o3);

                    float shade = broad * 0.6f + detail * 0.28f + speckle * 0.12f;

                    Color c = shade < 0.45f
                        ? Color.Lerp(deepGrass, midGrass, Mathf.InverseLerp(0.25f, 0.45f, shade))
                        : Color.Lerp(midGrass, paleGrass, Mathf.InverseLerp(0.45f, 0.72f, shade));

                    // Sparse worn-dirt patches where the broad noise dips.
                    float dirtMask = Mathf.InverseLerp(0.30f, 0.16f, broad);
                    if (dirtMask > 0f) c = Color.Lerp(c, dirt, dirtMask * 0.75f);

                    // Fine per-pixel grain so the surface doesn't look like a
                    // smooth gradient up close.
                    float grain = (float)rng.NextDouble() * 0.06f - 0.03f;
                    c.r = Mathf.Clamp01(c.r + grain);
                    c.g = Mathf.Clamp01(c.g + grain);
                    c.b = Mathf.Clamp01(c.b + grain);

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }

        // Perlin sampled around a torus so the texture wraps on both axes.
        static float TileableNoise(float u, float v, float frequency, float offset)
        {
            float twoPi = Mathf.PI * 2f;
            float nx = Mathf.Cos(u * twoPi) * frequency / twoPi + offset;
            float ny = Mathf.Sin(u * twoPi) * frequency / twoPi + offset;
            float nz = Mathf.Cos(v * twoPi) * frequency / twoPi + offset;
            float nw = Mathf.Sin(v * twoPi) * frequency / twoPi + offset;

            // Two 2D Perlin lookups averaged approximates the 4D sample well
            // enough for ground detail and avoids hand-rolling 4D noise.
            return (Mathf.PerlinNoise(nx, nz) + Mathf.PerlinNoise(ny, nw)) * 0.5f;
        }
    }
}
