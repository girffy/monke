using System;
using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        public float MaxHP { get; private set; } = 100f;
        public float CurrentHP { get; private set; }
        public bool IsInvulnerable => _invulnerableUntil > Time.time;
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDeath;

        // Tech tree: "Scarred" stacks multiplicatively rather than adding, so
        // three ranks of -12% can never reach immunity.
        public float DamageTakenMultiplier { get; private set; } = 1f;
        public void AddDamageReduction(float fraction) => DamageTakenMultiplier *= 1f - fraction;

        // Tech tree: "Old Wounds Close".
        public float RegenPerSecondFraction;

        float _invulnerableUntil;
        bool _dead;

        void Awake()
        {
            CurrentHP = MaxHP;
        }

        public void SetMaxHP(float newMax, bool healToFull = false)
        {
            MaxHP = newMax;
            if (healToFull) CurrentHP = MaxHP;
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void Heal(float amount)
        {
            if (_dead) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        // Only for the debug toggle — GrantInvulnerability can never shorten
        // an existing window, which is correct everywhere else.
        public void ClearInvulnerability() => _invulnerableUntil = 0f;

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        // A hit grants brief invulnerability and shoves the gorilla away from
        // whatever landed it. Without both, standing in a crowd meant every
        // body in contact ticked damage independently and the player melted
        // before they could react; the i-frames make a mob hit you once, and
        // the knockback is what gets you out of the pile.
        public const float HitInvulnerabilitySeconds = 0.5f;
        const float KnockbackSpeed = 9f;
        const float KnockbackDuration = 0.14f;

        public void TakeDamage(float amount, Vector3? sourcePosition)
        {
            if (_dead || IsInvulnerable) return;

            amount *= DamageTakenMultiplier;
            CurrentHP -= amount;
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            DamagePopup.Spawn(transform.position, amount);
            Sfx.Hurt(transform.position);

            if (CurrentHP <= 0f)
            {
                _dead = true;
                SetModelVisible(true);
                OnDeath?.Invoke();
                return;
            }

            GrantInvulnerability(HitInvulnerabilitySeconds);
            _hitFlashUntil = Time.time + HitInvulnerabilitySeconds;

            if (sourcePosition.HasValue)
            {
                Vector3 away = transform.position - sourcePosition.Value;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f) away = UnityEngine.Random.insideUnitSphere;
                away.y = 0f;
                GetComponent<PlayerController>()?.ApplyKnockback(away.normalized * KnockbackSpeed, KnockbackDuration);
            }
        }

        float _hitFlashUntil;
        Renderer[] _modelRenderers;
        bool _modelVisible = true;

        // Blink while hit-invulnerable so the i-frames are readable. Dash
        // i-frames deliberately don't blink — the dash is its own tell.
        void Update()
        {
            if (!_dead && RegenPerSecondFraction > 0f && CurrentHP < MaxHP
                && (GameManager.Instance == null || !GameManager.Instance.IsPaused))
            {
                Heal(MaxHP * RegenPerSecondFraction * Time.deltaTime);
            }

            bool flashing = !_dead && Time.time < _hitFlashUntil;
            SetModelVisible(!flashing || Mathf.Repeat(Time.time, 0.12f) < 0.07f);
        }

        void SetModelVisible(bool visible)
        {
            if (visible == _modelVisible) return;
            _modelVisible = visible;

            if (_modelRenderers == null)
            {
                var model = transform.Find("GorillaModel");
                _modelRenderers = model != null ? model.GetComponentsInChildren<Renderer>() : new Renderer[0];
            }
            foreach (var r in _modelRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
