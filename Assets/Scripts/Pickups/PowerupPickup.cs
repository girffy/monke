using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Pickups
{
    public enum PowerupType
    {
        Banana,     // instant heal
        Adrenaline, // temporary move speed + attack speed
        Rampage     // temporary damage multiplier
    }

    public class PowerupPickup : MonoBehaviour
    {
        public PowerupType Type;
        public float PickupRadius = 0.45f;

        Transform _player;

        static readonly Color[] Colors =
        {
            new Color(0.95f, 0.85f, 0.2f),  // Banana - yellow
            new Color(0.3f, 0.85f, 0.95f),  // Adrenaline - cyan
            new Color(0.85f, 0.2f, 0.85f),  // Rampage - magenta
        };

        public static PowerupPickup SpawnRandom(Vector3 position)
        {
            var type = (PowerupType)Random.Range(0, System.Enum.GetValues(typeof(PowerupType)).Length);
            return Spawn(position, type);
        }

        public static PowerupPickup Spawn(Vector3 position, PowerupType type)
        {
            var go = new GameObject($"Powerup_{type}");
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprites.Circle(Colors[(int)type], 20);
            renderer.sortingOrder = 4;

            var pickup = go.AddComponent<PowerupPickup>();
            pickup.Type = type;
            return pickup;
        }

        void Start()
        {
            if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
        }

        void Update()
        {
            if (_player == null)
            {
                if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
                return;
            }

            if (Vector2.Distance(transform.position, _player.position) <= PickupRadius)
            {
                Apply(_player);
                Destroy(gameObject);
            }
        }

        void Apply(Transform player)
        {
            var health = player.GetComponent<PlayerHealth>();
            var stats = player.GetComponent<PlayerStats>();

            switch (Type)
            {
                case PowerupType.Banana:
                    health?.Heal(30f);
                    break;
                case PowerupType.Adrenaline:
                    stats?.ApplyTemporaryMoveSpeedBuff(0.5f, 8f);
                    stats?.ApplyTemporaryAttackSpeedBuff(0.6f, 8f);
                    break;
                case PowerupType.Rampage:
                    stats?.ApplyTemporaryDamageBuff(1f, 8f);
                    break;
            }
        }
    }
}
