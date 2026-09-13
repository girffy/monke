using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // Builds a fully-wired enemy GameObject from code — no prefab asset needed,
    // so difficulty/appearance tuning lives in one place.
    public static class EnemyFactory
    {
        public static GameObject Create(Vector3 position, float difficultyScale)
        {
            var go = new GameObject("Human");
            go.transform.position = position;

            var model = Blocky3DArt.Human();
            model.transform.SetParent(go.transform, false);

            go.AddComponent<Rigidbody>();

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.35f;
            collider.height = 1.6f;
            collider.center = new Vector3(0f, 0.8f, 0f);

            var health = go.AddComponent<EnemyHealth>();
            health.Init(maxHP: 15f + 8f * difficultyScale, xpReward: 3f);

            var ai = go.AddComponent<EnemyAI>();
            ai.MoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
            ai.ContactDamage = 6f + 2f * Mathf.Min(difficultyScale, 3f);

            return go;
        }
    }
}
