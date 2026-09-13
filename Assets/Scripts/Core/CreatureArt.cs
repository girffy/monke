using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Simple but distinct procedural sprites for each character/pickup type,
    // built from PixelArt primitives. Cached so repeated spawns (enemies, XP
    // orbs) don't regenerate a texture each time.
    public static class CreatureArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<Sprite> factory)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            s = factory();
            Cache[key] = s;
            return s;
        }

        public static Sprite Gorilla(int size = 48) => Cached($"gorilla_{size}", () => BuildGorilla(size));
        public static Sprite Human(int size = 32) => Cached($"human_{size}", () => BuildHuman(size));
        public static Sprite Gem(int size = 16) => Cached($"gem_{size}", () => BuildGem(size));
        public static Sprite Banana(int size = 22) => Cached($"banana_{size}", () => BuildBanana(size));
        public static Sprite Adrenaline(int size = 22) => Cached($"adrenaline_{size}", () => BuildAdrenaline(size));
        public static Sprite Rampage(int size = 22) => Cached($"rampage_{size}", () => BuildRampage(size));
        public static Sprite SwipeWedge(int size = 64, float arcDegrees = 80f) => Cached($"swipe_{size}_{arcDegrees}", () => BuildSwipeWedge(size, arcDegrees));

        static Sprite BuildGorilla(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            float c = size / 2f;

            var furDark = new Color(0.16f, 0.11f, 0.09f);
            var fur = new Color(0.32f, 0.21f, 0.15f);
            var skin = new Color(0.60f, 0.45f, 0.36f);
            var pupil = new Color(0.05f, 0.05f, 0.05f);

            // body
            PixelArt.FillEllipse(tex, c, c * 0.92f, size * 0.34f, size * 0.30f, fur);
            // head
            PixelArt.FillEllipse(tex, c, c * 1.42f, size * 0.27f, size * 0.24f, fur);
            // ears
            PixelArt.FillEllipse(tex, c - size * 0.20f, c * 1.40f, size * 0.09f, size * 0.09f, furDark);
            PixelArt.FillEllipse(tex, c + size * 0.20f, c * 1.40f, size * 0.09f, size * 0.09f, furDark);
            // face patch
            PixelArt.FillEllipse(tex, c, c * 1.34f, size * 0.16f, size * 0.14f, skin);
            // belly patch
            PixelArt.FillEllipse(tex, c, c * 0.78f, size * 0.17f, size * 0.16f, skin);
            // eyes
            PixelArt.FillEllipse(tex, c - size * 0.08f, c * 1.38f, size * 0.03f, size * 0.035f, pupil);
            PixelArt.FillEllipse(tex, c + size * 0.08f, c * 1.38f, size * 0.03f, size * 0.035f, pupil);
            // nostrils
            PixelArt.FillEllipse(tex, c - size * 0.035f, c * 1.28f, size * 0.02f, size * 0.02f, furDark);
            PixelArt.FillEllipse(tex, c + size * 0.035f, c * 1.28f, size * 0.02f, size * 0.02f, furDark);
            // arms
            PixelArt.FillEllipse(tex, c - size * 0.36f, c * 0.85f, size * 0.10f, size * 0.20f, fur);
            PixelArt.FillEllipse(tex, c + size * 0.36f, c * 0.85f, size * 0.10f, size * 0.20f, fur);

            return PixelArt.ToSprite(tex, size);
        }

        static Sprite BuildHuman(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            float c = size / 2f;

            var skin = new Color(0.85f, 0.68f, 0.58f);
            var shirt = new Color(0.85f, 0.22f, 0.20f);
            var pants = new Color(0.30f, 0.30f, 0.36f);
            var hair = new Color(0.20f, 0.15f, 0.12f);

            // legs
            PixelArt.FillEllipse(tex, c - size * 0.10f, c * 0.62f, size * 0.08f, size * 0.20f, pants);
            PixelArt.FillEllipse(tex, c + size * 0.10f, c * 0.62f, size * 0.08f, size * 0.20f, pants);
            // arms
            PixelArt.FillEllipse(tex, c - size * 0.26f, c * 1.02f, size * 0.07f, size * 0.17f, skin);
            PixelArt.FillEllipse(tex, c + size * 0.26f, c * 1.02f, size * 0.07f, size * 0.17f, skin);
            // torso
            PixelArt.FillEllipse(tex, c, c * 1.05f, size * 0.20f, size * 0.22f, shirt);
            // head
            PixelArt.FillEllipse(tex, c, c * 1.46f, size * 0.16f, size * 0.16f, skin);
            // hair
            PixelArt.FillEllipse(tex, c, c * 1.54f, size * 0.17f, size * 0.09f, hair);

            return PixelArt.ToSprite(tex, size);
        }

        static Sprite BuildGem(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            float c = size / 2f;
            var gold = new Color(0.98f, 0.82f, 0.18f);
            var highlight = new Color(1f, 0.96f, 0.75f);

            var points = new List<Vector2>
            {
                new Vector2(c, size * 0.92f),
                new Vector2(size * 0.85f, c),
                new Vector2(c, size * 0.08f),
                new Vector2(size * 0.15f, c),
            };
            PixelArt.FillPolygon(tex, points, gold);
            PixelArt.FillEllipse(tex, c - size * 0.08f, c + size * 0.10f, size * 0.08f, size * 0.08f, highlight);

            return PixelArt.ToSprite(tex, size);
        }

        static Sprite BuildBanana(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            float c = size / 2f;
            var yellow = new Color(0.95f, 0.82f, 0.15f);
            var tip = new Color(0.45f, 0.35f, 0.12f);

            PixelArt.FillEllipse(tex, c, c * 0.92f, size * 0.44f, size * 0.24f, yellow);
            PixelArt.FillEllipse(tex, c, c * 1.20f, size * 0.44f, size * 0.24f, yellow, erase: true);
            PixelArt.FillEllipse(tex, size * 0.16f, c * 0.78f, size * 0.05f, size * 0.05f, tip);
            PixelArt.FillEllipse(tex, size * 0.86f, c * 0.86f, size * 0.045f, size * 0.045f, tip);

            return PixelArt.ToSprite(tex, size);
        }

        static Sprite BuildAdrenaline(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            var cyan = new Color(0.25f, 0.85f, 0.95f);

            var points = new List<Vector2>
            {
                new Vector2(size * 0.58f, size * 0.95f),
                new Vector2(size * 0.30f, size * 0.50f),
                new Vector2(size * 0.46f, size * 0.50f),
                new Vector2(size * 0.38f, size * 0.05f),
                new Vector2(size * 0.72f, size * 0.55f),
                new Vector2(size * 0.54f, size * 0.55f),
            };
            PixelArt.FillPolygon(tex, points, cyan);

            return PixelArt.ToSprite(tex, size);
        }

        static Sprite BuildRampage(int size)
        {
            var tex = PixelArt.NewCanvas(size);
            float c = size / 2f;
            var magenta = new Color(0.85f, 0.20f, 0.75f);
            var dark = new Color(0.55f, 0.10f, 0.48f);

            // fist mass
            PixelArt.FillEllipse(tex, c, c * 1.05f, size * 0.30f, size * 0.26f, magenta);
            // knuckle bumps
            PixelArt.FillEllipse(tex, c - size * 0.20f, c * 1.28f, size * 0.11f, size * 0.11f, magenta);
            PixelArt.FillEllipse(tex, c, c * 1.32f, size * 0.11f, size * 0.11f, magenta);
            PixelArt.FillEllipse(tex, c + size * 0.20f, c * 1.28f, size * 0.11f, size * 0.11f, magenta);
            // wrist
            PixelArt.FillEllipse(tex, c, c * 0.55f, size * 0.16f, size * 0.20f, dark);

            return PixelArt.ToSprite(tex, size);
        }

        // Fan-shaped swipe effect, authored pointing "up" (+Y) from a pivot near
        // the bottom edge — callers rotate via transform.up to aim it, and scale
        // to match the actual attack radius (drawn at ~0.88 world units at scale 1).
        static Sprite BuildSwipeWedge(int size, float arcDegrees)
        {
            var tex = PixelArt.NewCanvas(size);
            var color = new Color(1f, 1f, 1f, 0.55f);

            float cx = size / 2f;
            float pivotY = size * 0.12f;
            float radius = size * 0.88f;
            const int segments = 16;

            var points = new List<Vector2> { new Vector2(cx, pivotY) };
            float startAngle = 90f - arcDegrees / 2f;
            float endAngle = 90f + arcDegrees / 2f;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                points.Add(new Vector2(cx + Mathf.Cos(angle) * radius, pivotY + Mathf.Sin(angle) * radius));
            }
            PixelArt.FillPolygon(tex, points, color);

            return PixelArt.ToSprite(tex, size, new Vector2(0.5f, pivotY / size));
        }
    }
}
