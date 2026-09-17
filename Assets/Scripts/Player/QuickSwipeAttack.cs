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
        // The swing itself: an arc drawn at Reach in front of the gorilla,
        // catching anything within BandWidth of it (see MeleeArc).
        //
        // Deliberately short and close, and the INNER edge is what matters:
        // the arc has to start at the gorilla's own body, or an enemy pressed
        // against him falls inside the ring and is missed by a swing that
        // visibly passed through it. At 1.15 with a 0.65 band the inside edge
        // sat at 0.5 — right at contact range, so a close enemy was a coin
        // flip. This covers about 0.2 to 1.8.
        public float Reach = 1.0f;
        // Pulled in from 105: a swipe should be what is in FRONT of you, and
        // at 105 it was already reaching round past your shoulders before
        // any upgrade widened it further.
        public float ArcDegrees = 60f;
        // Half-thickness of the swept band, either side of the arc.
        public float BandWidth = 0.8f;
        // Slowed 20% from 0.22: at the old speed the arm was a blur and the
        // swing read as a flicker rather than a swipe.
        public const float SwipeDuration = 0.264f;

        // Tech tree.
        public float BleedFraction;       // "Rake"
        public float SecondHitFraction;   // "Flurry"

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
            if (Attack != null && (Attack.IsSlamming || Attack.IsCharging)) return;
            if (!WasSwipePressed()) return;

            // The direction is fixed AT THE CLICK, not read again when the
            // hit lands two frames later. Reading it late meant a fast mouse
            // flick during the wind-up silently redirected a swing you had
            // already committed to, so the attack went somewhere you hadn't
            // aimed it — the swing you see is now always the swing you get.
            Vector3 aim = _controller.GetAimDirection();
            aim.y = 0f;
            aim.Normalize();

            StartCoroutine(SwipeSequence(aim));
        }

        // Left mouse / X / left trigger. buttonNorth is deliberately NOT used
        // here — that's the chest beat, and the two used to share it.
        bool WasSwipePressed()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonWest.wasPressedThisFrame || gp.leftTrigger.wasPressedThisFrame)) return true;

            return UI.TouchControls.ConsumePress(UI.TouchButton.Swipe);
        }

        IEnumerator SwipeSequence(Vector3 aim)
        {
            _isSwiping = true;
            if (_animator != null) _animator.SuppressArms = true;

            // The model is held to the committed direction for the duration
            // too, so the animation matches where the damage goes. Movement
            // is deliberately NOT locked — the fast poke stays usable while
            // repositioning; it's only the facing that commits.
            var model = transform.Find("GorillaModel");
            var lockedRotation = Quaternion.LookRotation(aim, Vector3.up);
            _controller.FacingLocked = true;
            if (model != null) model.rotation = lockedRotation;

            const float outT = SwipeDuration * 0.4f;
            const float hitT = SwipeDuration * 0.15f;
            const float backT = SwipeDuration - outT - hitT;

            // The body counter-rotates into the swing and unwinds out of it,
            // so a fast poke still reads as a whole-body motion rather than
            // one arm flapping.
            yield return AnimateArm(RestDir, SwipeStartDir, outT, model, lockedRotation, 0f, -14f);
            yield return AnimateArm(SwipeStartDir, SwipeEndDir, hitT, model, lockedRotation, -14f, 16f);

            PerformSwipeHit(aim);
            CameraShake.Shake(0.08f, 0.12f);

            yield return AnimateArm(SwipeEndDir, RestDir, backT, model, lockedRotation, 16f, 0f);

            // "Flurry": a back-handed return swing on the way out, landing
            // in the same committed direction.
            if (SecondHitFraction > 0f)
            {
                yield return AnimateArm(RestDir, SwipeEndDir, 0.06f, model, lockedRotation, 0f, 10f);
                PerformSwipeHit(aim, SecondHitFraction);
                yield return AnimateArm(SwipeEndDir, RestDir, 0.08f, model, lockedRotation, 10f, 0f);
            }

            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyYaw = 0f;
            }
            _controller.FacingLocked = false;
            _isSwiping = false;
        }

        IEnumerator AnimateArm(Vector3 fromDir, Vector3 toDir, float duration, Transform model, Quaternion lockedRotation,
            float fromYaw = 0f, float toYaw = 0f)
        {
            if (duration <= 0f) yield break;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                SetArmDirection(Vector3.Slerp(fromDir, toDir, p));
                if (_animator != null) _animator.BodyYaw = Mathf.Lerp(fromYaw, toYaw, p);
                if (model != null) model.rotation = lockedRotation;
                yield return null;
            }
            SetArmDirection(toDir);
        }

        void SetArmDirection(Vector3 localDirection)
        {
            if (_armR == null) return;
            _armR.localRotation = Quaternion.FromToRotation(Vector3.down, localDirection);
        }

        PlayerPerks _perksCache;
        PlayerPerks Perks => _perksCache != null ? _perksCache : (_perksCache = GetComponent<PlayerPerks>());

        void PerformSwipeHit(Vector3 aimDirection, float damageScale = 1f)
        {
            float perkMultiplier = Perks != null ? Perks.MeleeDamageMultiplier : 1f;
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier * damageScale * perkMultiplier;
            // Area bonuses THICKEN the swing; they do not push it out.
            //
            // Scaling the radius as well moved the whole ring away from the
            // gorilla, so +50% area made the one real failure worse: the
            // inner edge marched forward and close enemies — the ones a
            // panic swipe is for — fell through the middle of it. A bigger
            // area should never shrink what an attack covers, and growing
            // only the band means it never can.
            // Area buys DISTANCE, and only distance. The angle is the shape
            // of the attack — a swipe that widened with every buff stopped
            // being a swipe — so the sweep stays where the tech tree put it
            // and the band reaches further out.
            //
            // Critically the INNER edge is pinned. Scaling reach and band
            // together would march the near edge forward as well, which is
            // the old bug that made a bigger swipe miss the man standing on
            // your toes. Only the outer edge moves, so the covered ground
            // can grow but can never shrink.
            float grow = _stats.LevelAttackRadiusBonus * _stats.AreaMultiplier;
            float inner = Mathf.Max(0f, Reach - BandWidth);
            float outer = inner + (Reach + BandWidth - inner) * grow;

            float reach = (inner + outer) * 0.5f;
            float band = (outer - inner) * 0.5f;
            float arc = ArcDegrees;

            Vector3 hitCenter = transform.position + aimDirection * (reach * 0.5f);

            int count = MeleeArc.Overlap(transform.position, aimDirection, reach, arc, band, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Vector3 knockDir = enemyHealth.transform.position - hitCenter;
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude < 0.0001f) knockDir = aimDirection;
                    enemyHealth.TakeDamage(damage, knockDir, 4f);
                    // Checked straight after the hit: EnemyHealth drops its
                    // HP immediately and only destroys the object at end of
                    // frame, so this is the kill the swipe just made.
                    if (enemyHealth.CurrentHP <= 0f) Perks?.NotifyMeleeKill();
                    else if (BleedFraction > 0f) enemyHealth.ApplyDamageOverTime(damage * BleedFraction, 2f);
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

            // Projectiles have no Collider, so the overlap pass above never
            // sees them. This used to test a small sphere at the swing's
            // midpoint, which is nowhere near the band the swipe actually
            // covers — so batting a rock away with a claw swipe almost never
            // worked. Same shape as the hit test now.
            foreach (var projectile in Projectile.Active)
            {
                if (projectile == null) continue;
                if (MeleeArc.Contains(transform.position, aimDirection, reach, arc, band,
                        projectile.transform.position, 0.25f))
                {
                    projectile.Deflect(aimDirection);
                }
            }

            SpawnSwipeEffect(aimDirection, reach, arc, band);
            Sfx.Swipe(hitCenter);
        }

        // Draws the crescent the hit test just used, so what you see and what
        // you hit are the same shape.
        void SpawnSwipeEffect(Vector3 aimDirection, float reach, float arc, float band)
        {
            var go = Blocky3DArt.SwipeArc(new Color(0.95f, 0.88f, 0.35f), reach, arc, band);
            // Clear of the sand and of the ground decals that live at 0.04.
            go.transform.position = transform.position + Vector3.up * 0.07f;
            go.transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);

            StartCoroutine(FadeArc(go));
        }

        IEnumerator FadeArc(GameObject go)
        {
            const float duration = 0.14f;
            float t = 0f;
            var baseScale = go.transform.localScale;

            while (t < duration)
            {
                t += Time.deltaTime;
                // Grows a touch as it fades, which reads as follow-through.
                go.transform.localScale = baseScale * Mathf.Lerp(0.92f, 1.06f, t / duration);
                yield return null;
            }

            Destroy(go);
        }

    }
}
