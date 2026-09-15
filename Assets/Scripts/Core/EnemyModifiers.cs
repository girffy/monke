using UnityEngine;

namespace GorillaSurvivors.Core
{
    public enum ModifierTier { None, Bronze, Silver, Gold, Diamond }

    // Rolled per-enemy on top of its base HumanVariant stats (see
    // EnemyFactory). Armor/Weapon/Shoes are independent and stack freely;
    // Crown is a rare extra multiplier layered on top of whatever else was
    // rolled ("all 3 buffs at once"), not a replacement for them. Odds scale
    // with round so the toughest combinations are rare early and increasingly
    // common late, deliberately outpacing how fast the player is expected to
    // get stronger so the game keeps getting harder.
    public struct EnemyModifierRoll
    {
        public ModifierTier Armor;
        public ModifierTier Weapon;
        public bool HasShoes;
        public bool HasCrown;

        const float CrownStatMult = 1.6f;
        const float CrownSpeedMult = 1.25f;
        const float ShoesSpeedMult = 1.35f;

        public float HPMultiplier => TierHPMult(Armor) * (HasCrown ? CrownStatMult : 1f);
        public float DamageMultiplier => TierDamageMult(Weapon) * (HasCrown ? CrownStatMult : 1f);
        public float SpeedMultiplier => (HasShoes ? ShoesSpeedMult : 1f) * (HasCrown ? CrownSpeedMult : 1f);

        // A modified enemy is worth more XP roughly in proportion to how
        // much tougher it is, so hunting them down is worth the risk.
        public float XPMultiplier
        {
            get
            {
                float mult = 1f;
                if (Armor != ModifierTier.None) mult += 0.25f + 0.15f * (int)Armor;
                if (Weapon != ModifierTier.None) mult += 0.25f + 0.15f * (int)Weapon;
                if (HasShoes) mult += 0.2f;
                if (HasCrown) mult += 0.6f;
                return mult;
            }
        }

        public bool IsPlain => Armor == ModifierTier.None && Weapon == ModifierTier.None && !HasShoes && !HasCrown;

        static float TierHPMult(ModifierTier t) => t switch
        {
            ModifierTier.Bronze => 1.4f,
            ModifierTier.Silver => 2.0f,
            ModifierTier.Gold => 2.8f,
            ModifierTier.Diamond => 4.0f,
            _ => 1f,
        };

        static float TierDamageMult(ModifierTier t) => t switch
        {
            ModifierTier.Bronze => 1.3f,
            ModifierTier.Silver => 1.7f,
            ModifierTier.Gold => 2.3f,
            ModifierTier.Diamond => 3.2f,
            _ => 1f,
        };

        public static Color TierColor(ModifierTier t) => t switch
        {
            ModifierTier.Bronze => new Color(0.72f, 0.45f, 0.20f),
            ModifierTier.Silver => new Color(0.78f, 0.79f, 0.81f),
            ModifierTier.Gold => new Color(0.95f, 0.80f, 0.25f),
            ModifierTier.Diamond => new Color(0.55f, 0.90f, 0.95f),
            _ => Color.white,
        };

        public static EnemyModifierRoll Roll(int round)
        {
            return new EnemyModifierRoll
            {
                Armor = RollTier(round),
                Weapon = RollTier(round),
                HasShoes = Random.value < Mathf.Clamp01(0.04f + round * 0.01f),
                HasCrown = Random.value < Mathf.Clamp01(0.01f + round * 0.006f),
            };
        }

        static ModifierTier RollTier(int round)
        {
            // Chance of getting ANY tier at all grows with round; once it
            // procs, a second roll decides how good it is — higher tiers
            // only really start showing up much later.
            float chanceAny = Mathf.Clamp01(0.05f + round * 0.015f);
            if (Random.value >= chanceAny) return ModifierTier.None;

            float tierRoll = Random.value;
            float diamondChance = Mathf.Clamp01(round * 0.012f);
            float goldChance = Mathf.Clamp01(0.06f + round * 0.02f);
            float silverChance = Mathf.Clamp01(0.18f + round * 0.03f);

            if (tierRoll < diamondChance) return ModifierTier.Diamond;
            if (tierRoll < diamondChance + goldChance) return ModifierTier.Gold;
            if (tierRoll < diamondChance + goldChance + silverChance) return ModifierTier.Silver;
            return ModifierTier.Bronze;
        }
    }
}
