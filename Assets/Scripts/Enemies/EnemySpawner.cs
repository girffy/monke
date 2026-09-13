using UnityEngine;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public float SpawnRadius = 9f;
        public float BaseSpawnInterval = 1.6f;
        public float MinSpawnInterval = 0.25f;
        public float DifficultyRampPerSecond = 0.01f;
        public int MaxEnemiesPerBatch = 8;

        float _elapsed;
        float _nextSpawnTime;
        bool _paused;

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;

        void Update()
        {
            if (_paused) return;
            if (PlayerController.Instance == null) return;

            _elapsed += Time.deltaTime;

            if (Time.time < _nextSpawnTime) return;

            float difficultyScale = _elapsed * DifficultyRampPerSecond;
            float interval = Mathf.Lerp(BaseSpawnInterval, MinSpawnInterval, Mathf.Clamp01(_elapsed / 180f));
            _nextSpawnTime = Time.time + interval;

            int batchSize = 1 + Mathf.FloorToInt(_elapsed / 25f);
            batchSize = Mathf.Min(batchSize, MaxEnemiesPerBatch);

            for (int i = 0; i < batchSize; i++)
            {
                SpawnOne(difficultyScale);
            }
        }

        void SpawnOne(float difficultyScale)
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;
            Vector2 offset2D = Random.insideUnitCircle.normalized * SpawnRadius;
            var offset = new Vector3(offset2D.x, 0f, offset2D.y);
            EnemyFactory.Create(playerPos + offset, difficultyScale);
        }
    }
}
