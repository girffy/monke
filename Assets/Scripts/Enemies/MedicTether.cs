using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // The Medic. Runs a green tether to one nearby ally and makes it
    // outright invulnerable for as long as the link holds, so the crowd
    // can't be cleared until the medic is dealt with.
    //
    // This replaced a heal-over-time aura, which was invisible in practice:
    // enemies die fast enough that topping them up changed nothing. A hard
    // "you cannot kill this one" is readable at a glance and forces the
    // player to find and kill the support first.
    public class MedicTether : MonoBehaviour
    {
        public float Range = 8f;
        public float RetargetInterval = 0.4f;

        EnemyHealth _target;
        LineRenderer _line;
        float _nextRetarget;

        static readonly Collider[] HitBuffer = new Collider[64];

        void Awake()
        {
            var lineGO = new GameObject("Tether");
            lineGO.transform.SetParent(transform, false);

            _line = lineGO.AddComponent<LineRenderer>();
            _line.material = MaterialCache.GetUnlit(new Color(0.35f, 1f, 0.45f));
            _line.widthMultiplier = 0.09f;
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.enabled = false;

            var health = GetComponent<EnemyHealth>();
            if (health != null) health.OnDied += _ => Release();
        }

        void OnDestroy() => Release();

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            // Drop a target that died or wandered out of range.
            if (_target != null && (_target.CurrentHP <= 0f
                || Vector3.Distance(_target.transform.position, transform.position) > Range * 1.25f))
            {
                Release();
            }

            if (_target == null && Time.time >= _nextRetarget)
            {
                _nextRetarget = Time.time + RetargetInterval;
                Acquire();
            }

            if (_target == null)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;
            _line.SetPosition(0, transform.position + Vector3.up * 1.2f);
            _line.SetPosition(1, _target.transform.position + Vector3.up * 1.0f);
        }

        void Acquire()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, Range, HitBuffer);
            EnemyHealth best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var candidate = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (candidate == null || candidate.gameObject == gameObject) continue;
                // Medics never shield each other — two of them propping each
                // other up would be unkillable.
                if (candidate.GetComponent<MedicTether>() != null) continue;
                // Prefer someone not already shielded by another medic.
                if (candidate.IsShielded) continue;

                float distance = Vector3.Distance(candidate.transform.position, transform.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidate;
            }

            if (best == null) return;

            _target = best;
            _target.AddShield();
            Sfx.Heal(transform.position);
        }

        void Release()
        {
            if (_target != null) _target.RemoveShield();
            _target = null;
            if (_line != null) _line.enabled = false;
        }
    }
}
