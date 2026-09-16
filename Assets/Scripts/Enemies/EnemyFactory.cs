using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // Builds a fully-wired enemy GameObject from code — no prefab asset needed,
    // so difficulty/appearance tuning lives in one place.
    public static class EnemyFactory
    {
        public static GameObject Create(HumanVariant type, Vector3 position, float difficultyScale, int round)
        {
            var modifiers = EnemyModifierRoll.Roll(round);

            var go = new GameObject(type.ToString());
            go.transform.position = position;

            var model = Blocky3DArt.Human(type, modifiers);
            model.transform.SetParent(go.transform, false);

            go.AddComponent<Rigidbody>();

            var collider = go.AddComponent<CapsuleCollider>();
            // EnemyHealth must be added before EnemyAI: EnemyAI declares
            // [RequireComponent(typeof(EnemyHealth))], and Unity auto-adds a
            // default, never-Init'd EnemyHealth the moment EnemyAI is added
            // if one doesn't already exist. Adding EnemyAI first silently
            // created a SECOND EnemyHealth here — the one Init() below
            // actually configures — while every GetComponent<EnemyHealth>()
            // elsewhere (attacks, HUD, EnemyHealth's own Die()) found the
            // first, uninitialized one instead, so real HP/XP tuning below
            // never mattered and enemies were effectively always ~20 HP.
            var health = go.AddComponent<EnemyHealth>();
            var ai = go.AddComponent<EnemyAI>();

            var anim = go.AddComponent<CharacterAnimator>();
            anim.ReferenceSpeed = 2.4f;
            anim.StrideFrequency = 3.4f;
            anim.LegSwing = 42f;
            anim.ArmSwing = 34f;
            anim.BobHeight = 0.05f;
            anim.LeanDegrees = 8f;

            // Contact/projectile damage is written against the ROUND rather
            // than difficultyScale, because it is tuned against how much HP
            // the player is expected to have by then.
            //
            // The player gains ~10 levels in round 1 alone (100 kills is a
            // lot of XP) and roughly +10 max HP per level, so expected HP is
            // about 200 by the end of round 1 and climbs ~12 a round after
            // that. The targets are ~15 hits to die from the weak enemies and
            // ~6 from the heavy ones, which is where these numbers come from:
            // the old figures were set when the player had ~100 HP and had
            // quietly become unable to kill anyone.
            float r = round - 1;

            float baseHP, baseXP, baseMoveSpeed;
            float baseContactDamage = 0f, baseProjectileDamage = 0f, baseProjectileInterval = 0f, preferredRange = 0f;
            bool isRanged = false;

            switch (type)
            {
                case HumanVariant.Runner:
                    collider.radius = 0.30f;
                    collider.height = 1.4f;
                    collider.center = new Vector3(0f, 0.7f, 0f);
                    // A fast, fragile pest, not a damage check: one swipe for
                    // a long time.
                    baseHP = 7f + 0.7f * difficultyScale;
                    baseXP = 2f;
                    baseMoveSpeed = 3.4f + 0.4f * Mathf.Min(difficultyScale, 4f);
                    // The lightest touch in the game — but it comes at you
                    // fast and in numbers, so it still adds up.
                    baseContactDamage = 10f + 0.7f * r;
                    break;

                case HumanVariant.Brute:
                    collider.radius = 0.55f;
                    collider.height = 2.4f;
                    collider.center = new Vector3(0f, 1.2f, 0f);
                    // The "big guy": two slams or four swipes in round 1, and
                    // tankier every round so damage upgrades stay meaningful
                    // against it long after Grunts stop mattering.
                    baseHP = 34f + 3.0f * difficultyScale;
                    baseXP = 7f;
                    baseMoveSpeed = 1.1f + 0.2f * Mathf.Min(difficultyScale, 4f);
                    // The heaviest hitter that walks: about six of these and
                    // you are dead, so being cornered by one is a real loss.
                    baseContactDamage = 34f + 2.2f * r;
                    break;

                case HumanVariant.Thrower:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    baseHP = 10f + 0.9f * difficultyScale;
                    baseXP = 4f;
                    baseMoveSpeed = 1.6f;
                    isRanged = true;
                    preferredRange = 5f;
                    baseProjectileDamage = 12f + 0.9f * r;
                    baseProjectileInterval = Mathf.Max(0.8f, 2f - 0.1f * difficultyScale);
                    break;

                case HumanVariant.Shieldman:
                    collider.radius = 0.40f;
                    collider.height = 1.7f;
                    collider.center = new Vector3(0f, 0.85f, 0f);
                    // Modest HP — the shield, not the health pool, is what
                    // makes these awkward, so flanking is rewarded rather
                    // than just out-damaging them.
                    baseHP = 20f + 1.5f * difficultyScale;
                    baseXP = 5f;
                    baseMoveSpeed = 1.35f + 0.18f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 22f + 1.5f * r;
                    // 60%, not more: EnemyAI keeps these turned toward the
                    // player, so the front arc is where hits normally land
                    // and a higher figure turns them into damage sponges
                    // rather than a positioning puzzle.
                    health.FrontalDamageReduction = 0.6f;
                    break;

                case HumanVariant.Bomber:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    // Fragile and fast-ish: the threat is where it dies, not
                    // how long it survives.
                    baseHP = 6f + 0.5f * difficultyScale;
                    baseXP = 5f;
                    baseMoveSpeed = 2.2f + 0.3f * Mathf.Min(difficultyScale, 4f);
                    // Touching one barely hurts — the blast is the whole
                    // threat, and it hits as hard as a Brute's fist.
                    baseContactDamage = 8f + 0.5f * r;
                    var bomb = go.AddComponent<ExplodeOnDeath>();
                    bomb.Damage = 40f + 2.6f * r;
                    break;

                case HumanVariant.Medic:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    baseHP = 14f + 1.0f * difficultyScale;
                    baseXP = 6f;
                    // Hangs back from the fight so it isn't trivially caught
                    // in the AoE aimed at the crowd it's healing.
                    baseMoveSpeed = 1.7f;
                    isRanged = true;
                    preferredRange = 8f;
                    baseProjectileInterval = 999f; // never throws; it shields
                    go.AddComponent<MedicTether>();
                    break;

                default: // Grunt
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    // One swipe in round 1. HP was last tuned against the 22-
                    // dmg slam back when that was the primary attack; now
                    // that LMB is the 10-dmg swipe, the old 16 HP meant two
                    // clicks per basic enemy from the very first wave. Sits
                    // just under a swipe so a level or damage buff keeps it
                    // a one-hit kill as the per-round growth creeps in.
                    baseHP = 9f + 0.9f * difficultyScale;
                    baseXP = 3f;
                    baseMoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
                    // The reference "weak enemy": about fifteen of these is
                    // a death, which is what every other figure is set from.
                    baseContactDamage = 13f + 0.9f * r;
                    break;
            }

            // Armor/Weapon/Shoes/Crown modifiers (see EnemyModifiers.cs) are
            // applied uniformly on top of the base per-variant numbers above.
            health.Init(maxHP: baseHP * modifiers.HPMultiplier, xpReward: baseXP * modifiers.XPMultiplier);
            health.SetHealthBarHeight(collider.center.y + collider.height / 2f + 0.35f);
            ai.MoveSpeed = baseMoveSpeed * modifiers.SpeedMultiplier;
            ai.ContactDamage = baseContactDamage * modifiers.DamageMultiplier;
            ai.IsRanged = isRanged;
            ai.PreferredRange = preferredRange;
            ai.ProjectileDamage = baseProjectileDamage * modifiers.DamageMultiplier;
            ai.ProjectileInterval = baseProjectileInterval;

            return go;
        }
    }
}
