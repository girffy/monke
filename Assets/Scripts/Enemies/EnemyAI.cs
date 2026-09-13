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

        Rigidbody _rb;
        Transform _model;
        Transform _target;
        PlayerHealth _targetHealth;
        float _nextContactDamageTime;

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

        void FixedUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            _rb.linearVelocity = toTarget.normalized * MoveSpeed;

            if (_model != null && toTarget.sqrMagnitude > 0.0001f)
            {
                _model.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            }

            // Distance-based contact damage — two solid Rigidbody circles
            // pushing directly into each other tend to separate every physics
            // step, so OnCollisionStay fires unreliably; a range check is
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
