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

        // ---- Carnivore ---------------------------------------------------
        // Replaces "Apex", whose two health-threshold effects were invisible
        // in play: nothing marked the moment you crossed 80% or 30%, so the
        // numbers silently changed under you. A chance on kill is legible —
        // you see the heal happen.
        public bool CarnivoreEnabled;
        public const float CarnivoreChance = 0.3f;
        public const float CarnivoreHeal = 5f;

        // ---- Harvesting --------------------------------------------------
        // Replaces "Second Wind", which was a once-a-round panic button that
        // fired on its own and so was never a decision. This one pays out
        // constantly and in a currency the Toss limb already cares about, so
        // the capstone reinforces the branch you spent the points in.
        public bool HarvestingEnabled;
        public const float HarvestChance = 0.5f;

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

        // Multiplies both melee attacks.
        public float MeleeDamageMultiplier
        {
            get
            {
                float bonus = 0f;
                if (FrenzyEnabled) bonus += FrenzyStacks * FrenzyPerStack;
                return 1f + bonus;
            }
        }

        public float DamageTakenMultiplier => 1f;

        public void NotifyDamaged() { }

        public void NotifyHealthDropped() { }

        // "Harvesting": half of everything you kill hands back a throw.
        // Hooked to the enemy's death rather than to the attack, so it pays
        // out for bleeds, blasts, dash damage and the dung itself — which is
        // the point, since a big enough spread can pay for its own next one.
        public void NotifyKill()
        {
            if (HarvestingEnabled && Random.value <= HarvestChance)
            {
                GetComponent<Abilities.DungTossAbility>()?.GrantCharge();
            }

            // "Carnivore": a bite out of whatever just went down.
            if (CarnivoreEnabled && _health != null && Random.value <= CarnivoreChance)
            {
                _health.Heal(CarnivoreHeal);
            }
        }
    }
}
