using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Pickups;

namespace GorillaSurvivors.Enemies
{
    public class EnemyHealth : MonoBehaviour
    {
        public float MaxHP = 20f;
        public float XPReward = 3f;
        [Range(0f, 1f)] public float PowerupDropChance = 0.06f;

        // Shieldman: hits that land on the front arc are mostly soaked. AoE
        // and environmental damage arrive with no direction and so ignore the
        // shield entirely — flanking or a Roar/boulder/tree is the counter.
        public float FrontalDamageReduction;

        public float CurrentHP => _currentHP;
        public System.Action<EnemyHealth> OnDied;
        // Fired with the damage actually taken. The Wizard uses it to blink
        // away from a solid hit.
        public System.Action<float> OnDamaged;

        // Counted rather than a bool so overlapping medics can't undo each
        // other: the shield lifts only when the last one drops.
        public bool IsShielded => _shieldSources > 0;
        public void AddShield() => _shieldSources++;
        public void RemoveShield() => _shieldSources = Mathf.Max(0, _shieldSources - 1);

        int _shieldSources;
        float _nextShieldSoundTime;

        float _currentHP;
        bool _initialized;
        bool _dead;
        EnemyHealthBar _healthBar;
        float _healthBarHeight = 2f;
        Transform _model;

        // Set by EnemyFactory right after the collider dimensions are known,
        // so the bar sits just above the model's actual head regardless of
        // variant/scale.
        public void SetHealthBarHeight(float height) => _healthBarHeight = height;

        void Awake()
        {
            _model = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (!_initialized) Init(MaxHP, XPReward);
        }

        public void Heal(float amount)
        {
            if (_dead || amount <= 0f) return;

            float before = _currentHP;
            _currentHP = Mathf.Min(MaxHP, _currentHP + amount);
            if (Mathf.Approximately(before, _currentHP)) return;

            // Only shows if the bar already exists — a never-damaged enemy
            // getting topped up shouldn't reveal a full bar.
            _healthBar?.SetFraction(_currentHP / MaxHP);
        }

        // Call right after AddComponent when spawning from code, so currentHP
        // reflects the real MaxHP rather than the field's default value.
        public void Init(float maxHP, float xpReward)
        {
            MaxHP = maxHP;
            XPReward = xpReward;
            _currentHP = MaxHP;
            _initialized = true;
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null, 0f);
        }

        // Knockback direction/force are optional — callers that represent a
        // directional hit (the ground-slam, Charge) pass them so a surviving
        // enemy gets shoved back; omnidirectional damage (contact, Roar
        // already does its own knockback, rock explosions) can skip it.
        public void TakeDamage(float amount, Vector3? knockbackDirection, float knockbackForce)
        {
            if (_dead) return;

            // Tethered to a live medic: nothing gets through until the medic
            // is dealt with. Rate-limited feedback so a crowd of attacks on a
            // shielded target doesn't machine-gun the sound.
            if (IsShielded)
            {
                if (Time.time >= _nextShieldSoundTime)
                {
                    _nextShieldSoundTime = Time.time + 0.2f;
                    Sfx.ShieldBlock(transform.position);
                    DamagePopup.SpawnText(transform.position, "0", new Color(0.45f, 1f, 0.55f), 0.07f);
                }
                return;
            }

            // A directional hit arriving from the front (the knockback would
            // shove this enemy backwards) gets soaked by the shield.
            if (FrontalDamageReduction > 0f && knockbackDirection.HasValue && _model != null)
            {
                Vector3 push = knockbackDirection.Value;
                push.y = 0f;
                if (push.sqrMagnitude > 0.0001f && Vector3.Dot(push.normalized, _model.forward) < -0.35f)
                {
                    amount *= 1f - FrontalDamageReduction;
                    Sfx.ShieldBlock(transform.position);
                }
            }

            _currentHP -= amount;
            OnDamaged?.Invoke(amount);

            if (_healthBar == null) _healthBar = EnemyHealthBar.Attach(transform, _healthBarHeight);
            _healthBar.SetFraction(_currentHP / MaxHP);

            if (_currentHP <= 0f)
            {
                Die();
                return;
            }

            Sfx.EnemyHit(transform.position);

            // Numbers only on enemies that SURVIVE. Most things die in one
            // hit, so this shows a figure exactly when the player needs it —
            // "that one is tanky and here's by how much" — instead of
            // spraying digits over every kill in a hundred-enemy round.
            DamagePopup.Spawn(transform.position, amount, new Color(1f, 0.92f, 0.62f), 0.075f);

            if (knockbackDirection.HasValue && knockbackForce > 0f)
            {
                GetComponent<EnemyAI>()?.ApplyKnockback(knockbackDirection.Value.normalized * knockbackForce, 0.25f);
            }
        }

        // Bleed / rot from the tech tree. Ticked here rather than as its own
        // component so a second application just tops up the same wound
        // instead of stacking a pile of coroutines on one enemy.
        float _dotRemaining;
        float _dotPerSecond;

        public void ApplyDamageOverTime(float totalDamage, float seconds)
        {
            if (_dead || totalDamage <= 0f || seconds <= 0f) return;

            // Carry over whatever the previous application still owed, so
            // re-applying never wipes damage already promised.
            float carried = _dotPerSecond * _dotRemaining;
            _dotRemaining = seconds;
            _dotPerSecond = (totalDamage + carried) / seconds;
        }

        void Update()
        {
            if (_dead || _dotRemaining <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            float step = Mathf.Min(Time.deltaTime, _dotRemaining);
            _dotRemaining -= step;

            // Deliberately NOT routed through TakeDamage: that plays a hit
            // sound and throws a damage number, and a per-frame tick would
            // machine-gun both. A shield still stops it, though — a tethered
            // enemy takes nothing from any source.
            if (IsShielded) return;

            _currentHP -= _dotPerSecond * step;

            if (_healthBar != null) _healthBar.SetFraction(_currentHP / MaxHP);
            if (_currentHP <= 0f) Die();
        }

        // Removes the enemy outright, ignoring shields and armour. For deaths
        // the enemy inflicts on itself — a Bomber reaching the player and
        // going off — where routing through TakeDamage would let a medic's
        // tether block a detonation that has already happened.
        public void Kill() => Die();

        void Die()
        {
            // A lethal hit and, in the same frame, another source (e.g. a
            // rock explosion overlapping the slam's own hit) could both
            // call TakeDamage before Destroy(gameObject) actually takes
            // effect at end of frame — without this guard that meant a
            // double Die() call, double XP/loot, and EnemySpawner's alive
            // count getting decremented twice for one real enemy.
            if (_dead) return;
            _dead = true;

            if (_healthBar != null) Destroy(_healthBar.gameObject);

            // Death reactions (the Bomber's charge going off) run before this
            // object is torn down; they spawn their own independent objects
            // so the effect outlives the corpse.
            OnDied?.Invoke(this);

            XPOrb.Spawn(transform.position, XPReward);

            if (Random.value < PowerupDropChance)
            {
                PowerupPickup.SpawnRandom(transform.position);
            }

            Sfx.EnemyDeath(transform.position);
            EnemySpawner.Instance?.NotifyEnemyDied();
            Destroy(gameObject);
        }
    }
}
