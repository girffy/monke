using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player.Abilities
{
    // Unlockable AoE ability: press K to roar, damaging and knocking back
    // every enemy in a radius around the player. Doesn't lock movement —
    // meant to feel like a quick "get off me" panic button, distinct from
    // the committed ground-slam basic attack.
    public class RoarAbility : MonoBehaviour
    {
        public bool Unlocked;
        public float Cooldown = 6f;
        public float Radius = 4.5f;
        public float BaseDamage = 14f;
        public float KnockbackForce = 10f;
        public float KnockbackDuration = 0.35f;

        PlayerStats _stats;
        Transform _head;
        float _nextReadyTime;

        static readonly Collider[] HitBuffer = new Collider[48];

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            var model = transform.Find("GorillaModel");
            _head = model != null ? model.Find("Head") : null;
        }

        void Update()
        {
            if (!Unlocked) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown;
            StartCoroutine(RoarSequence());
        }

        bool WasPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonNorth.wasPressedThisFrame) return true;

            return false;
        }

        IEnumerator RoarSequence()
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) continue;

                enemyHealth.TakeDamage(damage);

                var enemyAI = HitBuffer[i].GetComponentInParent<EnemyAI>();
                if (enemyAI != null)
                {
                    Vector3 away = enemyAI.transform.position - transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitSphere;
                    enemyAI.ApplyKnockback(away.normalized * KnockbackForce, KnockbackDuration);
                }
            }

            SpawnRoarEffect();
            Sfx.Roar(transform.position);
            yield return AnimateHeadPulse();
        }

        void SpawnRoarEffect()
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 0.95f, 0.6f));
            go.transform.position = transform.position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            StartCoroutine(AnimateRing(go));
        }

        IEnumerator AnimateRing(GameObject go)
        {
            float duration = 0.25f;
            float t = 0f;
            float targetScale = Radius * 1.9f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.1f, targetScale, t / duration);
                go.transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }

            Destroy(go);
        }

        IEnumerator AnimateHeadPulse()
        {
            if (_head == null) yield break;

            var baseScale = _head.localScale;
            float duration = 0.2f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                float pulse = 1f + 0.25f * Mathf.Sin(p * Mathf.PI);
                _head.localScale = baseScale * pulse;
                yield return null;
            }

            _head.localScale = baseScale;
        }
    }
}
