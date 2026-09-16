using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player.Abilities
{
    // The thing the gorilla throws. Lobbed on an arc rather than fired flat,
    // so it reads as thrown weight and lands visibly short or long — then
    // splatters into a patch that damages on impact and slows anything that
    // walks through it afterwards.
    public class DungProjectile : MonoBehaviour
    {
        public float ImpactRadius = 2.2f;
        public float Damage = 26f;
        public float PatchDuration = 3.5f;
        public float SlowFactor = 0.45f;

        Vector3 _start;
        Vector3 _target;
        float _flightTime;
        float _arcHeight;

        static readonly Collider[] HitBuffer = new Collider[64];

        public static DungProjectile Launch(Vector3 from, Vector3 target, float damage, float radius)
        {
            var go = new GameObject("Dung");
            go.transform.position = from;

            // A lumpy clod rather than a clean sphere.
            var main = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(main.GetComponent<Collider>());
            main.transform.SetParent(go.transform, false);
            main.transform.localScale = Vector3.one * 0.36f;
            main.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(new Color(0.34f, 0.24f, 0.14f));

            for (int i = 0; i < 2; i++)
            {
                var lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(lump.GetComponent<Collider>());
                lump.transform.SetParent(go.transform, false);
                lump.transform.localPosition = Random.insideUnitSphere * 0.14f;
                lump.transform.localScale = Vector3.one * Random.Range(0.18f, 0.26f);
                lump.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(new Color(0.27f, 0.19f, 0.11f));
            }

            var proj = go.AddComponent<DungProjectile>();
            proj.Damage = damage;
            proj.ImpactRadius = radius;
            proj._start = from;
            proj._target = target;

            float distance = Vector3.Distance(from, target);
            // Constant-ish speed: a short lob shouldn't hang in the air as
            // long as a full-range one.
            proj._flightTime = Mathf.Clamp(distance / 13f, 0.18f, 0.9f);
            proj._arcHeight = Mathf.Clamp(distance * 0.22f, 0.7f, 2.6f);

            Sfx.Throw(from);
            proj.StartCoroutine(proj.Fly());
            return proj;
        }

        // How close to a body the clod passes before it bursts on them.
        const float InterceptRadius = 0.65f;
        // Skipped for the first stretch of the flight so the clod never
        // detonates on something already pressed against the gorilla.
        const float InterceptStart = 0.12f;

        IEnumerator Fly()
        {
            float t = 0f;
            while (t < _flightTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _flightTime);

                Vector3 pos = Vector3.Lerp(_start, _target, p);
                // Parabola peaking at the midpoint of the throw.
                pos.y += _arcHeight * 4f * p * (1f - p);
                transform.position = pos;
                transform.Rotate(520f * Time.deltaTime, 340f * Time.deltaTime, 0f, Space.Self);

                // The throw is stopped by the first body in its path. It used
                // to fly over everything and land exactly on whatever it was
                // aimed at, which made it a sniper rifle for picking the
                // medics and throwers out of the back of a crowd from safety.
                // Now the crowd is in the way, so reaching the back line
                // means making a lane first.
                if (p > InterceptStart)
                {
                    Vector3 groundPos = new Vector3(pos.x, _target.y, pos.z);
                    if (FindIntercept(groundPos) != null)
                    {
                        _target = groundPos;
                        break;
                    }
                }

                yield return null;
            }

            Splat();
        }

        // Horizontal check, not a 3D one: the clod is metres up mid-arc, so a
        // sphere test at its actual height would sail cleanly over everybody
        // and the interception would never fire.
        EnemyHealth FindIntercept(Vector3 groundPos)
        {
            int count = Physics.OverlapSphereNonAlloc(groundPos, InterceptRadius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemy = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemy != null) return enemy;
            }
            return null;
        }

        void Splat()
        {
            Sfx.Splat(transform.position);

            int count = Physics.OverlapSphereNonAlloc(_target, ImpactRadius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemy = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null) continue;

                Vector3 away = enemy.transform.position - _target;
                away.y = 0f;
                enemy.TakeDamage(Damage, away, 4f);
            }

            DungPatch.Create(_target, ImpactRadius, PatchDuration, SlowFactor);
            Destroy(gameObject);
        }
    }

    // The lingering mess. Enemies inside it are slowed; the slow lifts when
    // they leave or the patch dries up.
    public class DungPatch : MonoBehaviour
    {
        float _radius;
        float _expiresAt;
        float _slowFactor;

        readonly HashSet<EnemyAI> _slowed = new HashSet<EnemyAI>();
        static readonly Collider[] HitBuffer = new Collider[64];

        public static DungPatch Create(Vector3 position, float radius, float duration, float slowFactor)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(0.30f, 0.22f, 0.12f));
            go.name = "DungPatch";
            go.transform.position = position + Vector3.up * 0.04f;
            go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            var patch = go.AddComponent<DungPatch>();
            patch._radius = radius;
            patch._slowFactor = slowFactor;
            patch._expiresAt = Time.time + duration;
            return patch;
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            if (Time.time >= _expiresAt)
            {
                ClearAll();
                Destroy(gameObject);
                return;
            }

            var stillInside = new HashSet<EnemyAI>();
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var ai = HitBuffer[i].GetComponentInParent<EnemyAI>();
                if (ai == null) continue;

                stillInside.Add(ai);
                if (_slowed.Add(ai)) ai.ApplySlow(_slowFactor);
            }

            // Anything that walked out (or died) gets its speed back.
            _slowed.RemoveWhere(ai =>
            {
                if (ai != null && stillInside.Contains(ai)) return false;
                if (ai != null) ai.ClearSlow();
                return true;
            });
        }

        void ClearAll()
        {
            foreach (var ai in _slowed)
            {
                if (ai != null) ai.ClearSlow();
            }
            _slowed.Clear();
        }
    }
}
