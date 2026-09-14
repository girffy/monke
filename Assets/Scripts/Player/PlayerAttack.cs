using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player
{
    // Active attack: press J/Enter (or click, or the gamepad attack button) to
    // ground-slam a zone in front of the gorilla. Plays a short, rooted
    // animation — no moving/dashing until it finishes.
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        public float BaseDamage = 22f;
        public float HitRadius = 1.4f;
        public float ForwardOffset = 1.3f;
        public float BaseCooldown = 0.4f;
        public const float SlamDuration = 0.5f;

        PlayerStats _stats;
        PlayerController _controller;
        Transform _armL;
        Transform _armR;
        float _nextAttackReadyTime;
        bool _isSlamming;

        static readonly Collider[] HitBuffer = new Collider[32];

        // Arm poses as local directions the hanging arm points in (relative to
        // the shoulder pivot, which itself faces the attack direction) —
        // rest hangs straight down; windup raises up-and-back; slam drives
        // forward-and-down into the ground.
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 WindupDir = new Vector3(0f, 0.55f, -0.85f).normalized;
        static readonly Vector3 SlamDir = new Vector3(0f, -0.65f, 0.9f).normalized;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            var model = transform.Find("GorillaModel");
            _armL = model != null ? model.Find("ArmL") : null;
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (_isSlamming) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextAttackReadyTime) return;
            if (!WasAttackPressed()) return;

            float cooldown = BaseCooldown / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier);
            _nextAttackReadyTime = Time.time + cooldown + SlamDuration;

            Vector3 aimDirection = _controller.FacingDirection;
            aimDirection.y = 0f;
            aimDirection.Normalize();

            StartCoroutine(SlamSequence(aimDirection));
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

        IEnumerator SlamSequence(Vector3 aimDirection)
        {
            _isSlamming = true;
            _controller.MovementLocked = true;

            var model = transform.Find("GorillaModel");
            if (model != null) model.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);

            // Windup (arms raise), then slam down — damage lands the instant
            // the arms hit the ground — then a slower return to rest.
            const float windup = 0.15f;
            const float slam = 0.1f;
            const float recover = SlamDuration - windup - slam;

            yield return AnimateArms(RestDir, WindupDir, windup);
            yield return AnimateArms(WindupDir, SlamDir, slam);

            PerformSlamHit(aimDirection);

            yield return AnimateArms(SlamDir, RestDir, recover);

            _isSlamming = false;
            _controller.MovementLocked = false;
        }

        IEnumerator AnimateArms(Vector3 fromDir, Vector3 toDir, float duration)
        {
            if (duration <= 0f) yield break;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetArmDirection(Vector3.Slerp(fromDir, toDir, t / duration));
                yield return null;
            }
            SetArmDirection(toDir);
        }

        void SetArmDirection(Vector3 localDirection)
        {
            var rot = Quaternion.FromToRotation(Vector3.down, localDirection);
            if (_armL != null) _armL.localRotation = rot;
            if (_armR != null) _armR.localRotation = rot;
        }

        void PerformSlamHit(Vector3 aimDirection)
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float radius = HitRadius * _stats.LevelAttackRadiusBonus;
            Vector3 hitCenter = transform.position + aimDirection * ForwardOffset;

            int count = Physics.OverlapSphereNonAlloc(hitCenter, radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null) enemyHealth.TakeDamage(damage);
            }

            SpawnSlamEffect(hitCenter, radius);
        }

        void SpawnSlamEffect(Vector3 position, float radius)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 1f, 1f));
            go.transform.position = position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);

            StartCoroutine(AnimateSlamEffect(go, radius));
        }

        IEnumerator AnimateSlamEffect(GameObject go, float radius)
        {
            float duration = 0.16f;
            float t = 0f;
            float targetScale = radius * 1.8f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.05f, targetScale, t / duration);
                go.transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }

            Destroy(go);
        }
    }
}
