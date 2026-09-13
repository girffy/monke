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
            go.layer = LayerMask.NameToLayer("Default");

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatureArt.Human();
            renderer.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            var health = go.AddComponent<EnemyHealth>();
            health.Init(maxHP: 15f + 8f * difficultyScale, xpReward: 3f);

            var ai = go.AddComponent<EnemyAI>();
            ai.MoveSpeed = 1.8f + 0.35f * Mathf.Min(difficultyScale, 4f);
            ai.ContactDamage = 6f + 2f * Mathf.Min(difficultyScale, 3f);

            return go;
        }
    }
}
