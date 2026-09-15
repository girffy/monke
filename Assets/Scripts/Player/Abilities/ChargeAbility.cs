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
        CharacterAnimator _animator;
        Transform _armL, _armR;
        float _nextReadyTime;

        readonly HashSet<EnemyHealth> _hitThisCharge = new HashSet<EnemyHealth>();
        static readonly Collider[] HitBuffer = new Collider[48];

        public float CooldownRemaining01()
        {
            float total = Cooldown * _stats.AbilityCooldownMultiplier;
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _armL = model != null ? model.Find("ArmL") : null;
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void SetArmDirection(Vector3 localDirection)
        {
            var rot = Quaternion.FromToRotation(Vector3.down, localDirection);
            if (_armL != null) _armL.localRotation = rot;
            if (_armR != null) _armR.localRotation = rot;
        }

        void Update()
        {
            if (!Unlocked) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown * _stats.AbilityCooldownMultiplier;
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
            CameraShake.Shake(0.18f, 0.2f);

            // Drop into a low forward-leaning barge with the arms swept back.
            if (_animator != null)
            {
                _animator.SuppressArms = true;
                _animator.BodyPitch = 22f;
                _animator.BodyHeightOffset = -0.1f;
            }
            SetArmDirection(new Vector3(0f, -0.45f, -0.89f).normalized);

            float speed = Distance / Duration;
            float t = 0f;
            float nextDust = 0f;

            while (t < Duration)
            {
                t += Time.fixedDeltaTime;
                _rb.linearVelocity = dir * speed;
                if (model != null) model.rotation = lockedRotation;
                CheckHits();

                // Dust kicked up along the charge path.
                if (t >= nextDust)
                {
                    nextDust = t + 0.05f;
                    var puff = Blocky3DArt.SwipeDisc(new Color(0.66f, 0.60f, 0.48f));
                    puff.transform.position = transform.position + Vector3.up * 0.05f - dir * 0.4f;
                    puff.transform.localScale = new Vector3(0.25f, 0.02f, 0.25f);
                    puff.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(1.3f, 0.3f);
                }

                yield return new WaitForFixedUpdate();
            }

            _rb.linearVelocity = Vector3.zero;
            _controller.IsExternallyControlled = false;
            _controller.MovementLocked = false;

            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyPitch = 0f;
                _animator.BodyHeightOffset = 0f;
            }
            SetArmDirection(Vector3.down);
        }

        void CheckHits()
        {
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            Vector3 chargeDir = _rb.linearVelocity.sqrMagnitude > 0.0001f ? _rb.linearVelocity.normalized : transform.forward;
            int count = Physics.OverlapSphereNonAlloc(transform.position, HitRadius * _stats.AreaMultiplier, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null || _hitThisCharge.Contains(enemyHealth)) continue;

                _hitThisCharge.Add(enemyHealth);
                enemyHealth.TakeDamage(damage, chargeDir, 9f);
            }

            // Charging through a tree bowls it over in the direction you're
            // already travelling.
            for (int i = 0; i < count; i++)
            {
                var tree = HitBuffer[i].GetComponentInParent<GorillaSurvivors.Environment.FellableTree>();
                if (tree != null && !tree.IsFelled) tree.Fell(chargeDir, damage * 4f);
            }
        }
    }
}
