using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player
{
    // Active attack: press J/Enter (or click, or the gamepad attack button) to
    // swing at whatever's in a cone in the direction the gorilla is facing.
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerAttack : MonoBehaviour
    {
        public float BaseDamage = 22f;
        public float BaseRange = 1.9f;
        public float ArcDegrees = 80f;
        public float BaseCooldown = 0.4f;

        PlayerStats _stats;
        PlayerController _controller;
        float _nextAttackReadyTime;

        static readonly Collider2D[] HitBuffer = new Collider2D[32];

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (Time.time < _nextAttackReadyTime) return;
            if (!WasAttackPressed()) return;

            float cooldown = BaseCooldown / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier);
            _nextAttackReadyTime = Time.time + cooldown;

            PerformSwipe(_controller != null ? _controller.FacingDirection : Vector2.down);
        }

        bool WasAttackPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.jKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonWest.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame)) return true;

            return false;
        }

        void PerformSwipe(Vector2 aimDirection)
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float range = BaseRange * _stats.LevelAttackRadiusBonus;
            float cosHalfArc = Mathf.Cos(ArcDegrees * 0.5f * Mathf.Deg2Rad);

            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(transform.position, range, filter, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) continue;

                Vector2 toEnemy = (Vector2)enemyHealth.transform.position - (Vector2)transform.position;
                if (toEnemy.sqrMagnitude < 0.0001f || Vector2.Dot(toEnemy.normalized, aimDirection) >= cosHalfArc)
                {
                    enemyHealth.TakeDamage(damage);
                }
            }

            SpawnSwipeEffect(aimDirection, range);
        }

        void SpawnSwipeEffect(Vector2 aimDirection, float range)
        {
            var go = new GameObject("SwipeEffect");
            go.transform.position = transform.position;
            go.transform.up = aimDirection;
            go.transform.localScale = Vector3.one * (range / 0.88f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatureArt.SwipeWedge(64, ArcDegrees);
            renderer.sortingOrder = 11;

            StartCoroutine(FadeAndDestroy(go, renderer));
        }

        IEnumerator FadeAndDestroy(GameObject go, SpriteRenderer renderer)
        {
            float duration = 0.14f;
            float t = 0f;
            var baseColor = renderer.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                var c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, t / duration);
                renderer.color = c;
                yield return null;
            }

            Destroy(go);
        }
    }
}
