using System;
using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Player
{
    // Tracks XP/leveling, temporary powerup buffs, and permanent round-reward
    // bonuses (picked between rounds). Leveling itself is automatic (flat
    // per-level scaling + a full heal); round rewards are player choices.
    public class PlayerStats : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public float CurrentXP { get; private set; }
        public float XPToNextLevel { get; private set; } = 10f;

        public float DamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        // Permanent-for-the-run bonuses granted by round-reward choices.
        public float PermanentDamageBonus { get; private set; }
        public float PermanentAttackSpeedBonus { get; private set; }
        public float PermanentMoveSpeedBonus { get; private set; }

        public event Action<int> OnLevelUp;
        public event Action<float, float> OnXPChanged; // current, needed

        PlayerHealth _health;

        float _damageBuffUntil, _attackSpeedBuffUntil, _moveSpeedBuffUntil;
        float _damageBuffAmount, _attackSpeedBuffAmount, _moveSpeedBuffAmount;

        void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            bool changed = false;
            if (_damageBuffUntil > 0f && Time.time > _damageBuffUntil) { _damageBuffUntil = 0f; changed = true; }
            if (_attackSpeedBuffUntil > 0f && Time.time > _attackSpeedBuffUntil) { _attackSpeedBuffUntil = 0f; changed = true; }
            if (_moveSpeedBuffUntil > 0f && Time.time > _moveSpeedBuffUntil) { _moveSpeedBuffUntil = 0f; changed = true; }
            if (changed) RecomputeMultipliers();
        }

        public void AddXP(float amount)
        {
            CurrentXP += amount;
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                LevelUp();
            }
            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
        }

        void LevelUp()
        {
            Level++;
            XPToNextLevel *= 1.25f;

            if (_health != null)
            {
                _health.SetMaxHP(_health.MaxHP + 10f, healToFull: true);
            }

            Sfx.LevelUp();

            OnLevelUp?.Invoke(Level);
        }

        // Permanent-for-run per-level scaling, read by PlayerAttack/PlayerController.
        public float LevelDamageBonus => 1f + (Level - 1) * 0.12f;
        public float LevelAttackRadiusBonus => 1f + (Level - 1) * 0.06f;

        public void ApplyTemporaryDamageBuff(float multiplierAdd, float seconds)
        {
            _damageBuffAmount = Mathf.Max(_damageBuffAmount, multiplierAdd);
            _damageBuffUntil = Mathf.Max(_damageBuffUntil, Time.time + seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryAttackSpeedBuff(float multiplierAdd, float seconds)
        {
            _attackSpeedBuffAmount = Mathf.Max(_attackSpeedBuffAmount, multiplierAdd);
            _attackSpeedBuffUntil = Mathf.Max(_attackSpeedBuffUntil, Time.time + seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryMoveSpeedBuff(float multiplierAdd, float seconds)
        {
            _moveSpeedBuffAmount = Mathf.Max(_moveSpeedBuffAmount, multiplierAdd);
            _moveSpeedBuffUntil = Mathf.Max(_moveSpeedBuffUntil, Time.time + seconds);
            RecomputeMultipliers();
        }

        public void AddPermanentDamageBonus(float amount)
        {
            PermanentDamageBonus += amount;
            RecomputeMultipliers();
        }

        public void AddPermanentAttackSpeedBonus(float amount)
        {
            PermanentAttackSpeedBonus += amount;
            RecomputeMultipliers();
        }

        public void AddPermanentMoveSpeedBonus(float amount)
        {
            PermanentMoveSpeedBonus += amount;
            RecomputeMultipliers();
        }

        public void AddPermanentMaxHP(float amount)
        {
            _health?.SetMaxHP(_health.MaxHP + amount, healToFull: true);
        }

        void RecomputeMultipliers()
        {
            DamageMultiplier = 1f + PermanentDamageBonus + (_damageBuffUntil > 0f ? _damageBuffAmount : 0f);
            AttackSpeedMultiplier = 1f + PermanentAttackSpeedBonus + (_attackSpeedBuffUntil > 0f ? _attackSpeedBuffAmount : 0f);
            MoveSpeedMultiplier = 1f + PermanentMoveSpeedBonus + (_moveSpeedBuffUntil > 0f ? _moveSpeedBuffAmount : 0f);
        }
    }
}
