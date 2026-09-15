using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // Builds a fully-wired enemy GameObject from code — no prefab asset needed,
    // so difficulty/appearance tuning lives in one place.
    public static class EnemyFactory
    {
        public static GameObject Create(HumanVariant type, Vector3 position, float difficultyScale)
        {
            var go = new GameObject(type.ToString());
            go.transform.position = position;

            var model = Blocky3DArt.Human(type);
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

            switch (type)
            {
                case HumanVariant.Runner:
                    collider.radius = 0.30f;
                    collider.height = 1.4f;
                    collider.center = new Vector3(0f, 0.7f, 0f);
                    // Stays one-shot-able by the base attack (22 dmg) for a
                    // very long time — it's meant to be a fast, fragile
                    // pest, not a damage check.
                    health.Init(maxHP: 12f + 1.0f * difficultyScale, xpReward: 2f);
                    ai.MoveSpeed = 3.4f + 0.4f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 4f + 1f * Mathf.Min(difficultyScale, 3f);
                    break;

                case HumanVariant.Brute:
                    collider.radius = 0.55f;
                    collider.height = 2.4f;
                    collider.center = new Vector3(0f, 1.2f, 0f);
                    // The "big guy": always takes 2-3 unbuffed base-attack
                    // hits (base attack = 22 dmg), even in round 1, and
                    // gets tankier every round so damage upgrades stay
                    // meaningful against it long after Grunts stop mattering.
                    health.Init(maxHP: 50f + 4f * difficultyScale, xpReward: 7f);
                    ai.MoveSpeed = 1.1f + 0.2f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 13f + 3f * Mathf.Min(difficultyScale, 4f);
                    break;

                case HumanVariant.Thrower:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    health.Init(maxHP: 18f + 1.2f * difficultyScale, xpReward: 4f);
                    ai.MoveSpeed = 1.6f;
                    ai.IsRanged = true;
                    ai.PreferredRange = 5f;
                    ai.ProjectileDamage = 5f + 1f * Mathf.Min(difficultyScale, 4f);
                    ai.ProjectileInterval = Mathf.Max(0.8f, 2f - 0.1f * difficultyScale);
                    break;

                default: // Grunt
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    // Meant to stay a reliable one-shot against the base
                    // 22-dmg attack for the first several rounds, then
                    // gradually creep past it so late-game runs without any
                    // damage round-reward/powerup start needing 2 hits.
                    health.Init(maxHP: 16f + 1.2f * difficultyScale, xpReward: 3f);
                    ai.MoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 6f + 2f * Mathf.Min(difficultyScale, 3f);
                    break;
            }

            return go;
        }
    }
}
