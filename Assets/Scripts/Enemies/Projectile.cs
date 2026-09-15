using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // Simple thrown rock: travels in a straight line, damages the player on
    // proximity (same distance-check convention as pickups/contact damage),
    // and expires after a short lifetime so misses don't linger forever. Has
    // no Collider of its own (see Spawn) so the player's attacks can't find
    // it via Physics.OverlapSphere — they instead check the Active registry
    // directly and call Deflect on anything in range.
    public class Projectile : MonoBehaviour
    {
        public float Speed = 8f;
        public float Damage = 6f;
        public float Lifetime = 3f;
        public float HitRadius = 0.4f;

        public static readonly List<Projectile> Active = new List<Projectile>();

        Vector3 _direction;
        float _spawnTime;
        bool _isEnemyOwned = true;

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
            Sfx.Throw(position);
            return proj;
        }

        void Awake()
        {
            _spawnTime = Time.time;
            Active.Add(this);
        }

        void OnDestroy()
        {
            Active.Remove(this);
        }

        void Update()
        {
            transform.position += _direction * Speed * Time.deltaTime;

            if (Time.time - _spawnTime >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (_isEnemyOwned)
            {
                var player = PlayerController.Instance;
                if (player == null) return;

                if (Vector3.Distance(transform.position, player.transform.position) <= HitRadius)
                {
                    player.GetComponent<PlayerHealth>()?.TakeDamage(Damage);
                    Destroy(gameObject);
                }
            }
            else
            {
                int count = Physics.OverlapSphereNonAlloc(transform.position, HitRadius, DeflectHitBuffer);
                for (int i = 0; i < count; i++)
                {
                    var enemyHealth = DeflectHitBuffer[i].GetComponentInParent<EnemyHealth>();
                    if (enemyHealth == null) continue;

                    enemyHealth.TakeDamage(Damage, _direction, 5f);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        static readonly Collider[] DeflectHitBuffer = new Collider[8];

        // Called by the player's attacks when they land on this projectile
        // mid-flight: reverses it to fly toward whatever the attack was
        // aimed at and re-flags it to hurt enemies instead of the player.
        public void Deflect(Vector3 newDirection)
        {
            if (!_isEnemyOwned) return;

            _isEnemyOwned = false;
            _direction = newDirection.normalized;
            _spawnTime = Time.time;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = MaterialCache.Get(new Color(1f, 0.85f, 0.2f));
        }
    }
}
