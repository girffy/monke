using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Player
{
    // The capstones. These are the tech tree's four big conditional effects,
    // kept together because every one of them is read from somewhere else —
    // the melee attacks, PlayerHealth, the abilities — and scattering four
    // half-implemented state machines across those files is how they end up
    // disagreeing with each other.
    public class PlayerPerks : MonoBehaviour
    {
        // ---- Silverback: Frenzy ----------------------------------------
        // Melee kills stack a decaying damage bonus. Deliberately NOT the
        // first design ("a kill readies both attacks"), which turned any
        // crowd into uninterrupted swinging with no ceiling. A capped,
        // decaying stack rewards the same aggression but can't run away.
        public bool FrenzyEnabled;
        public const int MaxFrenzy = 5;
        public const float FrenzyPerStack = 0.08f;
        public const float FrenzyDuration = 3f;

        public int FrenzyStacks { get; private set; }
        // For the HUD's buff chip.
        public float FrenzyRemaining => Mathf.Max(0f, _frenzyExpires - Time.time);
        float _frenzyExpires;

        public void NotifyMeleeKill()
        {
            if (!FrenzyEnabled) return;

            FrenzyStacks = Mathf.Min(MaxFrenzy, FrenzyStacks + 1);
            _frenzyExpires = Time.time + FrenzyDuration;
        }

        // ---- Apex ------------------------------------------------------
        public bool ApexEnabled;
        const float ApexHighHP = 0.8f;
        const float ApexLowHP = 0.3f;

        // ---- Second Wind -----------------------------------------------
        public bool SecondWindEnabled;
        bool _secondWindSpent;
        public void RefreshSecondWind() => _secondWindSpent = false;

        PlayerHealth _health;

        void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (FrenzyStacks > 0 && Time.time >= _frenzyExpires) FrenzyStacks = 0;
        }

        float HealthFraction()
        {
            if (_health == null || _health.MaxHP <= 0f) return 1f;
            return _health.CurrentHP / _health.MaxHP;
        }

        // Multiplies both melee attacks. Frenzy and Apex stack additively
        // with each other so the ceiling stays legible: 5 stacks and full
        // health is +65%, not +81%.
        public float MeleeDamageMultiplier
        {
            get
            {
                float bonus = 0f;
                if (FrenzyEnabled) bonus += FrenzyStacks * FrenzyPerStack;
                if (ApexEnabled && HealthFraction() > ApexHighHP) bonus += 0.25f;
                return 1f + bonus;
            }
        }

        public float DamageTakenMultiplier
        {
            get
            {
                if (ApexEnabled && HealthFraction() < ApexLowHP) return 0.7f;
                return 1f;
            }
        }

        public void NotifyDamaged() { }

        // "Second Wind": falling under a quarter health readies everything,
        // once a round. Fires from PlayerHealth after the hit resolves.
        public void NotifyHealthDropped()
        {
            if (!SecondWindEnabled || _secondWindSpent) return;
            if (HealthFraction() >= 0.25f) return;

            _secondWindSpent = true;
            ReadyEverything();
            UI.HUDController.Instance?.ShowToast("SECOND WIND");
        }

        void ReadyEverything()
        {
            GetComponent<PlayerAttack>()?.ReadyNow();
            GetComponent<PlayerController>()?.ReadyDash();
            GetComponent<Abilities.ChestBeatAbility>()?.ReadyNow();
            GetComponent<Abilities.DungTossAbility>()?.ReadyNow();
        }

        // Kept as a hook for future kill-triggered perks; nothing listens
        // since "One Gorilla" was removed.
        public void NotifyKill() { }
    }
}
