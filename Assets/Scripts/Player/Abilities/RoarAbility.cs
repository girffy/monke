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
        CharacterAnimator _animator;
        Transform _head;
        Transform _armL, _armR;
        float _nextReadyTime;

        static readonly Collider[] HitBuffer = new Collider[48];

        public float CooldownRemaining01()
        {
            float total = Cooldown * _stats.AbilityCooldownMultiplier;
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _head = model != null ? model.Find("Head") : null;
            _armL = model != null ? model.Find("ArmL") : null;
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (!Unlocked) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown * _stats.AbilityCooldownMultiplier;
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
            float radius = Radius * _stats.AreaMultiplier;

            // Rear back and throw the arms wide before the shout lands. It's
            // only ~7 frames, short enough to still work as a panic button,
            // but it gives the ability a readable anticipation beat.
            if (_animator != null) _animator.SuppressArms = true;
            yield return PoseArms(RestDir, ChestBeatDir, 0.12f, 0f, -16f);

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, HitBuffer);
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

            SpawnRoarEffect(radius);
            Sfx.Roar(transform.position);
            CameraShake.Shake(0.3f, 0.28f);

            // A second, faster ring behind the first sells the shockwave as
            // having force rather than being a single expanding outline.
            var inner = Blocky3DArt.SwipeDisc(new Color(1f, 1f, 0.85f));
            inner.transform.position = transform.position + Vector3.up * 0.06f;
            inner.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            inner.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(radius * 1.1f, 0.16f);

            StartCoroutine(AnimateHeadPulse());
            yield return PoseArms(ChestBeatDir, RestDir, 0.22f, -16f, 0f);

            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyPitch = 0f;
            }
        }

        // Arms flung up and out — the chest-beating pose that precedes the
        // shout.
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 ChestBeatDir = new Vector3(0.62f, 0.72f, 0.3f).normalized;

        IEnumerator PoseArms(Vector3 fromDir, Vector3 toDir, float duration, float fromPitch, float toPitch)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                var dir = Vector3.Slerp(fromDir, toDir, p);
                // Mirrored on X so both arms splay outward rather than
                // pointing the same way.
                if (_armL != null) _armL.localRotation = Quaternion.FromToRotation(Vector3.down, new Vector3(-dir.x, dir.y, dir.z));
                if (_armR != null) _armR.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
                if (_animator != null) _animator.BodyPitch = Mathf.Lerp(fromPitch, toPitch, p);
                yield return null;
            }
        }

        void SpawnRoarEffect(float radius)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 0.95f, 0.6f));
            go.transform.position = transform.position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            StartCoroutine(AnimateRing(go, radius));
        }

        IEnumerator AnimateRing(GameObject go, float radius)
        {
            float duration = 0.25f;
            float t = 0f;
            float targetScale = radius * 1.9f;

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
