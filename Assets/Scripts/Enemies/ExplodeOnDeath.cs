using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // The Bomber's payload. There are two ways it goes off:
    //
    //  - You kill it, and it drops a live bomb where it fell. That one sits
    //    still, so killing a Bomber in a pack is a gift and killing one at
    //    your feet is a problem you have a moment to walk out of.
    //  - It reaches you, lights the fuse itself, and KEEPS CHASING. That one
    //    can't be outlasted by standing still — you have to break away or
    //    kill it and then leave the bomb behind.
    //
    // Both show the same countdown: a yellow disc marking the blast radius
    // with orange filling it from the middle outward, detonating the instant
    // the orange reaches the rim.
    public class ExplodeOnDeath : MonoBehaviour
    {
        // Long enough to see the countdown, place yourself, and walk or dash
        // clear — a short fuse just felt like an unavoidable tax for killing
        // the wrong enemy at the wrong moment.
        public float FuseSeconds = 1.8f;
        public float Radius = 3.2f;
        public float Damage = 28f;

        // How close it has to get before it lights the fuse on its own.
        public float ArmRange = 1.5f;

        // Other enemies take a third. At full damage a couple of Bombers
        // going off in a crowd cleared the wave for the player, which turned
        // the scariest enemy in the game into the most helpful one.
        public const float AllyDamageFraction = 1f / 3f;

        bool _armed;

        static readonly Collider[] HitBuffer = new Collider[48];

        void Awake()
        {
            var health = GetComponent<EnemyHealth>();
            // Already counting down: the bomb is committed and follows its
            // own timer, so dying part-way through must not start a second one.
            if (health != null) health.OnDied += _ => { if (!_armed) SpawnCharge(null); };
        }

        void Update()
        {
            if (_armed) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > ArmRange) return;

            _armed = true;
            SpawnCharge(transform);
        }

        // The charge is its own GameObject because the Bomber is destroyed
        // the moment it dies — a coroutine on the corpse would never finish.
        // When `follow` is set it tracks the still-living Bomber and takes it
        // with it when it goes off.
        void SpawnCharge(Transform follow)
        {
            var go = new GameObject("BomberCharge");
            go.transform.position = transform.position;
            go.AddComponent<PendingExplosion>().Begin(FuseSeconds, Radius, Damage, follow);
        }

        public class PendingExplosion : MonoBehaviour
        {
            float _radius;
            float _damage;
            Transform _follow;

            public void Begin(float fuse, float radius, float damage, Transform follow)
            {
                _radius = radius;
                _damage = damage;
                _follow = follow;
                StartCoroutine(Run(fuse));
            }

            IEnumerator Run(float fuse)
            {
                Sfx.BomberFuse(transform.position);

                // A dropped bomb needs to be a thing on the ground you can
                // see and walk around, not just a coloured circle. A bomb
                // still being carried doesn't get one — the Bomber itself is
                // the thing you can see coming.
                GameObject bomb = null;
                if (_follow == null)
                {
                    bomb = Blocky3DArt.Bomb();
                    bomb.transform.SetParent(transform, false);
                    bomb.transform.localPosition = Vector3.zero;
                }

                // Fixed yellow disc at the blast radius, with orange filling
                // it from the centre out. When the orange reaches the rim,
                // it goes off — so the countdown reads as a distance as well
                // as a time, and you can see exactly what "clear" means.
                var rim = Blocky3DArt.SwipeDisc(new Color(0.96f, 0.84f, 0.18f));
                rim.transform.position = transform.position + Vector3.up * 0.05f;
                rim.transform.localScale = new Vector3(_radius * 2f, 0.02f, _radius * 2f);

                var fill = Blocky3DArt.SwipeDisc(new Color(0.95f, 0.42f, 0.10f));
                fill.transform.position = transform.position + Vector3.up * 0.07f;

                float t = 0f;
                while (t < fuse)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / fuse);

                    if (_follow != null) transform.position = _follow.position;

                    Vector3 discPos = transform.position;
                    rim.transform.position = discPos + Vector3.up * 0.05f;
                    fill.transform.position = discPos + Vector3.up * 0.07f;

                    float filled = _radius * 2f * p;
                    fill.transform.localScale = new Vector3(filled, 0.02f, filled);

                    // A bob that quickens as the fuse burns down, so the last
                    // half-second is obvious from peripheral vision.
                    if (bomb != null)
                    {
                        float urgency = Mathf.Lerp(8f, 30f, p);
                        bomb.transform.localScale = Vector3.one * (1f + 0.09f * Mathf.Abs(Mathf.Sin(t * urgency)));
                    }

                    yield return null;
                }

                Destroy(rim);
                Destroy(fill);
                if (bomb != null) Destroy(bomb);

                // A carried bomb takes its carrier with it.
                if (_follow != null)
                {
                    var carrier = _follow.GetComponent<EnemyHealth>();
                    if (carrier != null) carrier.Kill();
                }

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
                        enemy.TakeDamage(_damage * AllyDamageFraction, away, 8f);
                        continue;
                    }

                    var player = HitBuffer[i].GetComponentInParent<PlayerHealth>();
                    if (player != null) player.TakeDamage(_damage, transform.position);
                }
            }
        }
    }
}
