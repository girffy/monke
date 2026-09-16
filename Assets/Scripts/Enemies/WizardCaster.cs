using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Environment;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // The Wizard: the first enemy that fights on its own terms rather than
    // yours.
    //
    // Everything else in the game is solved by closing the distance — even
    // the Thrower just walks backwards while you catch it. The Wizard answers
    // that by leaving: get close, or hurt it, and it blinks somewhere else in
    // the arena. So the counter isn't chasing, it's the ranged tools (dung
    // toss) or catching it in the moment after a cast while it's committed.
    //
    // Its fireball is slow but lands as an area, which means walking out of
    // the marked circle is always possible and standing in the crowd is not.
    [RequireComponent(typeof(EnemyHealth))]
    public class WizardCaster : MonoBehaviour
    {
        public float FireballDamage = 26f;
        public float FireballRadius = 2.6f;
        public float CastInterval = 3.2f;
        public float CastWindup = 0.85f;

        // Blinks away when the player gets inside this.
        public float PanicRange = 4.5f;
        public float TeleportCooldown = 4f;
        // Where it lands, relative to the player: far enough to be a problem,
        // close enough that it can still see and be seen.
        public float TeleportMinRange = 8f;
        public float TeleportMaxRange = 12f;

        EnemyHealth _health;
        EnemyAI _ai;
        Transform _model;
        float _nextCastTime;
        float _nextTeleportTime;
        bool _casting;

        void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _ai = GetComponent<EnemyAI>();
            _model = transform.Find("HumanModel");

            // Taking a real hit is the other thing that makes it leave, so
            // cornering it doesn't simply work.
            _health.OnDamaged += HandleDamaged;

            _nextCastTime = Time.time + 1.2f;
            _nextTeleportTime = Time.time + 1f;
        }

        void OnDestroy()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
        }

        void HandleDamaged(float amount)
        {
            // Only flees from a meaningful hit — a bleed tick shouldn't
            // trigger an escape.
            if (amount < 6f) return;
            TryTeleport();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance < PanicRange) TryTeleport();

            if (!_casting && Time.time >= _nextCastTime && distance < 18f)
            {
                StartCoroutine(Cast(player));
            }
        }

        void TryTeleport()
        {
            if (_casting || Time.time < _nextTeleportTime) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            _nextTeleportTime = Time.time + TeleportCooldown;

            Vector3 destination = FindDestination(player.transform.position);
            SpawnBlink(transform.position);

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.position = destination;
            transform.position = destination;

            SpawnBlink(destination);
            Sfx.Dash(destination);
        }

        Vector3 FindDestination(Vector3 playerPos)
        {
            var arena = Arena.Instance;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = Random.Range(0f, 360f);
                float range = Random.Range(TeleportMinRange, TeleportMaxRange);
                Vector3 candidate = playerPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * range;
                candidate.y = transform.position.y;

                if (arena == null || arena.IsInside(candidate, 1.5f)) return candidate;
            }

            // Small arena, player in the middle of it: settle for anywhere
            // legal rather than refusing to move.
            return arena != null ? arena.ClampInside(playerPos + Vector3.forward * TeleportMinRange, 1.5f) : playerPos;
        }

        void SpawnBlink(Vector3 position)
        {
            var puff = Blocky3DArt.SwipeDisc(new Color(0.55f, 0.75f, 1f));
            puff.transform.position = position + Vector3.up * 0.06f;
            puff.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            puff.AddComponent<ExpandingDisc>().Play(3.2f, 0.32f);
        }

        IEnumerator Cast(PlayerController player)
        {
            _casting = true;
            _nextCastTime = Time.time + CastInterval;

            // The target is fixed where the player is standing NOW, and the
            // circle is drawn immediately. The whole point is that it can be
            // walked out of — a homing fireball from a teleporting enemy
            // would be unanswerable.
            Vector3 target = player.transform.position;
            target.y = 0f;

            var rim = Blocky3DArt.SwipeDisc(new Color(0.95f, 0.45f, 0.15f));
            rim.transform.position = target + Vector3.up * 0.05f;
            rim.transform.localScale = new Vector3(FireballRadius * 2f, 0.02f, FireballRadius * 2f);

            var fill = Blocky3DArt.SwipeDisc(new Color(1f, 0.78f, 0.25f));
            fill.transform.position = target + Vector3.up * 0.07f;

            // Staff lifts while the spell builds.
            var staff = _model != null ? _model.Find("Staff") : null;
            Quaternion staffRest = staff != null ? staff.localRotation : Quaternion.identity;

            float t = 0f;
            while (t < CastWindup)
            {
                t += Time.deltaTime;
                if (GameManager.Instance != null && GameManager.Instance.IsPaused) { yield return null; continue; }

                float p = Mathf.Clamp01(t / CastWindup);
                float filled = FireballRadius * 2f * p;
                fill.transform.localScale = new Vector3(filled, 0.02f, filled);
                if (staff != null) staff.localRotation = staffRest * Quaternion.Euler(-60f * p, 0f, 0f);
                yield return null;
            }

            if (staff != null) staff.localRotation = staffRest;
            Destroy(rim);
            Destroy(fill);

            Fireball.Launch(transform.position + Vector3.up * 1.6f, target, FireballDamage, FireballRadius);
            _casting = false;
        }
    }

    // The Wizard's payload: a lobbed ball of fire that bursts for area
    // damage where it lands.
    public class Fireball : MonoBehaviour
    {
        float _damage;
        float _radius;
        Vector3 _start;
        Vector3 _target;
        float _flightTime;
        float _arcHeight;

        static readonly Collider[] HitBuffer = new Collider[32];

        public static Fireball Launch(Vector3 from, Vector3 target, float damage, float radius)
        {
            var go = new GameObject("Fireball");
            go.transform.position = from;

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * 0.44f;
            core.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetGlowing(new Color(1f, 0.55f, 0.12f), 2.6f);

            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(go.transform, false);
            halo.transform.localScale = Vector3.one * 0.66f;
            halo.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetUnlit(new Color(0.92f, 0.30f, 0.08f));

            var ball = go.AddComponent<Fireball>();
            ball._damage = damage;
            ball._radius = radius;
            ball._start = from;
            ball._target = target;

            float distance = Vector3.Distance(from, target);
            ball._flightTime = Mathf.Clamp(distance / 11f, 0.3f, 1.4f);
            ball._arcHeight = Mathf.Clamp(distance * 0.18f, 0.8f, 3f);

            Sfx.Throw(from);
            ball.StartCoroutine(ball.Fly());
            return ball;
        }

        IEnumerator Fly()
        {
            float t = 0f;
            while (t < _flightTime)
            {
                t += Time.deltaTime;
                if (GameManager.Instance != null && GameManager.Instance.IsPaused) { yield return null; continue; }

                float p = Mathf.Clamp01(t / _flightTime);
                Vector3 pos = Vector3.Lerp(_start, _target, p);
                pos.y += _arcHeight * 4f * p * (1f - p);
                transform.position = pos;
                yield return null;
            }

            Burst();
        }

        void Burst()
        {
            Sfx.RockExplosion(transform.position);
            CameraShake.Shake(0.2f, 0.2f);

            var blast = Blocky3DArt.SwipeDisc(new Color(1f, 0.6f, 0.18f));
            blast.transform.position = _target + Vector3.up * 0.07f;
            blast.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            blast.AddComponent<ExpandingDisc>().Play(_radius * 2.1f, 0.26f);

            // Hurts the player only. Wizards don't kill their own side — the
            // Bomber already fills the "blows up its neighbours" role, and a
            // second one would just make crowds delete themselves.
            var player = PlayerController.Instance;
            if (player != null)
            {
                Vector3 flat = player.transform.position - _target;
                flat.y = 0f;
                if (flat.magnitude <= _radius)
                {
                    player.GetComponent<PlayerHealth>()?.TakeDamage(_damage, _target);
                }
            }

            Destroy(gameObject);
        }
    }
}
