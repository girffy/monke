using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Pickups;

namespace GorillaSurvivors.Enemies
{
    public class EnemyHealth : MonoBehaviour
    {
        public float MaxHP = 20f;
        public float XPReward = 3f;
        [Range(0f, 1f)] public float PowerupDropChance = 0.06f;

        float _currentHP;
        bool _initialized;
        bool _dead;

        void Awake()
        {
            if (!_initialized) Init(MaxHP, XPReward);
        }

        // Call right after AddComponent when spawning from code, so currentHP
        // reflects the real MaxHP rather than the field's default value.
        public void Init(float maxHP, float xpReward)
        {
            MaxHP = maxHP;
            XPReward = xpReward;
            _currentHP = MaxHP;
            _initialized = true;
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null, 0f);
        }

        // Knockback direction/force are optional — callers that represent a
        // directional hit (the ground-slam, Charge) pass them so a surviving
        // enemy gets shoved back; omnidirectional damage (contact, Roar
        // already does its own knockback, rock explosions) can skip it.
        public void TakeDamage(float amount, Vector3? knockbackDirection, float knockbackForce)
        {
            if (_dead) return;

            _currentHP -= amount;
            if (_currentHP <= 0f)
            {
                Die();
                return;
            }

            Sfx.EnemyHit(transform.position);

            if (knockbackDirection.HasValue && knockbackForce > 0f)
            {
                GetComponent<EnemyAI>()?.ApplyKnockback(knockbackDirection.Value.normalized * knockbackForce, 0.25f);
            }
        }

        void Die()
        {
            // A lethal hit and, in the same frame, another source (e.g. a
            // rock explosion overlapping the slam's own hit) could both
            // call TakeDamage before Destroy(gameObject) actually takes
            // effect at end of frame — without this guard that meant a
            // double Die() call, double XP/loot, and EnemySpawner's alive
            // count getting decremented twice for one real enemy.
            if (_dead) return;
            _dead = true;

            XPOrb.Spawn(transform.position, XPReward);

            if (Random.value < PowerupDropChance)
            {
                PowerupPickup.SpawnRandom(transform.position);
            }

            Sfx.EnemyDeath(transform.position);
            EnemySpawner.Instance?.NotifyEnemyDied();
            Destroy(gameObject);
        }
    }
}
