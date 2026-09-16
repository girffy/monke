using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        public float MoveSpeed = 2.2f;
        public float ContactDamage = 8f;
        public float ContactDamageInterval = 0.75f;
        public float ContactRange = 1.05f;

        [Header("Ranged (Thrower)")]
        public bool IsRanged;
        public float PreferredRange = 5f;
        public float ProjectileDamage = 6f;
        public float ProjectileInterval = 2f;
        float _nextProjectileTime;

        Rigidbody _rb;
        Transform _model;
        Transform _target;
        PlayerHealth _targetHealth;
        float _nextContactDamageTime;

        float _knockbackUntil;
        Vector3 _knockbackVelocity;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            _model = transform.Find("HumanModel");
        }

        void Start()
        {
            AcquireTarget();
        }

        public void ApplyKnockback(Vector3 velocity, float duration)
        {
            _knockbackVelocity = velocity;
            _knockbackUntil = Time.time + duration;
        }

        // Tech tree: the slam's "Concussive". A stunned enemy stops dead —
        // no walking, no contact damage, no throwing — which is what makes
        // the slow, committed heavy attack worth its recovery time.
        float _stunnedUntil;
        public bool IsStunned => Time.time < _stunnedUntil;
        public void ApplyStun(float seconds)
        {
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
        }

        // Multiplicative slow applied while standing in something nasty (the
        // dung patch). Stored as a plain factor rather than a timer, because
        // whatever applied it is responsible for clearing it when the enemy
        // leaves — that keeps overlapping patches from fighting over it.
        public void ApplySlow(float factor) => _slowFactor = Mathf.Clamp01(factor);
        public void ClearSlow() => _slowFactor = 1f;

        float _slowFactor = 1f;
        float CurrentMoveSpeed => MoveSpeed * _slowFactor;

        void FixedUpdate()
        {
            // Without this, enemies kept moving, dealing contact damage,
            // and lobbing projectiles during the round-clear/upgrade-choice
            // screen (and after game over) even though everything else —
            // spawning, player attacks/input — correctly stopped.
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                _rb.linearVelocity = Vector3.zero;
                return;
            }

            if (_target == null)
            {
                AcquireTarget();
                return;
            }

            if (IsStunned)
            {
                _rb.linearVelocity = Vector3.zero;
                ConfineToArena();
                return;
            }

            if (Time.time < _knockbackUntil)
            {
                _rb.linearVelocity = _knockbackVelocity;
                // Knockback is the likeliest thing to put an enemy through
                // the wall, so it has to be clamped too, not just walking.
                ConfineToArena();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            Vector3 dir = dist > 0.0001f ? toTarget / dist : Vector3.zero;

            if (IsRanged)
            {
                // Hold at range and lob projectiles instead of closing in.
                if (dist > PreferredRange + 0.5f)
                {
                    _rb.linearVelocity = dir * CurrentMoveSpeed;
                }
                else if (dist < PreferredRange - 0.5f)
                {
                    _rb.linearVelocity = -dir * CurrentMoveSpeed;
                }
                else
                {
                    _rb.linearVelocity = Vector3.zero;
                }

                // ProjectileDamage of 0 means "ranged positioning, no attack"
                // — the Medic keeps its distance to stay out of the AoE
                // aimed at the crowd it's healing, but never throws.
                if (ProjectileDamage > 0f && Time.time >= _nextProjectileTime && dist <= PreferredRange * 1.5f)
                {
                    Projectile.Spawn(transform.position + Vector3.up * 0.8f, dir, ProjectileDamage);
                    _nextProjectileTime = Time.time + ProjectileInterval;
                }
            }
            else
            {
                _rb.linearVelocity = dir * CurrentMoveSpeed;

                // Distance-based contact damage — two solid Rigidbody circles
                // pushing directly into each other tend to separate every physics
                // step, so OnCollisionStay fires unreliably; a range check is
                // simple and consistent with how pickups already detect the player.
                if (Time.time >= _nextContactDamageTime && dist <= ContactRange)
                {
                    _targetHealth.TakeDamage(ContactDamage, transform.position);
                    _nextContactDamageTime = Time.time + ContactDamageInterval;
                }
            }

            if (_model != null && dir.sqrMagnitude > 0.0001f)
            {
                _model.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            ConfineToArena();
        }

        // Same position clamp the player is held by (PlayerController). The
        // wall has no colliders, so without this enemies walked straight out
        // through it — most visibly Throwers and Medics, which back away from
        // the player and so reverse into the wall on purpose.
        void ConfineToArena()
        {
            var arena = Environment.Arena.Instance;
            if (arena == null) return;

            // The ring wall holds them in; the central well keeps them out of
            // the middle, the same as it does the player.
            Vector3 inside = arena.ClampInside(_rb.position, 0.5f);
            if (inside != _rb.position)
            {
                _rb.position = inside;
                StripRadial(inside - arena.Center, outward: true);
            }

            Vector3 outside = arena.ClampOutsideCore(_rb.position, 0.5f);
            if (outside != _rb.position)
            {
                _rb.position = outside;
                StripRadial(outside - arena.Center, outward: false);
            }
        }

        void StripRadial(Vector3 radial, bool outward)
        {
            radial.y = 0f;
            if (radial.sqrMagnitude < 0.0001f) return;

            radial.Normalize();
            Vector3 v = _rb.linearVelocity;
            float into = Vector3.Dot(v, radial);
            if (outward ? into > 0f : into < 0f) _rb.linearVelocity = v - radial * into;
        }

        void AcquireTarget()
        {
            if (PlayerController.Instance == null) return;
            _target = PlayerController.Instance.transform;
            _targetHealth = _target.GetComponent<PlayerHealth>();
        }
    }
}
