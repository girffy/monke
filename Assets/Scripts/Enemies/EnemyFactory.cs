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

            float baseHP, baseXP, baseMoveSpeed;
            float baseContactDamage = 0f, baseProjectileDamage = 0f, baseProjectileInterval = 0f, preferredRange = 0f;
            bool isRanged = false;

            switch (type)
            {
                case HumanVariant.Runner:
                    collider.radius = 0.30f;
                    collider.height = 1.4f;
                    collider.center = new Vector3(0f, 0.7f, 0f);
                    // Stays one-shot-able by the base attack (22 dmg) for a
                    // very long time — it's meant to be a fast, fragile
                    // pest, not a damage check.
                    baseHP = 12f + 1.0f * difficultyScale;
                    baseXP = 2f;
                    baseMoveSpeed = 3.4f + 0.4f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 4f + 1f * Mathf.Min(difficultyScale, 3f);
                    break;

                case HumanVariant.Brute:
                    collider.radius = 0.55f;
                    collider.height = 2.4f;
                    collider.center = new Vector3(0f, 1.2f, 0f);
                    // The "big guy": always takes 2-3 unbuffed base-attack
                    // hits (base attack = 22 dmg), even in round 1, and
                    // gets tankier every round so damage upgrades stay
                    // meaningful against it long after Grunts stop mattering.
                    baseHP = 50f + 4f * difficultyScale;
                    baseXP = 7f;
                    baseMoveSpeed = 1.1f + 0.2f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 13f + 3f * Mathf.Min(difficultyScale, 4f);
                    break;

                case HumanVariant.Thrower:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    baseHP = 18f + 1.2f * difficultyScale;
                    baseXP = 4f;
                    baseMoveSpeed = 1.6f;
                    isRanged = true;
                    preferredRange = 5f;
                    baseProjectileDamage = 5f + 1f * Mathf.Min(difficultyScale, 4f);
                    baseProjectileInterval = Mathf.Max(0.8f, 2f - 0.1f * difficultyScale);
                    break;

                case HumanVariant.Shieldman:
                    collider.radius = 0.40f;
                    collider.height = 1.7f;
                    collider.center = new Vector3(0f, 0.85f, 0f);
                    // Modest HP — the shield, not the health pool, is what
                    // makes these awkward, so flanking is rewarded rather
                    // than just out-damaging them.
                    baseHP = 26f + 2.0f * difficultyScale;
                    baseXP = 5f;
                    baseMoveSpeed = 1.35f + 0.18f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 8f + 2f * Mathf.Min(difficultyScale, 3f);
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
                    baseHP = 10f + 0.8f * difficultyScale;
                    baseXP = 5f;
                    baseMoveSpeed = 2.2f + 0.3f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 4f;
                    var bomb = go.AddComponent<ExplodeOnDeath>();
                    bomb.Damage = 26f + 2.5f * Mathf.Min(difficultyScale, 8f);
                    break;

                case HumanVariant.Medic:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    baseHP = 20f + 1.4f * difficultyScale;
                    baseXP = 6f;
                    // Hangs back from the fight so it isn't trivially caught
                    // in the AoE aimed at the crowd it's healing.
                    baseMoveSpeed = 1.7f;
                    isRanged = true;
                    preferredRange = 8f;
                    baseProjectileInterval = 999f; // never throws; it heals
                    var healer = go.AddComponent<HealerAura>();
                    healer.HealAmount = 8f + 0.8f * Mathf.Min(difficultyScale, 10f);
                    break;

                default: // Grunt
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    // Meant to stay a reliable one-shot against the base
                    // 22-dmg attack for the first several rounds, then
                    // gradually creep past it so late-game runs without any
                    // damage round-reward/powerup start needing 2 hits.
                    baseHP = 16f + 1.2f * difficultyScale;
                    baseXP = 3f;
                    baseMoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
                    baseContactDamage = 6f + 2f * Mathf.Min(difficultyScale, 3f);
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
