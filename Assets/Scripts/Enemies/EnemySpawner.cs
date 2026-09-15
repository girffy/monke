using UnityEngine;
using GorillaSurvivors.Core;
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
            Vector2 offset2D = Random.insideUnitCircle.normalized * SpawnRadius;
            var offset = new Vector3(offset2D.x, 0f, offset2D.y);

            float difficultyScale = (CurrentRound - 1) * 1.4f;
            var type = ChooseEnemyType(CurrentRound);

            EnemyFactory.Create(type, playerPos + offset, difficultyScale, CurrentRound);
            _spawnedThisRound++;
            _aliveThisRound++;
        }

        // Each specialist enters the mix at the round where the player has
        // plausibly unlocked a tool that answers it, then becomes steadily
        // more common. Rarer, more disruptive types are checked first so the
        // commoner ones can't crowd them out of the roll.
        static HumanVariant ChooseEnemyType(int round)
        {
            float roll = Random.value;

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
