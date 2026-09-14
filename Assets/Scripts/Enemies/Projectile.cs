using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // Simple thrown rock: travels in a straight line, damages the player on
    // proximity (same distance-check convention as pickups/contact damage),
    // and expires after a short lifetime so misses don't linger forever.
    public class Projectile : MonoBehaviour
    {
        public float Speed = 8f;
        public float Damage = 6f;
        public float Lifetime = 3f;
        public float HitRadius = 0.4f;

        Vector3 _direction;
        float _spawnTime;

        public static Projectile Spawn(Vector3 position, Vector3 direction, float damage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(new Color(0.4f, 0.3f, 0.2f));

            var proj = go.AddComponent<Projectile>();
            proj._direction = direction.normalized;
            proj.Damage = damage;
            return proj;
        }

        void Awake()
        {
            _spawnTime = Time.time;
        }

        void Update()
        {
            transform.position += _direction * Speed * Time.deltaTime;

            if (Time.time - _spawnTime >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var player = PlayerController.Instance;
            if (player == null) return;

            if (Vector3.Distance(transform.position, player.transform.position) <= HitRadius)
            {
                player.GetComponent<PlayerHealth>()?.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
    }
}
