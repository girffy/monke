using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player.Abilities
{
    // Unlockable ranged attack: press E to hurl a clod of dung at whatever
    // you're aiming at. It's the only way the gorilla reaches something it
    // isn't standing next to, and the patch it leaves behind slows anything
    // that crosses it — so it doubles as a way to shape where the crowd goes.
    //
    // Aim comes from the usual mouse/right-stick direction, with a light
    // snap onto a nearby enemy so a lobbed throw at a moving target doesn't
    // feel unfairly fiddly.
    public class DungTossAbility : MonoBehaviour
    {
        public bool Unlocked;
        public float Cooldown = 4.5f;
        public float BaseDamage = 14f;
        public float Range = 11f;
        public float ImpactRadius = 1.4f;
        public float AimAssistAngle = 18f;

        const float WindupTime = 0.16f;
        const float RecoverTime = 0.16f;

        PlayerStats _stats;
        PlayerController _controller;
        CharacterAnimator _animator;
        Transform _armR;

        float _nextReadyTime;
        bool _isThrowing;

        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 WindupDir = new Vector3(0.35f, 0.62f, -0.70f).normalized;
        static readonly Vector3 ReleaseDir = new Vector3(0.25f, 0.30f, 0.92f).normalized;

        public float CooldownRemaining01()
        {
            float total = Cooldown * _stats.AbilityCooldownMultiplier;
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (!Unlocked || _isThrowing) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown * _stats.AbilityCooldownMultiplier;
            StartCoroutine(ThrowSequence());
        }

        bool WasPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonEast.wasPressedThisFrame) return true;

            return false;
        }

        IEnumerator ThrowSequence()
        {
            _isThrowing = true;
            if (_animator != null) _animator.SuppressArms = true;

            Vector3 aim = _controller.GetAimDirection();
            aim.y = 0f;
            aim.Normalize();

            // Throwing doesn't root the player — it's a quick over-the-
            // shoulder lob, so it stays usable while backpedalling.
            yield return AnimateArm(RestDir, WindupDir, WindupTime, 0f, -10f);

            Vector3 target = ResolveTarget(aim);
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float radius = ImpactRadius * _stats.AreaMultiplier;
            Vector3 origin = transform.position + Vector3.up * 1.3f + aim * 0.4f;

            DungProjectile.Launch(origin, target, damage, radius);
            CameraShake.Shake(0.07f, 0.1f);

            yield return AnimateArm(WindupDir, ReleaseDir, 0.07f, -10f, 12f);
            yield return AnimateArm(ReleaseDir, RestDir, RecoverTime, 12f, 0f);

            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyPitch = 0f;
            }
            _isThrowing = false;
        }

        // Snap to the enemy closest to the aim line within a narrow cone;
        // otherwise just throw the full distance along the aim.
        Vector3 ResolveTarget(Vector3 aim)
        {
            Vector3 fallback = transform.position + aim * Range;

            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth best = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in enemies)
            {
                Vector3 toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0f;
                float distance = toEnemy.magnitude;
                if (distance > Range || distance < 0.01f) continue;

                float angle = Vector3.Angle(aim, toEnemy);
                if (angle > AimAssistAngle) continue;

                // Prefer the nearest target inside the cone, nudged by how
                // centred it is, so the throw favours what you're looking at.
                float score = distance + angle * 0.08f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            if (best == null) return fallback;

            var hit = best.transform.position;
            hit.y = 0f;
            return hit;
        }

        IEnumerator AnimateArm(Vector3 fromDir, Vector3 toDir, float duration, float fromPitch, float toPitch)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                if (_armR != null) _armR.localRotation = Quaternion.FromToRotation(Vector3.down, Vector3.Slerp(fromDir, toDir, p));
                if (_animator != null) _animator.BodyPitch = Mathf.Lerp(fromPitch, toPitch, p);
                yield return null;
            }
            if (_armR != null) _armR.localRotation = Quaternion.FromToRotation(Vector3.down, toDir);
        }
    }
}
