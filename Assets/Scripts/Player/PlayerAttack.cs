using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player
{
    // Basic attack: a periodic ground-pound AoE centered on the gorilla. No aiming
    // needed — everything within range gets hit, Vampire-Survivors style.
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerAttack : MonoBehaviour
    {
        public float BaseDamage = 12f;
        public float BaseRadius = 1.6f;
        public float BaseInterval = 0.9f;

        PlayerStats _stats;
        float _nextAttackTime;

        static readonly Collider2D[] HitBuffer = new Collider2D[32];

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            float interval = BaseInterval / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier);
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + interval;
            PerformSmash();
        }

        void PerformSmash()
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float radius = BaseRadius * _stats.LevelAttackRadiusBonus;

            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                }
            }

            SpawnSmashEffect(radius);
        }

        void SpawnSmashEffect(float radius)
        {
            var go = new GameObject("SmashEffect");
            go.transform.position = transform.position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprites.Circle(new Color(1f, 1f, 1f, 0.6f), 64);
            renderer.sortingOrder = 2;
            go.transform.localScale = Vector3.one * 0.05f;

            StartCoroutine(AnimateSmash(go, radius));
        }

        IEnumerator AnimateSmash(GameObject go, float radius)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            float duration = 0.22f;
            float t = 0f;
            float targetScale = radius * 2f; // sprite's base diameter is 1 world unit at scale 1

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                float scale = Mathf.Lerp(0.05f, targetScale, p);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var c = renderer.color;
                c.a = Mathf.Lerp(0.55f, 0f, p);
                renderer.color = c;

                yield return null;
            }

            Destroy(go);
        }
    }
}
