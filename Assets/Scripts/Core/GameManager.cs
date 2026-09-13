using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsGameOver { get; private set; }
        public float SurvivalTime { get; private set; }

        public event Action OnGameOver;

        EnemySpawner _spawner;

        void Awake()
        {
            Instance = this;
        }

        public void RegisterSpawner(EnemySpawner spawner)
        {
            _spawner = spawner;
        }

        public void RegisterPlayer(PlayerHealth health)
        {
            health.OnDeath += HandlePlayerDeath;
        }

        void Update()
        {
            if (IsGameOver)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            SurvivalTime += Time.deltaTime;
        }

        void HandlePlayerDeath()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            _spawner?.Pause();
            OnGameOver?.Invoke();
        }
    }
}
