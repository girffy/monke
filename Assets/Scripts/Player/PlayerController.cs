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
        public float DashCooldown = 1.2f;
        public float DashInvulnerabilitySeconds = 0.25f;

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

        Rigidbody _rb;
        PlayerHealth _health;
        PlayerStats _stats;
        Transform _model;

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

            if (MovementLocked)
            {
                // Buffer a dash press during the attack animation so it fires
                // the instant the animation releases, instead of requiring a
                // second press timed just right.
                if (dashPressed)
                {
                    _dashBuffered = true;
                    _dashBufferedDirection = _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : FacingDirection;
                }
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

            if (IsDashing && Time.time >= _dashEndTime)
            {
                IsDashing = false;
            }

            if (_model != null && !MovementLocked && _moveInput.sqrMagnitude > 0.01f)
            {
                _model.rotation = Quaternion.LookRotation(_moveInput.normalized, Vector3.up);
            }
        }

        void FixedUpdate()
        {
            if (IsExternallyControlled) return;

            float speed = MoveSpeed * (_stats != null ? _stats.MoveSpeedMultiplier : 1f);

            if (IsDashing)
            {
                _rb.linearVelocity = _dashDirection * DashSpeed;
            }
            else
            {
                _rb.linearVelocity = _moveInput * speed;
            }
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

            return kbDash || gpDash;
        }

        void StartDash()
        {
            IsDashing = true;
            _dashDirection = _moveInput.normalized;
            _dashEndTime = Time.time + DashDuration;
            _dashReadyTime = Time.time + DashCooldown;
            // Always cover at least the full dash — iframes are the point of
            // dashing through a crowd, not an accidental side effect.
            _health.GrantInvulnerability(Mathf.Max(DashInvulnerabilitySeconds, DashDuration));
            Sfx.Dash(transform.position);
        }

        public float DashCooldownRemaining01()
        {
            float total = DashCooldown;
            float remaining = Mathf.Max(0f, _dashReadyTime - Time.time);
            return total <= 0f ? 0f : remaining / total;
        }
    }
}
