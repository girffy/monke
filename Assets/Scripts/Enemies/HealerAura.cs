using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // The Medic. Periodically tops up damaged enemies around it, which turns
    // a crowd the player was chipping down into one that heals faster than
    // they can clear it — the intended answer is to identify the white shirt
    // and kill it first.
    public class HealerAura : MonoBehaviour
    {
        public float Radius = 5.5f;
        public float HealAmount = 9f;
        public float Interval = 2.4f;

        float _nextPulseTime;
        static readonly Collider[] HitBuffer = new Collider[48];

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextPulseTime) return;

            _nextPulseTime = Time.time + Interval;
            Pulse();
        }

        void Pulse()
        {
            bool healedAnyone = false;
            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, HitBuffer);

            for (int i = 0; i < count; i++)
            {
                var enemy = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null) continue;
                // Medics don't heal themselves — otherwise they're a nuisance
                // to kill on top of being a priority target.
                if (enemy.gameObject == gameObject) continue;
                if (enemy.CurrentHP >= enemy.MaxHP) continue;

                enemy.Heal(HealAmount);
                healedAnyone = true;
            }

            if (!healedAnyone) return;

            Sfx.Heal(transform.position);
            var ring = Blocky3DArt.SwipeDisc(new Color(0.45f, 0.95f, 0.55f));
            ring.transform.position = transform.position + Vector3.up * 0.05f;
            ring.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            ring.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(Radius * 2f, 0.45f);
        }
    }
}
