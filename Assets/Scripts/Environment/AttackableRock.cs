using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Environment
{
    // A terrain rock the player can hit to send it flying as a boulder that
    // explodes for AoE damage — either on hitting an enemy, or after
    // travelling its max distance. Consumed on launch; AttackableRockManager
    // spawns a replacement offscreen after a delay.
    public class AttackableRock : MonoBehaviour
    {
        const float FlightSpeed = 16f;
        const float ExplosionRadius = 2.2f;

        Collider _collider;
        bool _launched;

        static readonly Collider[] HitBuffer = new Collider[32];

        void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public bool IsLaunched => _launched;

        public void Launch(Vector3 direction, float damage, float maxDistance)
        {
            if (_launched) return;
            _launched = true;
            if (_collider != null) _collider.enabled = false;

            StartCoroutine(FlySequence(direction.normalized, damage, maxDistance));
        }

        IEnumerator FlySequence(Vector3 dir, float damage, float maxDistance)
        {
            float traveled = 0f;

            while (traveled < maxDistance)
            {
                float step = FlightSpeed * Time.deltaTime;
                transform.position += dir * step;
                traveled += step;

                if (HasHitAnEnemy()) break;

                yield return null;
            }

            Explode(damage);
        }

        bool HasHitAnEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 0.6f, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].GetComponentInParent<EnemyHealth>() != null) return true;
            }
            return false;
        }

        void Explode(float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, ExplosionRadius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null) enemyHealth.TakeDamage(damage);
            }

            SpawnExplosionEffect();
            Sfx.RockExplosion(transform.position);

            AttackableRockManager.Instance?.NotifyRockConsumed();
            Destroy(gameObject);
        }

        void SpawnExplosionEffect()
        {
            var go = Blocky3DArt.SwipeDisc(new Color(0.5f, 0.42f, 0.35f));
            go.transform.position = transform.position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            var runner = go.AddComponent<ExplosionEffectRunner>();
            runner.Play(ExplosionRadius * 1.8f, 0.22f);
        }
    }

    // Scales a disc up and destroys it — pulled out of AttackableRock so the
    // VFX object (which outlives the rock itself) can run its own coroutine.
    public class ExplosionEffectRunner : MonoBehaviour
    {
        public void Play(float targetScale, float duration)
        {
            StartCoroutine(Animate(targetScale, duration));
        }

        IEnumerator Animate(float targetScale, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.1f, targetScale, t / duration);
                transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
