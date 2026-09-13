using UnityEngine;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        public float MoveSpeed = 2.2f;
        public float ContactDamage = 8f;
        public float ContactDamageInterval = 0.75f;
        public float ContactRange = 1.05f;

        Rigidbody2D _rb;
        Transform _target;
        PlayerHealth _targetHealth;
        float _nextContactDamageTime;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        void Start()
        {
            AcquireTarget();
        }

        void FixedUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                return;
            }

            Vector2 toTarget = (_target.position - transform.position);
            _rb.linearVelocity = toTarget.normalized * MoveSpeed;

            // Distance-based contact damage — two solid Rigidbody2D circles
            // pushing directly into each other tend to separate every physics
            // step, so OnCollisionStay2D fires unreliably; a range check is
            // simple and consistent with how pickups already detect the player.
            if (Time.time >= _nextContactDamageTime && toTarget.sqrMagnitude <= ContactRange * ContactRange)
            {
                _targetHealth.TakeDamage(ContactDamage);
                _nextContactDamageTime = Time.time + ContactDamageInterval;
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
