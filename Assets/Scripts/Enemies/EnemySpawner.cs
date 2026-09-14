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

            EnemyFactory.Create(type, playerPos + offset, difficultyScale);
            _spawnedThisRound++;
            _aliveThisRound++;
        }

        static HumanVariant ChooseEnemyType(int round)
        {
            float roll = Random.value;

            if (round >= 7 && roll < 0.15f) return HumanVariant.Brute;
            if (round >= 5 && roll < 0.35f) return HumanVariant.Thrower;
            if (round >= 3 && roll < 0.60f) return HumanVariant.Runner;

            return HumanVariant.Grunt;
        }
    }
}
