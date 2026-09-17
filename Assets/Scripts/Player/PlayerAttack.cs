using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Environment;

namespace GorillaSurvivors.Player
{
    // Active attack: press J/Enter (or click, or the gamepad attack button) to
    // ground-slam a zone in front of the gorilla. Plays a short, rooted
    // animation — no moving/dashing until it finishes.
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        public float BaseDamage = 22f;
        // A CIRCLE centred on the gorilla, not an arc. This is a two-handed
        // overhead smash into the ground — the force goes out in every
        // direction from the point of impact, so a wedge in front was the
        // wrong shape for it. The swipe keeps the arc; that one really is a
        // swung arm.
        public float Radius = 1.52f;
        // How far forward the circle sits, in METRES.
        //
        // This used to be a fraction of the radius, which meant every area
        // bonus shoved the whole circle away from the gorilla as it grew —
        // so a bigger slam covered different ground rather than more of it,
        // and the thing standing on your toes that the small slam would have
        // hit fell out of the back of the big one. The fists land where the
        // arms reach, which is a fixed distance, whatever the blow's size.
        public const float CenterOffset = 0.68f;
        public float BaseCooldown = 0.48f;
        public const float SlamDuration = 0.5f;

        // Tech tree.
        public float StunSeconds;      // "Concussive"
        public bool ChargeEnabled;     // "Wind Up"
        public bool CoreImpactEnabled; // "Focal Impact"
        public float ChargeDamageReduction; // "Braced"

        // Everything under the gorilla's fists takes extra. This is the
        // inner THIRD of the circle, marked out on the ground both when the
        // blow lands and while it is being charged, so the reward for
        // standing on top of what you are hitting is something you can see
        // and aim rather than a hidden number.
        public const float CoreFraction = 1f / 3f;
        public const float CoreDamageBonus = 0.5f;

        // How long holding RMB can build for, and what a full hold is worth.
        //
        // Charging grows the blow's REACH, not its damage. A 2.2x damage
        // multiplier on an attack that already one-shots most things was
        // worth holding for every single time, which made the uncharged slam
        // the wrong move and the charge a tax rather than a choice. Area is
        // a decision instead: hold to catch the crowd, tap to hit the one in
        // front of you now.
        public const float MaxChargeTime = 1.1f;
        public const float MaxChargeReach = 1.4f;

        PlayerStats _stats;
        PlayerController _controller;
        CharacterAnimator _animator;
        QuickSwipeAttack _swipeCache;
        Transform _armL;
        Transform _armR;
        float _nextAttackReadyTime;
        bool _isSlamming;
        bool _hitLanded;
        Vector3 _pendingAimDirection;
        Coroutine _slamCoroutine;

        bool _attackBuffered;
        Vector3 _bufferedAimDirection;
        bool _wasDashing;

        public bool IsSlamming => _isSlamming;

        // Lazy for the same reason PlayerController.Attack is: add order
        // between the two attack components isn't guaranteed either way.
        QuickSwipeAttack Swipe => _swipeCache != null ? _swipeCache : (_swipeCache = GetComponent<QuickSwipeAttack>());

        PlayerPerks _perksCache;
        PlayerPerks Perks => _perksCache != null ? _perksCache : (_perksCache = GetComponent<PlayerPerks>());

        static readonly Collider[] HitBuffer = new Collider[32];

        public float AttackCooldownRemaining01()
        {
            float total = BaseCooldown * _stats.AbilityCooldownMultiplier / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier) + SlamDuration;
            float remaining = Mathf.Max(0f, _nextAttackReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        // Arm poses as local directions the hanging arm points in (relative to
        // the shoulder pivot, which itself faces the attack direction) —
        // rest hangs straight down; windup raises the arms up and forward
        // (in front of the head); slam drives them forward-and-down into
        // the ground, like a two-handed overhead smash.
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 WindupDir = new Vector3(0f, 0.8f, 0.5f).normalized;
        static readonly Vector3 SlamDir = new Vector3(0f, -0.55f, 0.85f).normalized;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _armL = model != null ? model.Find("ArmL") : null;
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (_isSlamming) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            bool isDashingNow = _controller.IsDashing;

            // A dash finishing releases any attack buffered while it was
            // playing, regardless of whether the normal cooldown has
            // elapsed yet — the cooldown already started the moment the
            // press was buffered below.
            if (_wasDashing && !isDashingNow && _attackBuffered)
            {
                _attackBuffered = false;
                _wasDashing = isDashingNow;

                // Still holding the button as the dash lands: roll straight
                // into a charge rather than firing the tap you buffered.
                // Dash-then-charge is the natural opener and it used to be
                // impossible — the buffered press always fired instantly.
                if (ChargeEnabled && IsAttackHeld()) BeginCharge();
                else _slamCoroutine = StartCoroutine(SlamSequence(_bufferedAimDirection));
                return;
            }
            _wasDashing = isDashingNow;

            if (Swipe != null && Swipe.IsSwiping) return;

            // "Wind Up": holding the button builds the blow and releasing
            // throws it. The gorilla is rooted while charging — this is the
            // committed attack, and being able to walk around at full speed
            // holding a 2.2x hit would make it strictly better than tapping.
            // A dash still cancels out of it (TryCancelWithDash).
            if (_charging)
            {
                // A full charge HOLDS at maximum instead of auto-firing. It
                // used to release itself the moment the meter filled, which
                // tore the pose out from under the player mid-hold and read
                // as the animation glitching.
                if (IsAttackHeld())
                {
                    HoldChargePose();
                    return;
                }
                ReleaseCharge();
                return;
            }

            if (Time.time < _nextAttackReadyTime) return;
            if (!WasAttackPressed()) return;

            float cooldown = BaseCooldown * _stats.AbilityCooldownMultiplier / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier);
            _nextAttackReadyTime = Time.time + cooldown + SlamDuration;

            Vector3 aimDirection = _controller.GetAimDirection();
            aimDirection.y = 0f;
            aimDirection.Normalize();

            if (isDashingNow)
            {
                // Can't play the slam animation mid-dash — commit to it
                // (cooldown starts now) and fire it the instant the dash ends.
                _attackBuffered = true;
                _bufferedAimDirection = aimDirection;
            }
            else if (ChargeEnabled)
            {
                BeginCharge();
            }
            else
            {
                _slamCoroutine = StartCoroutine(SlamSequence(aimDirection));
            }
        }

        bool _charging;
        float _chargeStart;
        GameObject _chargeRim;
        GameObject _chargeFill;
        GameObject _chargeCore;

        public bool IsCharging => _charging;

        // 0 at a tap, 1 at a full hold. Also drives the HUD's charge readout.
        public float Charge01 => Mathf.Clamp01((Time.time - _chargeStart) / MaxChargeTime);

        void BeginCharge()
        {
            _charging = true;
            _chargeStart = Time.time;
            _controller.MovementLocked = true;
            if (_animator != null)
            {
                _animator.SuppressArms = true;
                _animator.StandUpright = true;
            }

            // The same readout the bomber's fuse uses, for the same reason:
            // a ring at the full size with the inside filling outward is
            // legible at a glance and says "how much" and "how far" at once.
            _chargeRim = Blocky3DArt.SwipeDisc(new Color(0.55f, 0.50f, 0.32f));
            _chargeFill = Blocky3DArt.SwipeDisc(new Color(1f, 0.86f, 0.35f));
            // The bonus zone is drawn while you hold, not only when you land
            // — a bonus for standing on top of something is only worth
            // having if you can aim it before committing.
            if (CoreImpactEnabled) _chargeCore = Blocky3DArt.SwipeDisc(new Color(1f, 0.62f, 0.18f));
        }

        // Where the held slam is currently pointed, flattened. The gorilla
        // turns to face this while charging, so his own forward is the
        // honest answer.
        Vector3 ChargeAim()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.sqrMagnitude < 0.0001f ? Vector3.forward : dir.normalized;
        }

        // Arms cocked overhead, rising with the charge, so the size of the
        // blow you are holding is visible before you throw it.
        void HoldChargePose()
        {
            float c = Charge01;
            SetArmDirection(Vector3.Slerp(RestDir, WindupDir, Mathf.Clamp01(c * 2.5f)));
            if (_animator != null)
            {
                _animator.BodyHeightOffset = Mathf.Lerp(0f, 0.17f, c);
                _animator.BodyPitch = Mathf.Lerp(0f, -12f, c);
            }

            // The ring shows the reach a FULL charge will have, so holding
            // longer visibly fills toward the area it is going to cover.
            float full = Radius * _stats.LevelAttackRadiusBonus * _stats.AreaMultiplier * MaxChargeReach;
            // Sits where the blow will actually land — forward of the body,
            // same as the hit test — so the preview isn't promising an area
            // the slam won't cover.
            Vector3 center = transform.position + ChargeAim() * CenterOffset;
            if (_chargeRim != null)
            {
                _chargeRim.transform.position = center + Vector3.up * 0.05f;
                _chargeRim.transform.localScale = new Vector3(full * 2f, 0.02f, full * 2f);
            }
            if (_chargeFill != null)
            {
                float filled = full * 2f * c;
                _chargeFill.transform.position = center + Vector3.up * 0.07f;
                _chargeFill.transform.localScale = new Vector3(filled, 0.02f, filled);
            }
            if (_chargeCore != null)
            {
                // Tracks the radius the blow has ACTUALLY reached, not the
                // full-charge one, so it reads as the bonus zone growing
                // under your hands rather than a fixed target.
                float now = Radius * _stats.LevelAttackRadiusBonus * _stats.AreaMultiplier
                            * Mathf.Lerp(1f, MaxChargeReach, c) * CoreFraction * 2f;
                _chargeCore.transform.position = center + Vector3.up * 0.09f;
                _chargeCore.transform.localScale = new Vector3(now, 0.02f, now);
            }
        }

        void ClearChargeVisual()
        {
            if (_chargeRim != null) Destroy(_chargeRim);
            if (_chargeFill != null) Destroy(_chargeFill);
            if (_chargeCore != null) Destroy(_chargeCore);
            _chargeRim = null;
            _chargeFill = null;
            _chargeCore = null;
        }

        void ReleaseCharge()
        {
            float charge = Charge01;
            _charging = false;
            ClearChargeVisual();

            Vector3 aim = _controller.GetAimDirection();
            aim.y = 0f;
            aim.Normalize();

            // fromWindup: the charge already held the arms up, so the swing
            // carries on from where they are instead of snapping back to rest
            // and winding up a second time.
            _slamCoroutine = StartCoroutine(SlamSequence(aim, charge, fromWindup: true));
        }

        // "Momentum": a finished dash clears the slam's recovery outright.
        public void ReadyNow() => _nextAttackReadyTime = 0f;

        // Called by PlayerController when a dash is pressed mid-attack.
        // Cuts the animation short — but the hit still lands right away if
        // it hasn't already, so canceling never costs the player the damage,
        // just the recovery time.
        public bool TryCancelWithDash()
        {
            // Dashing out of a charge throws whatever was built rather than
            // losing it, same as dashing out of the swing itself.
            if (_charging)
            {
                float built = Charge01;
                _charging = false;
                ClearChargeVisual();
                PerformSlamHit(_controller.GetAimDirection(), built);
                SetArmDirection(RestDir);
                if (_animator != null)
                {
                    _animator.SuppressArms = false;
                    _animator.StandUpright = false;
                    _animator.BodyHeightOffset = 0f;
                    _animator.BodyPitch = 0f;
                }
                _controller.MovementLocked = false;
                return true;
            }

            if (!_isSlamming) return false;

            if (_slamCoroutine != null) StopCoroutine(_slamCoroutine);

            if (!_hitLanded)
            {
                PerformSlamHit(_pendingAimDirection);
            }

            SetArmDirection(RestDir);
            _isSlamming = false;
            _hitLanded = false;
            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.StandUpright = false;
                _animator.BodyHeightOffset = 0f;
                _animator.BodyPitch = 0f;
            }
            _controller.MovementLocked = false;
            return true;
        }

        // Right mouse / right trigger: the heavy, committed attack sits on
        // the secondary button, with the fast poke on the primary one.
        bool WasAttackPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.jKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.rightTrigger.wasPressedThisFrame) return true;

            return UI.TouchControls.ConsumePress(UI.TouchButton.Slam);
        }

        // Whether the attack input is still down, for the charge.
        bool IsAttackHeld()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.jKey.isPressed || kb.enterKey.isPressed)) return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.rightTrigger.isPressed) return true;

            return UI.TouchControls.Held(UI.TouchButton.Slam);
        }

        IEnumerator SlamSequence(Vector3 aimDirection, float charge01 = 0f, bool fromWindup = false)
        {
            _isSlamming = true;
            _hitLanded = false;
            _pendingAimDirection = aimDirection;
            _controller.MovementLocked = true;
            if (_animator != null)
            {
                _animator.SuppressArms = true;
                // An overhead two-handed smash needs both hands free, so the
                // gorilla rears up off its knuckles for it.
                _animator.StandUpright = true;
            }

            var model = transform.Find("GorillaModel");
            var lockedRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            if (model != null) model.rotation = lockedRotation;

            // Three beats: rear up and back (anticipation), drive down fast
            // (the hit lands on the frame the arms bottom out), then a slower
            // settle. The body rears with the arms and drops with them, which
            // is what sells the weight — arms alone read as a wave.
            // The rotation is reasserted every frame throughout so nothing
            // else (e.g. PlayerController's aim tracking, if MovementLocked
            // ever lags a frame) can drift it mid-swing.
            const float windup = 0.17f;
            const float slam = 0.07f;
            const float recover = SlamDuration - windup - slam;

            if (fromWindup)
            {
                // Released out of a charge. The arms are already up and the
                // body is already reared, so this picks the swing up from
                // exactly where the pose left off — replaying the wind-up
                // from rest snapped the arms back down and then raised them
                // again, which is what made a released charge look broken.
                yield return AnimateArms(_armDirection, SlamDir, slam + 0.03f, model, lockedRotation,
                    _animator != null ? _animator.BodyHeightOffset : 0f, -0.16f,
                    _animator != null ? _animator.BodyPitch : 0f, 14f);
            }
            else
            {
                yield return AnimateArms(RestDir, WindupDir, windup, model, lockedRotation, 0f, 0.13f, 0f, -9f);
                yield return AnimateArms(WindupDir, SlamDir, slam, model, lockedRotation, 0.13f, -0.16f, -9f, 14f);
            }

            PerformSlamHit(aimDirection, charge01);
            _hitLanded = true;
            CameraShake.Shake(0.22f, 0.22f);

            yield return AnimateArms(SlamDir, RestDir, recover, model, lockedRotation, -0.16f, 0f, 14f, 0f);

            _isSlamming = false;
            _hitLanded = false;
            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.StandUpright = false;
                _animator.BodyHeightOffset = 0f;
                _animator.BodyPitch = 0f;
            }
            _controller.MovementLocked = false;
        }

        IEnumerator AnimateArms(Vector3 fromDir, Vector3 toDir, float duration, Transform model, Quaternion lockedRotation,
            float fromHeight = 0f, float toHeight = 0f, float fromPitch = 0f, float toPitch = 0f)
        {
            if (duration <= 0f) yield break;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                SetArmDirection(Vector3.Slerp(fromDir, toDir, p));
                if (_animator != null)
                {
                    _animator.BodyHeightOffset = Mathf.Lerp(fromHeight, toHeight, p);
                    _animator.BodyPitch = Mathf.Lerp(fromPitch, toPitch, p);
                }
                if (model != null) model.rotation = lockedRotation;
                yield return null;
            }
            SetArmDirection(toDir);
        }

        // Where the arms are pointing right now, so a swing released out of a
        // charge can continue from the live pose rather than a fixed one.
        Vector3 _armDirection = Vector3.down;

        void SetArmDirection(Vector3 localDirection)
        {
            _armDirection = localDirection;
            var rot = Quaternion.FromToRotation(Vector3.down, localDirection);
            if (_armL != null) _armL.localRotation = rot;
            if (_armR != null) _armR.localRotation = rot;
        }

        void PerformSlamHit(Vector3 aimDirection, float charge01 = 0f)
        {
            aimDirection.y = 0f;
            aimDirection.Normalize();

            float perkMultiplier = Perks != null ? Perks.MeleeDamageMultiplier : 1f;
            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier * perkMultiplier;
            // Charging buys AREA, nothing else.
            float radius = Radius * _stats.LevelAttackRadiusBonus * _stats.AreaMultiplier
                           * Mathf.Lerp(1f, MaxChargeReach, Mathf.Clamp01(charge01));
            float coreRadius = radius * CoreFraction;
            // Pushed out in front rather than centred on the gorilla. His
            // fists land ahead of the body, so a circle on the body's centre
            // spent half its area behind him where nothing was ever hit, and
            // fell short of what he was visibly reaching for.
            Vector3 hitCenter = transform.position + aimDirection * CenterOffset;

            int count = Physics.OverlapSphereNonAlloc(hitCenter, radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Vector3 knockDir = enemyHealth.transform.position - hitCenter;
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude < 0.0001f) knockDir = aimDirection;

                    // "Focal Impact": directly under the fists hits harder.
                    float blow = damage;
                    if (CoreImpactEnabled && knockDir.magnitude <= coreRadius) blow *= 1f + CoreDamageBonus;

                    enemyHealth.TakeDamage(blow, knockDir, 7f);
                    if (enemyHealth.CurrentHP <= 0f) Perks?.NotifyMeleeKill();
                    else if (StunSeconds > 0f) HitBuffer[i].GetComponentInParent<EnemyAI>()?.ApplyStun(StunSeconds);
                    continue;
                }

                var rock = HitBuffer[i].GetComponentInParent<AttackableRock>();
                if (rock != null && !rock.IsLaunched)
                {
                    float rockDashDistance = _controller.DashSpeed * _controller.DashDuration;
                    rock.Launch(aimDirection, damage * 2f, rockDashDistance * 1.5f);
                    continue;
                }

                // Felling a tree is the biggest single hit available — it
                // costs positioning (you have to fight next to one and line
                // the fall up), so it pays out several times a normal slam
                // to everything caught underneath.
                var tree = HitBuffer[i].GetComponentInParent<FellableTree>();
                if (tree != null && !tree.IsFelled)
                {
                    tree.Fell(aimDirection, damage * 4f);
                }
            }

            // Projectiles have no Collider (see Projectile.Spawn), so they
            // never show up in the OverlapSphere pass above — check the
            // registry directly instead.
            foreach (var projectile in Projectile.Active)
            {
                if (projectile == null) continue;
                if (Vector3.Distance(projectile.transform.position, hitCenter) <= radius)
                {
                    projectile.Deflect(aimDirection);
                }
            }

            SpawnSlamEffect(hitCenter, radius);
            if (CoreImpactEnabled) SpawnCoreEffect(hitCenter, coreRadius);
            Sfx.Slam(hitCenter);
        }

        // The bright inner disc, matching where the damage bonus applies.
        void SpawnCoreEffect(Vector3 center, float radius)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 0.82f, 0.30f));
            go.transform.position = center + Vector3.up * 0.09f;
            go.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            go.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(radius * 2f, 0.16f);
        }

        // A ring going out from where the fists land, matching the circular
        // hit test — including its offset forward.
        void SpawnSlamEffect(Vector3 center, float radius)
        {
            var go = Blocky3DArt.SwipeDisc(new Color(1f, 0.97f, 0.88f));
            go.transform.position = center + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            go.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(radius * 2f, 0.18f);
        }
    }
}
