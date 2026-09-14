using UnityEngine;
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

        void FixedUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                return;
            }

            if (Time.time < _knockbackUntil)
            {
                _rb.linearVelocity = _knockbackVelocity;
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
                    _rb.linearVelocity = dir * MoveSpeed;
                }
                else if (dist < PreferredRange - 0.5f)
                {
                    _rb.linearVelocity = -dir * MoveSpeed;
                }
                else
                {
                    _rb.linearVelocity = Vector3.zero;
                }

                if (Time.time >= _nextProjectileTime && dist <= PreferredRange * 1.5f)
                {
                    Projectile.Spawn(transform.position + Vector3.up * 0.8f, dir, ProjectileDamage);
                    _nextProjectileTime = Time.time + ProjectileInterval;
                }
            }
            else
            {
                _rb.linearVelocity = dir * MoveSpeed;

                // Distance-based contact damage — two solid Rigidbody circles
                // pushing directly into each other tend to separate every physics
                // step, so OnCollisionStay fires unreliably; a range check is
                // simple and consistent with how pickups already detect the player.
                if (Time.time >= _nextContactDamageTime && dist <= ContactRange)
                {
                    _targetHealth.TakeDamage(ContactDamage);
                    _nextContactDamageTime = Time.time + ContactDamageInterval;
                }
            }

            if (_model != null && dir.sqrMagnitude > 0.0001f)
            {
                _model.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        void AcquireTarget()
        {
            if (PlayerController.Instance == null) return;
            _target = PlayerController.Instance.transform;
            _targetHealth = _target.GetComponent<PlayerHealth>();
        }
    }
}
