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
        public bool IsManuallyPaused { get; private set; }
        public bool IsPaused => IsGameOver || IsChoosingUpgrade || IsManuallyPaused;
        public float SurvivalTime { get; private set; }
        public int CurrentRound => _spawner != null ? _spawner.CurrentRound : 1;
        public int KilledThisRound => _spawner != null ? _spawner.KilledThisRound : 0;
        public int EnemiesPerRound => _spawner != null ? _spawner.EnemiesPerRound : 100;

        public event Action OnGameOver;
        public event Action<List<RoundReward>> OnUpgradeChoiceReady;
        public event Action<int> OnRoundStarted;
        public event Action<bool> OnPauseToggled;

        EnemySpawner _spawner;
        GameObject _player;

        void Awake()
        {
            Instance = this;
            // Time.timeScale is global engine state, not scene state — it
            // survives a scene reload, so a restart must never inherit a
            // frozen clock from a session that ended while paused.
            Time.timeScale = 1f;
        }

        // Esc pause freezes the clock itself rather than just raising
        // IsPaused: cooldowns, respawn timers, coroutines and physics all run
        // on scaled time, so a flag alone would let them keep ticking behind
        // the pause screen. The round-reward screen doesn't need this (its
        // timers resolving in the background is harmless), so it keeps the
        // flag-only pause.
        public void SetManualPause(bool paused)
        {
            if (IsGameOver || IsChoosingUpgrade) return;
            if (IsManuallyPaused == paused) return;

            IsManuallyPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            OnPauseToggled?.Invoke(paused);
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
            // P as well as Esc: browsers reserve Escape for leaving
            // fullscreen/pointer lock, so a WebGL build never reliably sees
            // it — verified missing in an actual browser build.
            bool pausePressed = Keyboard.current != null
                && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);
            if (pausePressed
                || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
            {
                SetManualPause(!IsManuallyPaused);
            }

            if (IsGameOver)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            if (!IsPaused) SurvivalTime += Time.deltaTime;
        }

        // Called by EnemySpawner once every enemy in the round has been both
        // spawned and killed.
        public void BeginUpgradeChoice()
        {
            if (IsGameOver || IsChoosingUpgrade) return;
            IsChoosingUpgrade = true;
            Sfx.RoundClear();

            var choices = RoundRewardPool.RollChoices(_player);
            OnUpgradeChoiceReady?.Invoke(choices);
        }

        // Called by the HUD once the player picks one of the offered rewards.
        public void ResolveUpgradeChoice(RoundReward chosen)
        {
            if (!IsChoosingUpgrade) return;

            chosen.Apply?.Invoke(_player);
            IsChoosingUpgrade = false;
            int nextRound = CurrentRound + 1;
            _spawner?.StartNewRound(nextRound);
            OnRoundStarted?.Invoke(nextRound);
            Sfx.RoundStart();
        }

        void HandlePlayerDeath()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            _spawner?.Pause();
            Sfx.GameOver();
            OnGameOver?.Invoke();
        }
    }
}
