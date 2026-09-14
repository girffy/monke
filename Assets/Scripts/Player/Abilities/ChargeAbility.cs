using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player.Abilities
{
    // Unlockable dash-attack: press L to charge forward further/faster than
    // a normal dash, damaging every enemy along the path once. Grants iframes
    // for the duration, same reasoning as the regular dash.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerHealth))]
    public class ChargeAbility : MonoBehaviour
    {
        public bool Unlocked;
        public float Cooldown = 5f;
        public float Distance = 6f;
        public float Duration = 0.25f;
        public float BaseDamage = 18f;
        public float HitRadius = 0.9f;

        Rigidbody _rb;
        PlayerStats _stats;
        PlayerController _controller;
        PlayerHealth _health;
        float _nextReadyTime;

        readonly HashSet<EnemyHealth> _hitThisCharge = new HashSet<EnemyHealth>();
        static readonly Collider[] HitBuffer = new Collider[48];

        public float CooldownRemaining01()
        {
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return Cooldown <= 0f ? 0f : Mathf.Clamp01(remaining / Cooldown);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (!Unlocked) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown;
            StartCoroutine(ChargeSequence());
        }

        bool WasPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonEast.wasPressedThisFrame) return true;

            return false;
        }

        IEnumerator ChargeSequence()
        {
            _hitThisCharge.Clear();
            _controller.MovementLocked = true;
            _controller.IsExternallyControlled = true;
            _health.GrantInvulnerability(Duration + 0.1f);

            Vector3 dir = _controller.GetAimDirection();
            dir.y = 0f;
            dir.Normalize();

            var model = transform.Find("GorillaModel");
            var lockedRotation = Quaternion.LookRotation(dir, Vector3.up);
            if (model != null) model.rotation = lockedRotation;

            Sfx.Charge(transform.position);

            float speed = Distance / Duration;
            float t = 0f;

            while (t < Duration)
            {
                t += Time.fixedDeltaTime;
                _rb.linearVelocity = dir * speed;
                if (model != null) model.rotation = lockedRotation;
                CheckHits();
                yield return new WaitForFixedUpdate();
            }

            _rb.linearVelocity = Vector3.zero;
            _controller.IsExternallyControlled = false;
            _controller.MovementLocked = false;
        }

        void CheckHits()
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            Vector3 chargeDir = _rb.linearVelocity.sqrMagnitude > 0.0001f ? _rb.linearVelocity.normalized : transform.forward;
            int count = Physics.OverlapSphereNonAlloc(transform.position, HitRadius, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null || _hitThisCharge.Contains(enemyHealth)) continue;

                _hitThisCharge.Add(enemyHealth);
                enemyHealth.TakeDamage(damage, chargeDir, 9f);
            }
        }
    }
}
