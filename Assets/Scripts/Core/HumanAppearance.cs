using UnityEngine;

namespace GorillaSurvivors.Core
{
    public enum HairStyle { Cropped, Mop, Bald, Topknot, Cap }

    // Per-individual cosmetic roll for an enemy. The type's silhouette and
    // shirt HUE stay intact — green is still the fast little guy — but the
    // exact shade, skin, hair, face and build vary, so a wave of a hundred
    // reads as a crowd rather than one man cloned a hundred times.
    public struct HumanAppearance
    {
        public Color Skin;
        public Color Shirt;
        public Color Pants;
        public Color Hair;
        public Color Shoe;
        public Color CapColor;

        public HairStyle Hairstyle;
        public bool Beard;
        public bool Moustache;
        public bool HeavyBrow;

        public float HeightScale;
        public float BuildScale;
        public float EyeSpacing;
        public float EyeSize;
        public float NoseSize;

        static readonly Color[] SkinTones =
        {
            new Color(0.94f, 0.80f, 0.69f),
            new Color(0.88f, 0.71f, 0.58f),
            new Color(0.80f, 0.62f, 0.48f),
            new Color(0.66f, 0.47f, 0.34f),
            new Color(0.51f, 0.35f, 0.25f),
            new Color(0.38f, 0.26f, 0.19f),
            new Color(0.29f, 0.19f, 0.14f),
        };

        static readonly Color[] HairColors =
        {
            new Color(0.10f, 0.08f, 0.07f),  // black
            new Color(0.24f, 0.16f, 0.11f),  // dark brown
            new Color(0.40f, 0.26f, 0.14f),  // brown
            new Color(0.62f, 0.45f, 0.22f),  // dark blond
            new Color(0.82f, 0.70f, 0.42f),  // blond
            new Color(0.58f, 0.26f, 0.12f),  // ginger
            new Color(0.72f, 0.72f, 0.70f),  // grey
        };

        static readonly Color[] PantsColors =
        {
            new Color(0.24f, 0.25f, 0.31f),
            new Color(0.18f, 0.22f, 0.32f),
            new Color(0.30f, 0.28f, 0.24f),
            new Color(0.22f, 0.20f, 0.19f),
            new Color(0.35f, 0.31f, 0.26f),
            new Color(0.26f, 0.30f, 0.27f),
        };

        static readonly Color[] ShoeColors =
        {
            new Color(0.12f, 0.11f, 0.11f),
            new Color(0.25f, 0.17f, 0.11f),
            new Color(0.30f, 0.30f, 0.32f),
            new Color(0.45f, 0.40f, 0.34f),
        };

        public static HumanAppearance Roll(Color shirtBase)
        {
            var look = new HumanAppearance
            {
                Skin = SkinTones[Random.Range(0, SkinTones.Length)],
                Hair = HairColors[Random.Range(0, HairColors.Length)],
                Pants = Jitter(PantsColors[Random.Range(0, PantsColors.Length)], 0.01f, 0.08f, 0.10f),
                Shoe = ShoeColors[Random.Range(0, ShoeColors.Length)],
                Shirt = Jitter(shirtBase, 0.025f, 0.16f, 0.16f),
                CapColor = Jitter(shirtBase, 0.08f, 0.2f, 0.25f),

                HeightScale = Random.Range(0.92f, 1.09f),
                BuildScale = Random.Range(0.92f, 1.10f),
                EyeSpacing = Random.Range(0.24f, 0.32f),
                EyeSize = Random.Range(0.13f, 0.19f),
                NoseSize = Random.Range(0.14f, 0.21f),

                HeavyBrow = Random.value < 0.25f,
            };

            float style = Random.value;
            look.Hairstyle = style < 0.34f ? HairStyle.Cropped
                : style < 0.58f ? HairStyle.Mop
                : style < 0.74f ? HairStyle.Bald
                : style < 0.87f ? HairStyle.Topknot
                : HairStyle.Cap;

            // A bald head with a full beard is a good look; a bald head with
            // just a moustache is the rarer one, so they don't both roll.
            look.Beard = Random.value < 0.22f;
            look.Moustache = !look.Beard && Random.value < 0.14f;

            return look;
        }

        // Hue is nudged only slightly so the type still reads by color, while
        // saturation and value move more freely — that's where most of the
        // "these are different people" impression comes from.
        static Color Jitter(Color baseColor, float hueRange, float satRange, float valRange)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            // Near-white and near-grey bases (the Medic's coat) get their
            // jitter scaled right down. Hue is meaningless at low saturation,
            // so the full range would tint a white coat green or pink and
            // break the "white shirt = healer, kill it first" read.
            float saturationWeight = Mathf.Clamp01(s / 0.25f);
            hueRange *= saturationWeight;
            satRange *= saturationWeight;

            h = Mathf.Repeat(h + Random.Range(-hueRange, hueRange), 1f);
            s = Mathf.Clamp01(s + Random.Range(-satRange, satRange));
            v = Mathf.Clamp01(v + Random.Range(-valRange, valRange));
            return Color.HSVToRGB(h, s, v);
        }
    }
}
