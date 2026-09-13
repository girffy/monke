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

        public void TakeDamage(float amount)
        {
            if (_dead || IsInvulnerable) return;

            CurrentHP -= amount;
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            DamagePopup.Spawn(transform.position, amount);

            if (CurrentHP <= 0f)
            {
                _dead = true;
                OnDeath?.Invoke();
            }
        }
    }
}
