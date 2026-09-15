using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Environment;

namespace GorillaSurvivors.Player
{
    // Secondary attack: right-click for a quick one-handed swipe. Shorter
    // range and lower damage than the ground-slam, and — unlike the
    // slam — doesn't lock movement and has no cooldown beyond the swipe's
    // own short duration, so it's a fast poke you can throw out while
    // repositioning rather than a committed hit.
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerController))]
    public class QuickSwipeAttack : MonoBehaviour
    {
        public float BaseDamage = 10f;
        public float HitRadius = 0.9f;
        public float ForwardOffset = 1.1f;
        public const float SwipeDuration = 0.22f;

        PlayerStats _stats;
        PlayerController _controller;
        CharacterAnimator _animator;
        PlayerAttack _attackCache;
        Transform _armR;
        bool _isSwiping;

        static readonly Collider[] HitBuffer = new Collider[32];
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 SwipeStartDir = new Vector3(0.7f, 0.3f, 0.2f).normalized;
        static readonly Vector3 SwipeEndDir = new Vector3(-0.6f, -0.1f, 0.6f).normalized;

        public bool IsSwiping => _isSwiping;
        public float SwipeCooldownRemaining01() => _isSwiping ? 1f : 0f;

        // Lazy: PlayerAttack and QuickSwipeAttack can be added to the
        // player in either order, so an Awake-time GetComponent here isn't
        // guaranteed to find it yet.
        PlayerAttack Attack => _attackCache != null ? _attackCache : (_attackCache = GetComponent<PlayerAttack>());

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (_isSwiping) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Attack != null && Attack.IsSlamming) return;
            if (!WasSwipePressed()) return;

            StartCoroutine(SwipeSequence());
        }

        // Left mouse / X / left trigger. buttonNorth is deliberately NOT used
        // here — that's the chest beat, and the two used to share it.
        bool WasSwipePressed()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonWest.wasPressedThisFrame || gp.leftTrigger.wasPressedThisFrame)) return true;

            return false;
        }

        IEnumerator SwipeSequence()
        {
            _isSwiping = true;
            if (_animator != null) _animator.SuppressArms = true;

            const float outT = SwipeDuration * 0.4f;
            const float hitT = SwipeDuration * 0.15f;
            const float backT = SwipeDuration - outT - hitT;

            // The body counter-rotates into the swing and unwinds out of it,
            // so a fast poke still reads as a whole-body motion rather than
            // one arm flapping.
            yield return AnimateArm(RestDir, SwipeStartDir, outT, 0f, -14f);
            yield return AnimateArm(SwipeStartDir, SwipeEndDir, hitT, -14f, 16f);

            PerformSwipeHit();
            CameraShake.Shake(0.08f, 0.12f);

            yield return AnimateArm(SwipeEndDir, RestDir, backT, 16f, 0f);

            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyYaw = 0f;
            }
            _isSwiping = false;
        }

        IEnumerator AnimateArm(Vector3 fromDir, Vector3 toDir, float duration, float fromYaw = 0f, float toYaw = 0f)
        {
            if (duration <= 0f) yield break;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                SetArmDirection(Vector3.Slerp(fromDir, toDir, p));
                if (_animator != null) _animator.BodyYaw = Mathf.Lerp(fromYaw, toYaw, p);
                yield return null;
            }
            SetArmDirection(toDir);
        }

        void SetArmDirection(Vector3 localDirection)
        {
            if (_armR == null) return;
            _armR.localRotation = Quaternion.FromToRotation(Vector3.down, localDirection);
        }

        void PerformSwipeHit()
        {
            Vector3 aimDirection = _controller.GetAimDirection();
            aimDirection.y = 0f;
            aimDirection.Normalize();

            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float radius = HitRadius * _stats.LevelAttackRadiusBonus * _stats.AreaMultiplier;
            Vector3 hitCenter = transform.position + aimDirection * ForwardOffset;

            int count = Physics.OverlapSphereNonAlloc(hitCenter, radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Vector3 knockDir = enemyHealth.transform.position - hitCenter;
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude < 0.0001f) knockDir = aimDirection;
                    enemyHealth.TakeDamage(damage, knockDir, 4f);
                    continue;
                }

                var rock = HitBuffer[i].GetComponentInParent<AttackableRock>();
                if (rock != null && !rock.IsLaunched)
                {
                    float rockDashDistance = _controller.DashSpeed * _controller.DashDuration;
                    rock.Launch(aimDirection, damage * 2f, rockDashDistance);
                    continue;
                }

                var tree = HitBuffer[i].GetComponentInParent<FellableTree>();
                if (tree != null && !tree.IsFelled)
                {
                    tree.Fell(aimDirection, damage * 4f);
                }
            }

            foreach (var projectile in Projectile.Active)
            {
                if (projectile == null) continue;
                if (Vector3.Distance(projectile.transform.position, hitCenter) <= radius)
                {
                    projectile.Deflect(aimDirection);
                }
            }

            SpawnSwipeEffect(hitCenter, radius);
            Sfx.Swipe(hitCenter);
        }

        void SpawnSwipeEffect(Vector3 position, float radius)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(0.9f, 0.85f, 0.3f));
            go.transform.position = position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);

            StartCoroutine(AnimateSwipeEffect(go, radius));
        }

        IEnumerator AnimateSwipeEffect(GameObject go, float radius)
        {
            float duration = 0.12f;
            float t = 0f;
            float targetScale = radius * 1.8f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.05f, targetScale, t / duration);
                go.transform.localScale = new Vector3(scale, 0.02f, scale);
                yield return null;
            }

            Destroy(go);
        }
    }
}
