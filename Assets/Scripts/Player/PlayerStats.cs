using System;
using System.Collections.Generic;
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
        // < 1 shortens cooldowns (Dash, Roar, Charge, the LMB slam — not the
        // RMB swipe, which has no real cooldown to reduce).
        public float AbilityCooldownMultiplier { get; private set; } = 1f;
        // > 1 grows AoE radii (the slam, swipe, Roar, Charge hit areas).
        public float AreaMultiplier { get; private set; } = 1f;
        // > 1 grows the range at which XP orbs are pulled in.
        public float PickupRadiusMultiplier { get; private set; } = 1f;

        // Permanent-for-the-run bonuses granted by round-reward choices.
        public float PermanentDamageBonus { get; private set; }
        public float PermanentAttackSpeedBonus { get; private set; }
        public float PermanentMoveSpeedBonus { get; private set; }
        public float PermanentCooldownReduction { get; private set; }
        public float PermanentAreaBonus { get; private set; }
        public float PermanentPickupRadiusBonus { get; private set; }

        public event Action<int> OnLevelUp;
        public event Action<float, float> OnXPChanged; // current, needed

        PlayerHealth _health;

        // Temporary powerup buffs.
        //
        // Held as one struct per kind rather than ten parallel floats,
        // because the HUD has to enumerate whatever is currently running and
        // show how long is left — which needs the ORIGINAL duration as well
        // as the expiry, and two more loose floats per buff was five more
        // chances to update one and forget the other.
        public enum BuffKind { Damage, AttackSpeed, MoveSpeed, Cooldown, Area }

        public struct TimedBuff
        {
            public float Amount;
            public float Until;
            public float Duration;

            public bool Active => Until > 0f && Time.time < Until;
            public float Remaining => Mathf.Max(0f, Until - Time.time);
            public float Fraction01 => Duration <= 0f ? 0f : Mathf.Clamp01(Remaining / Duration);

            public void Apply(float amount, float seconds)
            {
                // A stronger or longer refresh wins on each axis separately,
                // so picking up a weak one never shortens a strong one.
                Amount = Mathf.Max(Amount, amount);
                float newUntil = Time.time + seconds;
                if (newUntil > Until)
                {
                    Until = newUntil;
                    Duration = seconds;
                }
            }

            public bool Expire()
            {
                if (Until <= 0f || Time.time < Until) return false;
                Until = 0f;
                Amount = 0f;
                Duration = 0f;
                return true;
            }
        }

        TimedBuff _damage, _attackSpeed, _moveSpeed, _cooldown, _area;

        public struct ActiveBuff
        {
            public BuffKind Kind;
            public float Amount;
            public float Remaining;
            public float Fraction01;
        }

        // Fills `into` with whatever is running right now, in a stable order
        // so chips in the HUD don't reshuffle as buffs come and go.
        public void GetActiveBuffs(List<ActiveBuff> into)
        {
            into.Clear();
            Add(into, BuffKind.Damage, _damage);
            Add(into, BuffKind.AttackSpeed, _attackSpeed);
            Add(into, BuffKind.MoveSpeed, _moveSpeed);
            Add(into, BuffKind.Cooldown, _cooldown);
            Add(into, BuffKind.Area, _area);
        }

        static void Add(List<ActiveBuff> into, BuffKind kind, TimedBuff buff)
        {
            if (!buff.Active) return;
            into.Add(new ActiveBuff
            {
                Kind = kind,
                Amount = buff.Amount,
                Remaining = buff.Remaining,
                Fraction01 = buff.Fraction01,
            });
        }

        void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            bool changed = _damage.Expire();
            changed |= _attackSpeed.Expire();
            changed |= _moveSpeed.Expire();
            changed |= _cooldown.Expire();
            changed |= _area.Expire();
            if (changed) RecomputeMultipliers();
        }

        // Tech tree: "Forager".
        public float XPBonus { get; private set; }
        public void AddXPBonus(float amount) => XPBonus += amount;

        public void AddXP(float amount)
        {
            CurrentXP += amount * (1f + XPBonus);
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
            _damage.Apply(multiplierAdd, seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryAttackSpeedBuff(float multiplierAdd, float seconds)
        {
            _attackSpeed.Apply(multiplierAdd, seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryMoveSpeedBuff(float multiplierAdd, float seconds)
        {
            _moveSpeed.Apply(multiplierAdd, seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryCooldownBuff(float reductionFraction, float seconds)
        {
            _cooldown.Apply(reductionFraction, seconds);
            RecomputeMultipliers();
        }

        public void ApplyTemporaryAreaBuff(float multiplierAdd, float seconds)
        {
            _area.Apply(multiplierAdd, seconds);
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

        public void AddPermanentCooldownReduction(float amount)
        {
            PermanentCooldownReduction += amount;
            RecomputeMultipliers();
        }

        public void AddPermanentAreaBonus(float amount)
        {
            PermanentAreaBonus += amount;
            RecomputeMultipliers();
        }

        public void AddPermanentPickupRadiusBonus(float amount)
        {
            PermanentPickupRadiusBonus += amount;
            RecomputeMultipliers();
        }

        void RecomputeMultipliers()
        {
            DamageMultiplier = 1f + PermanentDamageBonus + (_damage.Active ? _damage.Amount : 0f);
            AttackSpeedMultiplier = 1f + PermanentAttackSpeedBonus + (_attackSpeed.Active ? _attackSpeed.Amount : 0f);
            MoveSpeedMultiplier = 1f + PermanentMoveSpeedBonus + (_moveSpeed.Active ? _moveSpeed.Amount : 0f);
            AbilityCooldownMultiplier = Mathf.Max(0.25f, 1f - PermanentCooldownReduction - (_cooldown.Active ? _cooldown.Amount : 0f));
            AreaMultiplier = 1f + PermanentAreaBonus + (_area.Active ? _area.Amount : 0f);
            PickupRadiusMultiplier = 1f + PermanentPickupRadiusBonus;
        }
    }
}
