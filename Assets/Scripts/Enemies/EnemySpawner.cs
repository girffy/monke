using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Environment;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    // Spawns enemies in rounds of `EnemiesPerRound`. Once every enemy for the
    // round has been both spawned and killed, it stops and hands control to
    // GameManager to show the round-reward choice; StartNewRound resumes it.
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        public int EnemiesPerRound = 100;
        public float SpawnRadius = 9f;
        public float BaseSpawnInterval = 1.4f;
        public float MinSpawnInterval = 0.2f;
        public int MaxEnemiesPerBatch = 8;

        public int CurrentRound { get; private set; } = 1;

        // Every spawned enemy is either alive or dead, so killed-so-far is
        // just the difference — enough for a simple "X/100" HUD readout.
        public int KilledThisRound => _spawnedThisRound - _aliveThisRound;

        float _nextSpawnTime;
        int _spawnedThisRound;
        int _aliveThisRound;
        bool _roundEnding;
        bool _paused;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;

        // How many of this round's enemies have yet to appear. Used by the
        // debug round-skip to pay out the XP they would have dropped.
        public int RemainingToSpawn => Mathf.Max(0, EnemiesPerRound - _spawnedThisRound);

        // Ends the round immediately, as though every enemy in it had been
        // spawned and killed.
        public void ForceFinishRound()
        {
            _spawnedThisRound = EnemiesPerRound;
            _aliveThisRound = 0;
            if (_roundEnding) return;

            _roundEnding = true;
            GameManager.Instance?.BeginUpgradeChoice();
        }

        public void StartNewRound(int round)
        {
            CurrentRound = round;
            _spawnedThisRound = 0;
            _aliveThisRound = 0;
            _roundEnding = false;
            _nextSpawnTime = 0f;
        }

        public void NotifyEnemyDied()
        {
            _aliveThisRound = Mathf.Max(0, _aliveThisRound - 1);
        }

        void Update()
        {
            if (_paused || _roundEnding) return;
            if (PlayerController.Instance == null) return;

            if (_spawnedThisRound >= EnemiesPerRound)
            {
                if (_aliveThisRound <= 0)
                {
                    _roundEnding = true;
                    GameManager.Instance?.BeginUpgradeChoice();
                }
                return;
            }

            if (Time.time < _nextSpawnTime) return;

            float progress = _spawnedThisRound / (float)EnemiesPerRound;
            float interval = Mathf.Lerp(BaseSpawnInterval, MinSpawnInterval, progress);
            _nextSpawnTime = Time.time + interval;

            int batchSize = Mathf.Min(1 + CurrentRound / 2, MaxEnemiesPerBatch);
            batchSize = Mathf.Min(batchSize, EnemiesPerRound - _spawnedThisRound);

            for (int i = 0; i < batchSize; i++)
            {
                SpawnOne();
            }
        }

        void SpawnOne()
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;

            // Drives HP/speed growth, and it COMPOUNDS rather than growing in
            // a straight line. The player's damage does: +12% per level with
            // a dozen levels a round, times tech-tree multipliers, times flat
            // damage nodes. Against a linear enemy curve that meant a Brute —
            // the "takes several hits" enemy — was a one-shot for the light
            // attack by round 3. An exponent just under 1.6 keeps the number
            // of hits a Brute takes roughly flat as the run goes on.
            float difficultyScale = Mathf.Pow(CurrentRound, 1.55f) - 1f;
            var type = ChooseEnemyType(CurrentRound);

            EnemyFactory.Create(type, ChooseSpawnPosition(playerPos), difficultyScale, CurrentRound);
            _spawnedThisRound++;
            _aliveThisRound++;
        }

        // A minority of each wave walks in through a gate the player can see,
        // which is what makes the arena read as a colosseum rather than
        // enemies materialising out of thin air. The rest spawn on a ring
        // around the player exactly as before.
        //
        // Deliberately a minority: when EVERY enemy came from the visible
        // gate, standing in a gate mouth and swinging was the whole game,
        // because the entire round filed into the same two metres. Most of
        // the wave still arriving behind you is what stops camping working.
        const float GateSpawnChance = 0.3f;

        Vector3 ChooseSpawnPosition(Vector3 playerPos)
        {
            var arena = Arena.Instance;
            if (arena != null && Random.value < GateSpawnChance)
            {
                var cam = Camera.main;
                _visibleGates.Clear();
                for (int i = 0; i < Arena.GateDirections.Length; i++)
                {
                    if (arena.IsGateOnScreen(i, cam)) _visibleGates.Add(i);
                }

                if (_visibleGates.Count > 0)
                {
                    int gate = _visibleGates[Random.Range(0, _visibleGates.Count)];
                    Vector3 gatePos = arena.GatePosition(gate);
                    Vector3 side = Vector3.Cross(Vector3.up, Arena.GateDirections[gate]);
                    // Spread across the width of the opening so they file in
                    // rather than stacking on one point.
                    Vector3 spread = side * Random.Range(-2.4f, 2.4f);
                    return arena.ClampInside(gatePos + spread, 1.2f);
                }
            }

            // Ring around the player. In an arena, prefer an angle that
            // actually lands inside rather than clamping a bad one — clamping
            // pins every rejected angle onto the same stretch of wall, which
            // reads as enemies spawning out of the stonework in a line.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 offset2D = Random.insideUnitCircle.normalized * SpawnRadius;
                var ringPos = playerPos + new Vector3(offset2D.x, 0f, offset2D.y);
                if (arena == null) return ringPos;
                if (arena.IsInside(ringPos, 1.2f)) return ringPos;
            }

            Vector2 fallback2D = Random.insideUnitCircle.normalized * SpawnRadius;
            var fallbackPos = playerPos + new Vector3(fallback2D.x, 0f, fallback2D.y);
            return arena != null ? arena.ClampInside(fallbackPos, 1.2f) : fallbackPos;
        }

        readonly System.Collections.Generic.List<int> _visibleGates = new System.Collections.Generic.List<int>();

        // Each specialist enters the mix at the round where the player has
        // plausibly unlocked a tool that answers it, then becomes steadily
        // more common. Rarer, more disruptive types are checked first so the
        // commoner ones can't crowd them out of the roll.
        static HumanVariant ChooseEnemyType(int round)
        {
            float roll = Random.value;

            // The Wizard is the rarest thing in the game and the only enemy
            // that refuses to be chased. One at a time is a fight; a pack of
            // them teleporting around the arena is noise, so the chance is
            // kept low even deep into a run.
            if (round >= 7)
            {
                float wizardChance = Mathf.Min(0.05f, 0.012f + round * 0.002f);
                if (roll < wizardChance) return HumanVariant.Wizard;
                roll -= wizardChance;
            }

            // Medics rewrite the fight (a crowd that heals), so they stay the
            // rarest of the specialists.
            if (round >= 6)
            {
                float medicChance = Mathf.Clamp01(0.03f + round * 0.004f);
                if (roll < medicChance) return HumanVariant.Medic;
                roll -= medicChance;
            }

            // Brutes are the "takes several hits" check on the player's
            // damage output — a rare handful show up even in round 1, and
            // they get more common as rounds go on.
            float bruteChance = Mathf.Clamp01(0.05f + round * 0.015f);
            if (roll < bruteChance) return HumanVariant.Brute;
            roll -= bruteChance;

            if (round >= 5)
            {
                float bomberChance = Mathf.Clamp01(0.05f + round * 0.008f);
                if (roll < bomberChance) return HumanVariant.Bomber;
                roll -= bomberChance;
            }

            if (round >= 3)
            {
                float shieldChance = Mathf.Clamp01(0.05f + round * 0.007f);
                if (roll < shieldChance) return HumanVariant.Shieldman;
                roll -= shieldChance;
            }

            if (round >= 4 && roll < 0.32f) return HumanVariant.Thrower;
            if (round >= 2 && roll < 0.60f) return HumanVariant.Runner;

            return HumanVariant.Grunt;
        }
    }
}
