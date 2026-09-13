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

        static readonly Collider[] HitBuffer = new Collider[32];

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

            PerformSwipe(_controller != null ? _controller.FacingDirection : Vector3.forward);
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

        void PerformSwipe(Vector3 aimDirection)
        {
            aimDirection.y = 0f;
            aimDirection.Normalize();

            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float range = BaseRange * _stats.LevelAttackRadiusBonus;
            float cosHalfArc = Mathf.Cos(ArcDegrees * 0.5f * Mathf.Deg2Rad);

            int count = Physics.OverlapSphereNonAlloc(transform.position, range, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) continue;

                Vector3 toEnemy = enemyHealth.transform.position - transform.position;
                toEnemy.y = 0f;

                if (toEnemy.sqrMagnitude < 0.0001f || Vector3.Dot(toEnemy.normalized, aimDirection) >= cosHalfArc)
                {
                    enemyHealth.TakeDamage(damage);
                }
            }

            SpawnSwipeEffect(aimDirection, range);
        }

        void SpawnSwipeEffect(Vector3 aimDirection, float range)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 1f, 1f));
            go.transform.position = transform.position + aimDirection * (range * 0.4f) + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);

            var renderer = go.GetComponent<MeshRenderer>();
            StartCoroutine(AnimateSwipe(go, renderer, range));
        }

        IEnumerator AnimateSwipe(GameObject go, MeshRenderer renderer, float range)
        {
            float duration = 0.14f;
            float t = 0f;
            float targetScale = range * 0.9f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                float scale = Mathf.Lerp(0.05f, targetScale, p);
                go.transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }

            Destroy(go);
        }
    }
}
