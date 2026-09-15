using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Hand-authored pixel art for the ability bar, drawn as character grids
    // and blown up with point filtering so it stays crisp. Grids are either
    // full width, or a left half that gets mirrored (cheaper to author, and
    // the right choice for anything front-facing and symmetrical).
    //
    // Legend:
    //   . transparent   F fur        D fur shadow   B brow/outline
    //   S muzzle skin   E eye white  P pupil        M mouth interior
    //   T teeth         G tongue     Y highlight    W white
    //   R red accent    O orange     C cyan
    public static class PixelArtIcons
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'F', new Color32(96, 70, 56, 255) },
            { 'D', new Color32(62, 44, 35, 255) },
            { 'B', new Color32(32, 22, 18, 255) },
            { 'S', new Color32(158, 120, 98, 255) },
            { 'E', new Color32(236, 229, 214, 255) },
            { 'P', new Color32(20, 16, 14, 255) },
            { 'M', new Color32(92, 24, 26, 255) },
            { 'T', new Color32(245, 242, 234, 255) },
            { 'G', new Color32(186, 74, 82, 255) },
            { 'Y', new Color32(250, 214, 110, 255) },
            { 'W', new Color32(248, 248, 248, 255) },
            { 'R', new Color32(214, 68, 54, 255) },
            { 'O', new Color32(244, 150, 48, 255) },
            { 'C', new Color32(126, 206, 244, 255) },
        };

        // Front-facing silverback mid-roar: heavy brow, eyes pinched angry,
        // jaws wide with both rows of teeth showing.
        static readonly string[] ShoutHalf =
        {
            "...........",
            "........DDD",
            "......DDFFF",
            ".....DFFFFF",
            "....DFFFFFF",
            "...DFFFFFFF",
            "..DFFFFFFFF",
            "..DFBBBBBBB",
            ".FDFBBBBBBB",
            ".FDFBEPPBBB",
            ".FDFBEEPSSS",
            "..DFFSSSSSS",
            "..DFSSSMMMM",
            "..DFSMTTTTT",
            "..DFSMMMMMM",
            "..DFSMMGGGG",
            "..DFSMTTTTT",
            "...DFSMMMMM",
            "...DFFSSSSS",
            "....DFFFFFF",
            ".....DDFFFF",
            ".......DDD.",
        };

        // Both fists driven into the ground, with the impact burst under them.
        static readonly string[] SlamHalf =
        {
            "...........",
            "...........",
            "....DDDD...",
            "...DFFFFD..",
            "..DFFFFFFD.",
            "..DFFFFFFD.",
            "..DFBFBFBD.",
            "..DFFFFFFD.",
            "..DDFFFFDD.",
            "...DDDDDD..",
            "...........",
            "......YY...",
            "...YYYYYYY.",
            "..YYWWWWWWW",
            "...YYWWWWWW",
            "......YWWWW",
            "...Y...YYYY",
            "..Y.Y....YY",
            ".Y...Y.....",
            "...........",
            "...........",
            "...........",
        };

        // Three parallel claw rakes, thick at the top and tapering out — the
        // earlier version crossed over itself and read as a tick mark.
        static readonly string[] ClawFull =
        {
            "......................",
            "......................",
            ".....W.....W.....W....",
            ".....WW....WW....WW...",
            "....WWW...WWW...WWW...",
            "....WWW...WWW...WWW...",
            "...WWW...WWW...WWW....",
            "...WWW...WWW...WWW....",
            "..WWW...WWW...WWW.....",
            "..WW....WW....WW......",
            "..WW....WW....WW......",
            ".WW....WW....WW.......",
            ".W.....W.....W........",
            ".W.....W.....W........",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
        };

        // A gorilla bolting to the right, trailed by speed lines.
        static readonly string[] DashFull =
        {
            "......................",
            "......................",
            "......................",
            "...........DDDDD......",
            "..........DFFFFFD.....",
            "....CC....DFBEFFD.....",
            "..........DFFFFFFD....",
            "..CCCC...DDFFFFFFD....",
            ".........DFFFFFFFD....",
            "..CCCCC..DFFFFFFFFD...",
            ".........DFFFFFFFFD...",
            "..CCCC...DFFFFFFFD....",
            ".........DDFFFFFFD....",
            "..CCC.....DFFDDFFD....",
            "..........DFD..DFD....",
            "..........DD....DD....",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
            "......................",
        };

        // Head-down shoulder barge coming at the camera: mouth shut, brow
        // forward, huge shoulders, motion streaks flaring off both flanks.
        // Deliberately posed differently from the roar so the two gorilla
        // faces don't read as the same icon twice.
        static readonly string[] ChargeHalf =
        {
            "...........",
            "...........",
            "........DDD",
            "......DDFFF",
            ".....DFFFFF",
            ".....DFBBBB",
            ".....DFBEPP",
            "....DFFSSSS",
            "...DFFFFFFF",
            "..DFFFFFFFF",
            ".DFFFFFFFFF",
            "DFFFFFFFFFF",
            "DFFFFFFFFFF",
            "DFFDDDFFFFF",
            "DFD...DFFFF",
            "DD.....DFFF",
            "O.......DDD",
            "OO.........",
            ".OOO.......",
            "OOOOO......",
            ".OOO.......",
            "...........",
        };

        public static Sprite GorillaShout(int pixelScale = 6) => GetOrBuild("shout", ShoutHalf, true, pixelScale);
        public static Sprite Slam(int pixelScale = 6) => GetOrBuild("slam", SlamHalf, true, pixelScale);
        public static Sprite Claw(int pixelScale = 6) => GetOrBuild("claw", ClawFull, false, pixelScale);
        public static Sprite Dash(int pixelScale = 6) => GetOrBuild("dash", DashFull, false, pixelScale);
        public static Sprite Charge(int pixelScale = 6) => GetOrBuild("charge", ChargeHalf, true, pixelScale);

        static Sprite GetOrBuild(string key, string[] rows, bool mirrored, int pixelScale)
        {
            string cacheKey = $"{key}_{pixelScale}";
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            var sprite = Build(rows, mirrored, pixelScale);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        static Sprite Build(string[] rows, bool mirrored, int pixelScale)
        {
            int sourceWidth = rows[0].Length;
            int width = mirrored ? sourceWidth * 2 : sourceWidth;
            int height = rows.Length;
            int texWidth = width * pixelScale;
            int texHeight = height * pixelScale;

            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[texWidth * texHeight];
            var clear = new Color32(0, 0, 0, 0);

            for (int row = 0; row < height; row++)
            {
                string line = rows[row];
                // Row 0 is the top of the drawing; texture row 0 is its
                // bottom, so flip once here rather than authoring upside down.
                int texRow = height - 1 - row;

                for (int col = 0; col < width; col++)
                {
                    int sourceCol = !mirrored ? col
                        : (col < sourceWidth ? col : width - 1 - col);
                    char c = sourceCol < line.Length ? line[sourceCol] : '.';
                    Color32 color = c != '.' && Palette.TryGetValue(c, out var found) ? found : clear;

                    for (int py = 0; py < pixelScale; py++)
                    {
                        int y = texRow * pixelScale + py;
                        for (int px = 0; px < pixelScale; px++)
                        {
                            pixels[y * texWidth + col * pixelScale + px] = color;
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
