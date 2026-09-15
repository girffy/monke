using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // The Bomber's payload. Killing one arms a short fuse and then detonates
    // where it died, hurting the player AND other enemies — so a Bomber in
    // the middle of a pack is a gift, and one at your feet is a problem you
    // have a moment to dash away from.
    public class ExplodeOnDeath : MonoBehaviour
    {
        public float FuseSeconds = 0.55f;
        public float Radius = 3.2f;
        public float Damage = 28f;

        static readonly Collider[] HitBuffer = new Collider[48];

        void Awake()
        {
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.OnDied += _ => SpawnCharge();
        }

        // The charge is its own GameObject because the Bomber is destroyed
        // the moment it dies — a coroutine on the corpse would never finish.
        void SpawnCharge()
        {
            var go = new GameObject("BomberCharge");
            go.transform.position = transform.position;
            var charge = go.AddComponent<PendingExplosion>();
            charge.Begin(FuseSeconds, Radius, Damage);
        }

        public class PendingExplosion : MonoBehaviour
        {
            float _radius;
            float _damage;

            public void Begin(float fuse, float radius, float damage)
            {
                _radius = radius;
                _damage = damage;
                StartCoroutine(Run(fuse));
            }

            IEnumerator Run(float fuse)
            {
                Sfx.BomberFuse(transform.position);

                // A pulsing warning sphere marks the blast radius so the
                // detonation is something you can react to, not a surprise.
                var warn = Blocky3DArt.SwipeDisc(new Color(1f, 0.45f, 0.12f));
                warn.transform.position = transform.position + Vector3.up * 0.06f;

                float t = 0f;
                while (t < fuse)
                {
                    t += Time.deltaTime;
                    float p = t / fuse;
                    float pulse = 0.85f + 0.15f * Mathf.Sin(p * 34f);
                    float scale = _radius * 2f * Mathf.Lerp(0.35f, 1f, p) * pulse;
                    warn.transform.localScale = new Vector3(scale, 0.02f, scale);
                    yield return null;
                }

                Destroy(warn);
                Detonate();
                Destroy(gameObject, 0.5f);
            }

            void Detonate()
            {
                Sfx.RockExplosion(transform.position);
                CameraShake.Shake(0.35f, 0.3f);

                var blast = Blocky3DArt.SwipeDisc(new Color(1f, 0.72f, 0.25f));
                blast.transform.position = transform.position + Vector3.up * 0.07f;
                blast.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
                blast.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(_radius * 2.1f, 0.26f);

                int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, HitBuffer);
                for (int i = 0; i < count; i++)
                {
                    var enemy = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                    if (enemy != null)
                    {
                        Vector3 away = enemy.transform.position - transform.position;
                        away.y = 0f;
                        enemy.TakeDamage(_damage, away, 8f);
                        continue;
                    }

                    var player = HitBuffer[i].GetComponentInParent<PlayerHealth>();
                    if (player != null) player.TakeDamage(_damage);
                }
            }
        }
    }
}
