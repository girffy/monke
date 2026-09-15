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
        Transform _armR;
        bool _isSwiping;

        static readonly Collider[] HitBuffer = new Collider[32];
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 SwipeStartDir = new Vector3(0.7f, 0.3f, 0.2f).normalized;
        static readonly Vector3 SwipeEndDir = new Vector3(-0.6f, -0.1f, 0.6f).normalized;

        public float SwipeCooldownRemaining01() => _isSwiping ? 1f : 0f;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            var model = transform.Find("GorillaModel");
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (_isSwiping) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (!WasSwipePressed()) return;

            StartCoroutine(SwipeSequence());
        }

        bool WasSwipePressed()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonNorth.wasPressedThisFrame) return true;

            return false;
        }

        IEnumerator SwipeSequence()
        {
            _isSwiping = true;

            const float outT = SwipeDuration * 0.4f;
            const float hitT = SwipeDuration * 0.15f;
            const float backT = SwipeDuration - outT - hitT;

            yield return AnimateArm(RestDir, SwipeStartDir, outT);
            yield return AnimateArm(SwipeStartDir, SwipeEndDir, hitT);

            PerformSwipeHit();

            yield return AnimateArm(SwipeEndDir, RestDir, backT);

            _isSwiping = false;
        }

        IEnumerator AnimateArm(Vector3 fromDir, Vector3 toDir, float duration)
        {
            if (duration <= 0f) yield break;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetArmDirection(Vector3.Slerp(fromDir, toDir, t / duration));
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
            float radius = HitRadius * _stats.LevelAttackRadiusBonus;
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
