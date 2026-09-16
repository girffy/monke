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

        public static Texture2D Grass(int size = 512)
        {
            return GetOrCreate($"grass_{size}", () => BuildGrass(size));
        }

        // The colosseum floor: raked sand over packed earth, with darker
        // trodden patches and scattered grit. Warm enough to sit against the
        // grey stone of the ring without the two greying into each other.
        // 1024 rather than 512, and tiled far more slowly by the arena, so
        // the repeat is a patch the size of the whole floor rather than an
        // obvious grid of identical squares.
        public static Texture2D Sand(int size = 1024)
        {
            return GetOrCreate($"sand_{size}", () => BuildSand(size));
        }

        static Texture2D BuildSand(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };

            var packed = new Color(0.44f, 0.36f, 0.25f);
            var midSand = new Color(0.60f, 0.51f, 0.35f);
            var paleSand = new Color(0.74f, 0.65f, 0.47f);
            var trodden = new Color(0.34f, 0.27f, 0.19f);
            var grit = new Color(0.56f, 0.54f, 0.50f);
            var crackDark = new Color(0.21f, 0.16f, 0.11f);

            var pixels = new Color32[size * size];
            var rng = new System.Random(776611);

            float o1 = (float)rng.NextDouble() * 100f;
            float o2 = (float)rng.NextDouble() * 100f;
            float o3 = (float)rng.NextDouble() * 100f;
            float o4 = (float)rng.NextDouble() * 100f;
            float o5 = (float)rng.NextDouble() * 100f;
            float o6 = (float)rng.NextDouble() * 100f;
            float o7 = (float)rng.NextDouble() * 100f;
            float o8 = (float)rng.NextDouble() * 100f;
            float o9 = (float)rng.NextDouble() * 100f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Five octaves rather than three. The low frequencies are
                    // what stop the floor reading as one flat tone, and the
                    // very low one (1.7) is deliberately below the tile size
                    // so a single repeat still has large light and dark
                    // regions rather than uniform speckle.
                    float sweep = TileableNoise(u, v, 1.7f, o5);
                    float broad = TileableNoise(u, v, 3.5f, o1);
                    float medium = TileableNoise(u, v, 6.5f, o6);
                    float detail = TileableNoise(u, v, 11f, o2);
                    float speckle = TileableNoise(u, v, 30f, o3);
                    // Stretched heavily along one axis so it reads as rake
                    // lines dragged across the floor.
                    float rake = TileableNoise(u * 0.18f, v * 2.2f, 46f, o4);
                    // A second rake at a different angle and scale, so the
                    // lines cross rather than running as one corduroy grain.
                    float rakeCross = TileableNoise(u * 1.9f, v * 0.22f, 38f, o7);

                    float shade = sweep * 0.26f + broad * 0.24f + medium * 0.16f
                                  + detail * 0.16f + speckle * 0.08f
                                  + rake * 0.12f + rakeCross * 0.08f;

                    Color c = shade < 0.47f
                        ? Color.Lerp(packed, midSand, Mathf.InverseLerp(0.24f, 0.47f, shade))
                        : Color.Lerp(midSand, paleSand, Mathf.InverseLerp(0.47f, 0.76f, shade));

                    // Churned, darker ground where the broad noise dips —
                    // the places the fight has been over and over.
                    float wornMask = Mathf.InverseLerp(0.36f, 0.18f, broad * 0.6f + sweep * 0.4f);
                    if (wornMask > 0f) c = Color.Lerp(c, trodden, wornMask * 0.8f);

                    // Scuffed pale patches where the sand has been kicked up,
                    // keyed off a different octave so they never coincide
                    // with the worn ones.
                    float scuff = Mathf.InverseLerp(0.63f, 0.82f, medium);
                    if (scuff > 0f) c = Color.Lerp(c, paleSand, scuff * 0.45f);

                    // Cracks in the dried-out ground. Ridged noise — folding
                    // the noise about its midpoint — turns smooth blobs into
                    // thin branching seams, which is the one feature that
                    // stops a sand texture reading as pure fuzz.
                    // The frequencies here have to be HIGH. The floor is 30
                    // metres across and the texture tiles barely more than
                    // once over it, so a crack drawn at the same frequency as
                    // the broad shading is metres wide on the ground — the
                    // first attempt read as dark scribbles crawling across
                    // the arena rather than as cracked earth.
                    float ridge = 1f - Mathf.Abs(TileableNoise(u, v, 48f, o8) * 2f - 1f);
                    float crack = Mathf.InverseLerp(0.955f, 1f, ridge);
                    if (crack > 0f) c = Color.Lerp(c, crackDark, crack * 0.22f);

                    // A second, finer set at a different scale so the seams
                    // read as a network rather than one wandering line.
                    float ridge2 = 1f - Mathf.Abs(TileableNoise(u, v, 96f, o9) * 2f - 1f);
                    float crack2 = Mathf.InverseLerp(0.965f, 1f, ridge2);
                    if (crack2 > 0f) c = Color.Lerp(c, crackDark, crack2 * 0.14f);

                    if (speckle > 0.74f && rng.NextDouble() < 0.28)
                    {
                        c = Color.Lerp(c, grit, Mathf.Lerp(0.3f, 0.7f, (float)rng.NextDouble()));
                    }

                    // Scattered whole stones pressed into the floor: a few
                    // pixels across, dark-rimmed, and rare enough to read as
                    // individual objects rather than noise.
                    if (rng.NextDouble() < 0.0016)
                    {
                        c = Color.Lerp(grit, packed, (float)rng.NextDouble() * 0.5f);
                    }

                    float grain = (float)rng.NextDouble() * 0.07f - 0.035f;
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

            var deepGrass = new Color(0.17f, 0.30f, 0.14f);
            var midGrass = new Color(0.27f, 0.42f, 0.20f);
            var paleGrass = new Color(0.40f, 0.54f, 0.27f);
            var dryGrass = new Color(0.55f, 0.53f, 0.28f);
            var dirt = new Color(0.34f, 0.28f, 0.19f);
            var pebble = new Color(0.52f, 0.50f, 0.47f);

            var pixels = new Color32[size * size];
            var rng = new System.Random(20260915);

            // Offsets keep the three noise octaves from lining up into an
            // obvious repeating blob.
            float o1 = (float)rng.NextDouble() * 100f;
            float o2 = (float)rng.NextDouble() * 100f;
            float o3 = (float)rng.NextDouble() * 100f;
            float o4 = (float)rng.NextDouble() * 100f;
            float o5 = (float)rng.NextDouble() * 100f;

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
                    // A high-frequency anisotropic layer: sampling at very
                    // different frequencies per axis stretches the noise into
                    // streaks that read as blades of grass rather than blobs.
                    float blades = TileableNoise(u * 1.6f, v * 0.25f, 60f, o4);

                    float shade = broad * 0.52f + detail * 0.26f + speckle * 0.12f + blades * 0.10f;

                    Color c = shade < 0.45f
                        ? Color.Lerp(deepGrass, midGrass, Mathf.InverseLerp(0.25f, 0.45f, shade))
                        : Color.Lerp(midGrass, paleGrass, Mathf.InverseLerp(0.45f, 0.72f, shade));

                    // Sun-bleached patches, offset from the dirt so the two
                    // don't land in the same places.
                    float dryMask = Mathf.InverseLerp(0.62f, 0.80f, TileableNoise(u, v, 4.5f, o5));
                    if (dryMask > 0f) c = Color.Lerp(c, dryGrass, dryMask * 0.55f);

                    // Sparse worn-dirt patches where the broad noise dips.
                    float dirtMask = Mathf.InverseLerp(0.30f, 0.16f, broad);
                    if (dirtMask > 0f)
                    {
                        c = Color.Lerp(c, dirt, dirtMask * 0.8f);
                        // Scattered grit, only inside the bare patches.
                        if (speckle > 0.74f && rng.NextDouble() < 0.28)
                        {
                            c = Color.Lerp(c, pebble, dirtMask * 0.7f);
                        }
                    }

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
