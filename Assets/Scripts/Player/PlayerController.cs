using UnityEngine;
using UnityEngine.InputSystem;

namespace GorillaSurvivors.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
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

        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public bool IsDashing { get; private set; }

        Rigidbody2D _rb;
        PlayerHealth _health;
        PlayerStats _stats;
        SpriteRenderer _renderer;

        Vector2 _moveInput;
        float _dashEndTime;
        float _dashReadyTime;
        Vector2 _dashDirection;

        void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<PlayerHealth>();
            _stats = GetComponent<PlayerStats>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            ReadInput();

            if (WasDashPressed() && Time.time >= _dashReadyTime && _moveInput.sqrMagnitude > 0.01f)
            {
                StartDash();
            }

            if (IsDashing && Time.time >= _dashEndTime)
            {
                IsDashing = false;
            }

            if (_renderer != null && Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _renderer.flipX = _moveInput.x < 0f;
            }
        }

        void FixedUpdate()
        {
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
            Vector2 input = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                var stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f) input = stick;
            }

            _moveInput = Vector2.ClampMagnitude(input, 1f);
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
            _health.GrantInvulnerability(DashInvulnerabilitySeconds);
        }

        public float DashCooldownRemaining01()
        {
            float total = DashCooldown;
            float remaining = Mathf.Max(0f, _dashReadyTime - Time.time);
            return total <= 0f ? 0f : remaining / total;
        }
    }
}
