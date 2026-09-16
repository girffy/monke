using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Core
{
    // Cheats for reaching the late game without playing twenty minutes to
    // get there. Everything here is deliberately keyboard-only and
    // undiscoverable — there is no UI for it, so a normal player will never
    // trip over it, but it is compiled into every build so the shared web
    // link can be poked at the same way.
    //
    //   F1  Skip the round: award the XP for every enemy left in it, clear
    //       the field, and finish the round as though you had killed them.
    //   F2  +1 tech point.
    //   F3  Toggle invulnerability.
    //   F4  +5 levels.
    //
    // F1 is the important one. Testing anything about round 8 otherwise
    // means surviving rounds 1 through 7 first, every single time.
    public class DebugControls : MonoBehaviour
    {
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) SkipRound();
            if (kb.f2Key.wasPressedThisFrame) GrantTechPoint();
            if (kb.f3Key.wasPressedThisFrame) ToggleInvulnerable();
            if (kb.f4Key.wasPressedThisFrame) GrantLevels(5);
        }

        static void SkipRound()
        {
            var player = PlayerController.Instance;
            var spawner = EnemySpawner.Instance;
            if (player == null || spawner == null) return;

            var stats = player.GetComponent<PlayerStats>();

            // Kill what's on the field. Routing through Kill() rather than
            // Destroy keeps the spawner's alive-count honest and pays the XP,
            // so the level you arrive at matches the level real play would
            // have given you — a skip that skipped the XP too would make
            // every late round look harder than it is.
            var alive = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var enemy in alive)
            {
                if (enemy != null) enemy.Kill();
            }

            // Then pay out everything that hadn't spawned yet, at the round's
            // average XP, and tell the spawner the round is done.
            int remaining = spawner.RemainingToSpawn;
            if (stats != null && remaining > 0) stats.AddXP(remaining * 3.5f);

            spawner.ForceFinishRound();
            HUDController_Toast($"Skipped to the end of round {spawner.CurrentRound}");
        }

        static void GrantTechPoint()
        {
            var player = PlayerController.Instance;
            player?.GetComponent<TechTreeState>()?.GrantPoints(1);
            HUDController_Toast("+1 tech point");
        }

        static void ToggleInvulnerable()
        {
            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health == null) return;

            _invulnerable = !_invulnerable;
            health.GrantInvulnerability(_invulnerable ? 99999f : 0f);
            if (!_invulnerable) health.ClearInvulnerability();
            HUDController_Toast(_invulnerable ? "Invulnerable ON" : "Invulnerable OFF");
        }

        static bool _invulnerable;

        static void GrantLevels(int count)
        {
            var player = PlayerController.Instance;
            var stats = player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats == null) return;

            for (int i = 0; i < count; i++) stats.AddXP(stats.XPToNextLevel - stats.CurrentXP);
            HUDController_Toast($"Level {stats.Level}");
        }

        // Routed through a helper so this file doesn't need a UI reference
        // when the HUD hasn't been built yet.
        static void HUDController_Toast(string message)
        {
            UI.HUDController.Instance?.ShowToast(message);
        }
    }
}
