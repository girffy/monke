using UnityEngine;
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
            _currentHP -= amount;
            if (_currentHP <= 0f) Die();
        }

        void Die()
        {
            XPOrb.Spawn(transform.position, XPReward);

            if (Random.value < PowerupDropChance)
            {
                PowerupPickup.SpawnRandom(transform.position);
            }

            Destroy(gameObject);
        }
    }
}
