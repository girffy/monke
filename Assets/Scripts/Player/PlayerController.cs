using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        public float MoveSpeed = 4.5f;

        [Header("Dash")]
        public float DashSpeed = 16f;
        public float DashDuration = 0.18f;
        public float DashCooldown = 2.4f;
        public float DashInvulnerabilitySeconds = 0.45f;

        // Tech tree. Barging through the crowd used to be free; it is now the
        // "Barge" node, so the early dash is an escape you have to aim
        // through gaps rather than a straight line through a hundred men.
        public bool DashPassesThrough;      // "Barge"
        public float DashDamage;            // "Freight Train"
        public bool DashRefreshesAttacks;   // "Momentum"

        // Movement happens on the flat XZ ground plane; Y stays constant.
        public Vector3 FacingDirection { get; private set; } = Vector3.forward;
        public bool IsDashing { get; private set; }

        // Set by PlayerAttack while the slam animation plays — root motion
        // stops (can't move/dash) but input is still read so nothing feels
        // stuck once it releases.
        public bool MovementLocked { get; set; }

        // Set by abilities (e.g. Charge) that drive the Rigidbody velocity
        // themselves for a short window — normal movement/dash velocity
        // assignment is skipped while this is true.
        public bool IsExternallyControlled { get; set; }

        // Set by an attack that has committed to a direction (the LMB swipe)
        // so aim tracking can't spin the model away from the swing that is
        // already playing. Movement is unaffected — this is facing only.
        public bool FacingLocked { get; set; }

        // Temporary movement-speed scale owned by whatever ability is
        // running ("Rolling Thunder" walks the gorilla at half speed through
        // a chest beat instead of rooting it). Always reset to 1 when the
        // ability ends.
        public float SpeedScale { get; set; } = 1f;

        Rigidbody _rb;
        Collider _collider;
        PlayerHealth _health;
        PlayerStats _stats;
        PlayerAttack _attackCache;
        Abilities.ChestBeatAbility _chestBeatCache;
        Transform _model;

        // Lazily resolved instead of cached in Awake: GameBootstrap adds
        // PlayerController before PlayerAttack, so an Awake-time
        // GetComponent<PlayerAttack>() call here would always find nothing
        // and silently disable dash-cancels-attack forever (the same class
        // of add-order bug EnemyAI/EnemyHealth hit).
        PlayerAttack Attack => _attackCache != null ? _attackCache : (_attackCache = GetComponent<PlayerAttack>());
        Abilities.ChestBeatAbility ChestBeat => _chestBeatCache != null ? _chestBeatCache : (_chestBeatCache = GetComponent<Abilities.ChestBeatAbility>());

        Vector3 _moveInput;
        float _dashEndTime;
        float _dashReadyTime;
        Vector3 _dashDirection;

        bool _dashBuffered;
        Vector3 _dashBufferedDirection;
        bool _wasMovementLocked;

        void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _health = GetComponent<PlayerHealth>();
            _stats = GetComponent<PlayerStats>();
            _model = transform.Find("GorillaModel");

            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                _moveInput = Vector3.zero;
                return;
            }

            ReadInput();
            bool dashPressed = WasDashPressed();

            // "Rolling Thunder" leaves the player mobile through a chest
            // beat, so MovementLocked alone no longer means "an ability is
            // running" — without this, dashing mid-beat would dash and leave
            // the beat playing on top of it.
            bool abilityRunning = MovementLocked || (ChestBeat != null && ChestBeat.IsBeating);

            if (abilityRunning && dashPressed)
            {
                // A dash press cuts a rooted animation short (its damage
                // still lands instead of being lost) and dashes immediately.
                //
                // The cooldown is checked BEFORE cancelling: cancelling first
                // and then finding the dash unavailable threw the ability
                // away for nothing, which is easy to hit because a chest
                // beat usually follows a dash and the dash is still cooling
                // down when you try to cancel out of it. If there's no dash
                // to be had, the press buffers instead.
                bool dashReady = Time.time >= _dashReadyTime;
                bool cancelled = dashReady
                    && ((Attack != null && Attack.TryCancelWithDash())
                        || (ChestBeat != null && ChestBeat.TryCancelWithDash()));

                if (cancelled)
                {
                    Vector3 dashDir = _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : FacingDirection;
                    _moveInput = dashDir;
                    StartDash();
                }
                else
                {
                    _dashBuffered = true;
                    _dashBufferedDirection = _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : FacingDirection;
                }
            }

            if (MovementLocked)
            {
                _moveInput = Vector3.zero;
            }
            else if (_wasMovementLocked && _dashBuffered)
            {
                _dashBuffered = false;
                if (Time.time >= _dashReadyTime)
                {
                    _moveInput = _dashBufferedDirection;
                    StartDash();
                }
            }
            else if (dashPressed && Time.time >= _dashReadyTime && _moveInput.sqrMagnitude > 0.01f)
            {
                StartDash();
            }

            _wasMovementLocked = MovementLocked;

            if (IsDashing)
            {
                if (DashDamage > 0f) DamageDashedThrough();

                if (Time.time >= _dashEndTime)
                {
                    IsDashing = false;
                    // Solid again. If the dash ended inside something, the
                    // physics engine pushes the gorilla back out over the
                    // next few steps rather than trapping it.
                    if (_collider != null) _collider.enabled = true;
                    _dashedThrough.Clear();

                    if (DashRefreshesAttacks)
                    {
                        Attack?.ReadyNow();
                    }
                }
            }

            if (_model != null && !MovementLocked && !FacingLocked)
            {
                // Twin-stick style: the gorilla always faces where you're
                // aiming (mouse / right stick), independent of movement, so
                // you can strafe while attacking in a different direction.
                Vector3 aim = GetAimDirection();
                if (aim.sqrMagnitude > 0.0001f)
                {
                    _model.rotation = Quaternion.LookRotation(aim, Vector3.up);
                }
            }
        }

        // Mouse (raycast onto the ground plane) or gamepad right stick;
        // falls back to last movement direction if neither gives a reading.
        // Shared by the model-facing above and by attack/ability aiming.
        public Vector3 GetAimDirection()
        {
            // On a touch screen there is no second stick to aim with, so the
            // gorilla aims itself at whatever is closest. Without this the
            // whole game is unplayable on a phone: every attack would fire
            // along the last direction walked.
            if (UI.TouchControls.Active)
            {
                Vector3 auto = NearestEnemyDirection();
                if (auto.sqrMagnitude > 0.0001f) return auto;
                return _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : FacingDirection;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                var stick = gp.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    return new Vector3(stick.x, 0f, stick.y).normalized;
                }
            }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse != null && cam != null)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
                var groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (groundPlane.Raycast(ray, out float dist))
                {
                    Vector3 hit = ray.GetPoint(dist);
                    Vector3 dir = hit - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f) return dir.normalized;
                }
            }

            return FacingDirection;
        }

        // Nearest living enemy on the ground plane, within a generous range.
        // Refreshed a few times a second rather than every call: the aim is
        // read by the facing code every frame and by every attack, and a
        // scene-wide scan at that rate in a hundred-enemy round is not free.
        static readonly Collider[] AimBuffer = new Collider[64];
        Vector3 _cachedAim;
        float _nextAimRefresh;

        Vector3 NearestEnemyDirection()
        {
            if (Time.time < _nextAimRefresh) return _cachedAim;
            _nextAimRefresh = Time.time + 0.12f;

            int count = Physics.OverlapSphereNonAlloc(transform.position, 14f, AimBuffer);
            float bestSqr = float.MaxValue;
            _cachedAim = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                if (AimBuffer[i].GetComponentInParent<Enemies.EnemyHealth>() == null) continue;

                Vector3 toEnemy = AimBuffer[i].transform.position - transform.position;
                toEnemy.y = 0f;
                float sqr = toEnemy.sqrMagnitude;
                if (sqr < 0.0001f || sqr >= bestSqr) continue;

                bestSqr = sqr;
                _cachedAim = toEnemy.normalized;
            }

            return _cachedAim;
        }

        void FixedUpdate()
        {
            if (IsExternallyControlled)
            {
                ConfineToArena();
                return;
            }

            float speed = MoveSpeed * (_stats != null ? _stats.MoveSpeedMultiplier : 1f) * SpeedScale;

            if (IsDashing)
            {
                _rb.linearVelocity = _dashDirection * DashSpeed;
            }
            else if (Time.time < _knockbackUntil)
            {
                // Decays to zero over the shove so it reads as a hit, not a
                // teleport — and input can't cancel it mid-way.
                float remaining = Mathf.Clamp01((_knockbackUntil - Time.time) / Mathf.Max(0.0001f, _knockbackDuration));
                _rb.linearVelocity = _knockbackVelocity * remaining;
            }
            else
            {
                _rb.linearVelocity = _moveInput * speed;
            }

            ConfineToArena();
        }

        // The arena wall is enforced here rather than with colliders, so it
        // holds even through a dash (which drops the player's collider) and
        // through Charge-style scripted movement.
        void ConfineToArena()
        {
            var arena = Environment.Arena.Instance;
            if (arena == null) return;

            Vector3 clamped = arena.ClampInside(_rb.position, 0.9f);
            if (clamped == _rb.position) return;

            _rb.position = clamped;

            // Strip the outward part of the velocity so the gorilla slides
            // along the wall instead of grinding into it.
            Vector3 outward = clamped - arena.Center;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f) return;

            outward.Normalize();
            Vector3 v = _rb.linearVelocity;
            float into = Vector3.Dot(v, outward);
            if (into > 0f) _rb.linearVelocity = v - outward * into;
        }

        void ReadInput()
        {
            Vector3 input = Vector3.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                var stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f) input = new Vector3(stick.x, 0f, stick.y);
            }

            var touch = UI.TouchControls.MoveInput;
            if (touch.sqrMagnitude > 0.01f) input = touch;

            _moveInput = Vector3.ClampMagnitude(input, 1f);
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = _moveInput.normalized;
            }
        }

        bool WasDashPressed()
        {
            var kb = Keyboard.current;
            bool kbDash = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame);

            var gp = Gamepad.current;
            bool gpDash = gp != null && gp.buttonSouth.wasPressedThisFrame;

            return kbDash || gpDash || UI.TouchControls.ConsumePress(UI.TouchButton.Dash);
        }

        // "Freight Train": everything the dash passes through takes a hit,
        // once each. Tracked in a set because this runs every frame of the
        // dash and the gorilla overlaps the same body for several of them.
        readonly System.Collections.Generic.HashSet<GorillaSurvivors.Enemies.EnemyHealth> _dashedThrough
            = new System.Collections.Generic.HashSet<GorillaSurvivors.Enemies.EnemyHealth>();
        static readonly Collider[] DashHitBuffer = new Collider[32];

        void DamageDashedThrough()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.1f, DashHitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemy = DashHitBuffer[i].GetComponentInParent<GorillaSurvivors.Enemies.EnemyHealth>();
                if (enemy == null || !_dashedThrough.Add(enemy)) continue;

                float damage = DashDamage * (_stats != null ? _stats.DamageMultiplier : 1f);
                enemy.TakeDamage(damage, _dashDirection, 6f);
            }
        }

        void StartDash()
        {
            IsDashing = true;
            _dashedThrough.Clear();
            // With "Barge", the dash goes THROUGH the crowd: dropping the
            // collider also means trees and rocks don't block the escape,
            // and whatever the gorilla lands inside of, physics shoves it
            // clear of afterwards. Without the node the dash is a normal
            // move and can be walled in by bodies.
            if (_collider != null && DashPassesThrough) _collider.enabled = false;
            _dashDirection = _moveInput.normalized;
            _dashEndTime = Time.time + DashDuration;
            _dashReadyTime = Time.time + DashCooldown * (_stats != null ? _stats.AbilityCooldownMultiplier : 1f);
            // Always cover at least the full dash — iframes are the point of
            // dashing through a crowd, not an accidental side effect.
            // Cover the whole dash plus a landing buffer. Invulnerability
            // that expires mid-slide means dashing into a crowd still trades
            // a hit, which defeats the point of dashing through one.
            _health.GrantInvulnerability(Mathf.Max(DashInvulnerabilitySeconds, DashDuration + 0.2f));
            Sfx.Dash(transform.position);

            // Kick up dust where the gorilla pushed off. The lean into the
            // dash comes for free from CharacterAnimator, which reads the
            // (now very high) velocity.
            var puff = Blocky3DArt.SwipeDisc(new Color(0.68f, 0.62f, 0.50f));
            puff.transform.position = transform.position + Vector3.up * 0.05f - _dashDirection * 0.3f;
            puff.transform.localScale = new Vector3(0.25f, 0.02f, 0.25f);
            puff.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(1.5f, 0.28f);
        }

        Vector3 _knockbackVelocity;
        float _knockbackUntil;
        float _knockbackDuration;

        public void ApplyKnockback(Vector3 velocity, float duration)
        {
            // A dash or a rooted attack/ability owns movement; a hit landing
            // then (only possible at the very edge of i-frames) shouldn't
            // drag the player out of it.
            if (IsDashing || MovementLocked || IsExternallyControlled) return;

            _knockbackVelocity = velocity;
            _knockbackDuration = duration;
            _knockbackUntil = Time.time + duration;
        }

        public float DashCooldownRemaining01()
        {
            float total = DashCooldown * (_stats != null ? _stats.AbilityCooldownMultiplier : 1f);
            float remaining = Mathf.Max(0f, _dashReadyTime - Time.time);
            return total <= 0f ? 0f : remaining / total;
        }
    }
}
