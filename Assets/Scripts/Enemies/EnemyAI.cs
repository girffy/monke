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

        Rigidbody2D _rb;
        Transform _target;
        float _nextContactDamageTime;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        void Start()
        {
            if (PlayerController.Instance != null)
            {
                _target = PlayerController.Instance.transform;
            }
        }

        void FixedUpdate()
        {
            if (_target == null)
            {
                if (PlayerController.Instance != null) _target = PlayerController.Instance.transform;
                return;
            }

            Vector2 toTarget = (_target.position - transform.position);
            _rb.linearVelocity = toTarget.normalized * MoveSpeed;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        void TryDamage(Collider2D other)
        {
            if (Time.time < _nextContactDamageTime) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            health.TakeDamage(ContactDamage);
            _nextContactDamageTime = Time.time + ContactDamageInterval;
        }
    }
}
