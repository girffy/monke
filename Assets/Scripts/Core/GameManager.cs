using System;
using System.Collections.Generic;
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
        public bool IsChoosingUpgrade { get; private set; }
        public bool IsPaused => IsGameOver || IsChoosingUpgrade;
        public float SurvivalTime { get; private set; }
        public int CurrentRound => _spawner != null ? _spawner.CurrentRound : 1;

        public event Action OnGameOver;
        public event Action<List<RoundReward>> OnUpgradeChoiceReady;

        EnemySpawner _spawner;
        GameObject _player;

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
            _player = health.gameObject;
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

            if (!IsChoosingUpgrade) SurvivalTime += Time.deltaTime;
        }

        // Called by EnemySpawner once every enemy in the round has been both
        // spawned and killed.
        public void BeginUpgradeChoice()
        {
            if (IsGameOver || IsChoosingUpgrade) return;
            IsChoosingUpgrade = true;

            var choices = RoundRewardPool.RollChoices(_player);
            OnUpgradeChoiceReady?.Invoke(choices);
        }

        // Called by the HUD once the player picks one of the offered rewards.
        public void ResolveUpgradeChoice(RoundReward chosen)
        {
            if (!IsChoosingUpgrade) return;

            chosen.Apply?.Invoke(_player);
            IsChoosingUpgrade = false;
            _spawner?.StartNewRound(CurrentRound + 1);
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
