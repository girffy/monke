using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Environment
{
    // A tree the player can knock down. Hitting one sends it toppling in the
    // attack's direction; everything caught under the trunk as it sweeps down
    // takes heavy damage. It's the highest-damage tool in the game, balanced
    // by having to fight near a tree and line the fall up with the crowd.
    //
    // A felled tree leaves a log behind, then quietly swaps itself for a new
    // standing tree off-camera so a long run doesn't strip the map bare.
    public class FellableTree : MonoBehaviour
    {
        const float FallDuration = 0.62f;
        const float TrunkHitRadius = 0.95f;
        const float RegrowDelay = 30f;

        // Set by whatever spawned this: inside the arena the "trees" are
        // stone columns, and a toppled column should leave rubble.
        public bool RemnantIsRubble;

        bool _felled;
        float _height = 3.2f;

        public bool IsFelled => _felled;

        static readonly Collider[] HitBuffer = new Collider[64];

        void Awake()
        {
            // Measure once from the renderers so each species/scale topples
            // over the right length of ground.
            var renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                _height = Mathf.Max(1.5f, bounds.max.y - transform.position.y);
            }
        }

        public void Fell(Vector3 direction, float damage)
        {
            if (_felled) return;
            _felled = true;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            StartCoroutine(FallSequence(direction, damage));
        }

        IEnumerator FallSequence(Vector3 direction, float damage)
        {
            // Topple around the horizontal axis perpendicular to the fall
            // direction, pivoting at the base (the models are built with all
            // geometry above the root origin precisely so this works).
            Vector3 axis = Vector3.Cross(Vector3.up, direction).normalized;
            Quaternion start = transform.rotation;
            Quaternion end = Quaternion.AngleAxis(88f, axis) * start;

            var alreadyHit = new HashSet<EnemyHealth>();
            Sfx.TreeCrack(transform.position);

            float t = 0f;
            while (t < FallDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / FallDuration);
                // Accelerating fall — a tree pivots slowly off vertical and
                // then comes down fast, which linear interpolation misses.
                float eased = p * p * (1.1f - 0.1f * p);
                transform.rotation = Quaternion.Slerp(start, end, eased);

                SweepDamage(direction, eased, damage, alreadyHit);
                yield return null;
            }

            transform.rotation = end;
            SweepDamage(direction, 1f, damage, alreadyHit);

            Impact(direction);
            StartCoroutine(CleanupAfterDelay());
        }

        // Damages anything under the trunk's current sweep. Each enemy is hit
        // at most once per fall, so a slow topple isn't a damage-over-time.
        void SweepDamage(Vector3 direction, float progress, float damage, HashSet<EnemyHealth> alreadyHit)
        {
            // Only the outer part of the swing actually reaches the ground
            // where enemies are standing.
            if (progress < 0.35f) return;

            Vector3 basePos = transform.position + Vector3.up * 0.4f;
            float reach = _height * Mathf.Sin(Mathf.Lerp(0f, Mathf.PI / 2f, progress));
            float lift = _height * Mathf.Cos(Mathf.Lerp(0f, Mathf.PI / 2f, progress));
            Vector3 tip = transform.position + direction * reach + Vector3.up * Mathf.Max(0.4f, lift * 0.5f);

            int count = Physics.OverlapCapsuleNonAlloc(basePos, tip, TrunkHitRadius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemy = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || alreadyHit.Contains(enemy)) continue;

                alreadyHit.Add(enemy);
                Vector3 knock = direction;
                enemy.TakeDamage(damage, knock, 11f);
            }
        }

        void Impact(Vector3 direction)
        {
            Sfx.TreeFall(transform.position);
            CameraShake.Shake(0.55f, 0.35f);

            // Dust kicked up along the length of the fallen trunk.
            for (int i = 0; i < 4; i++)
            {
                float along = (i + 1) / 5f * _height;
                var puff = Blocky3DArt.SwipeDisc(new Color(0.62f, 0.56f, 0.44f));
                puff.transform.position = transform.position + direction * along + Vector3.up * 0.06f;
                puff.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
                var runner = puff.AddComponent<ExpandingDisc>();
                runner.Play(2.2f, 0.4f);
            }

            // Columns leave a broken plinth where they stood; trees leave a
            // stump. Same lifecycle either way.
            var stump = RemnantIsRubble ? Blocky3DArt.ColumnRubble() : Blocky3DArt.Stump();
            stump.transform.position = transform.position;
            stump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            stump.transform.localScale = transform.localScale * 0.9f;
            // The stump is part of this tree's lifecycle: it disappears when
            // the replacement grows in, so props don't accumulate forever.
            _stump = stump;
        }

        GameObject _stump;

        // The log lies around for a while, then clears itself. Growing the
        // replacement isn't this tree's job — EnvironmentStreamer sees the
        // population drop and puts a new one in off-camera.
        IEnumerator CleanupAfterDelay()
        {
            yield return new WaitForSeconds(RegrowDelay);

            if (_stump != null) Destroy(_stump);
            Destroy(gameObject);
        }
    }

    // Scales a flat disc outward and destroys it. Lives on the VFX object so
    // the effect outlives whatever spawned it.
    public class ExpandingDisc : MonoBehaviour
    {
        public void Play(float targetScale, float duration)
        {
            StartCoroutine(Animate(targetScale, duration));
        }

        IEnumerator Animate(float targetScale, float duration)
        {
            float t = 0f;
            float startScale = transform.localScale.x;
            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(startScale, targetScale, t / duration);
                transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
