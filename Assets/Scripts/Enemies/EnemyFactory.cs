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
            var ai = go.AddComponent<EnemyAI>();
            var health = go.AddComponent<EnemyHealth>();

            switch (type)
            {
                case HumanVariant.Runner:
                    collider.radius = 0.30f;
                    collider.height = 1.4f;
                    collider.center = new Vector3(0f, 0.7f, 0f);
                    health.Init(maxHP: 8f + 4f * difficultyScale, xpReward: 2f);
                    ai.MoveSpeed = 3.4f + 0.4f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 4f + 1f * Mathf.Min(difficultyScale, 3f);
                    break;

                case HumanVariant.Brute:
                    collider.radius = 0.55f;
                    collider.height = 2.4f;
                    collider.center = new Vector3(0f, 1.2f, 0f);
                    health.Init(maxHP: 45f + 16f * difficultyScale, xpReward: 7f);
                    ai.MoveSpeed = 1.1f + 0.2f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 13f + 3f * Mathf.Min(difficultyScale, 4f);
                    break;

                case HumanVariant.Thrower:
                    collider.radius = 0.35f;
                    collider.height = 1.6f;
                    collider.center = new Vector3(0f, 0.8f, 0f);
                    health.Init(maxHP: 10f + 5f * difficultyScale, xpReward: 4f);
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
                    health.Init(maxHP: 15f + 8f * difficultyScale, xpReward: 3f);
                    ai.MoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
                    ai.ContactDamage = 6f + 2f * Mathf.Min(difficultyScale, 3f);
                    break;
            }

            return go;
        }
    }
}
