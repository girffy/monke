using UnityEngine;
using GorillaSurvivors.Player;
using GorillaSurvivors.Player.Abilities;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Environment;
using GorillaSurvivors.UI;

namespace GorillaSurvivors.Core
{
    // Builds the entire playable world at scene-load time, entirely from code.
    // Means the scene file itself can stay an empty template scene — no manual
    // wiring required in the Editor.
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Setup()
        {
            Sfx.ResetForNewSession();
            Sfx.WarmUp();
            CameraShake.Reset();
            SetupLighting();
            var ground = Blocky3DArt.Ground();
            ground.AddComponent<GroundFollower>();

            var player = CreatePlayer();

            // Props stream around the player rather than being scattered once,
            // so the world never runs out however far the run travels. Created
            // after the player because it seeds the first batch around them.
            var streamerGO = new GameObject("EnvironmentStreamer");
            streamerGO.AddComponent<EnvironmentStreamer>().PopulateInitial();

            SetupCamera(player.transform);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();

            var spawnerGO = new GameObject("EnemySpawner");
            var spawner = spawnerGO.AddComponent<EnemySpawner>();

            gameManager.RegisterSpawner(spawner);
            gameManager.RegisterPlayer(player.GetComponent<PlayerHealth>());

            var hud = HUDController.Build(player.GetComponent<PlayerHealth>(), player.GetComponent<PlayerStats>());
            gameManager.OnGameOver += hud.ShowGameOver;
            gameManager.OnUpgradeChoiceReady += hud.ShowUpgradeChoice;
            gameManager.OnRoundStarted += hud.ShowRoundBanner;
            gameManager.OnPauseToggled += hud.ShowPaused;
        }

        static GameObject CreatePlayer()
        {
            var go = new GameObject("Gorilla");
            go.transform.position = Vector3.zero;

            var model = Blocky3DArt.Gorilla();
            model.transform.SetParent(go.transform, false);

            go.AddComponent<Rigidbody>();

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.55f;
            collider.height = 1.3f;
            collider.center = new Vector3(0f, 0.65f, 0f);

            var anim = go.AddComponent<CharacterAnimator>();
            // Drops onto its knuckles to move and rises upright when still.
            // The directional lean is kept small because the knuckle pitch is
            // already doing most of the forward tilt.
            anim.KnuckleWalk = true;
            anim.StrideFrequency = 2.6f;
            anim.LegSwing = 26f;
            anim.ArmSwing = 34f;
            anim.BobHeight = 0.07f;
            anim.LeanDegrees = 5f;

            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerAttack>();
            go.AddComponent<QuickSwipeAttack>();
            go.AddComponent<ChestBeatAbility>();
            go.AddComponent<DungTossAbility>();

            return go;
        }

        // A single warm key light angled to match the fixed camera, plus a
        // cool ambient sky/ground gradient so shadowed sides stay readable
        // instead of going black. Everything in the world is built from
        // untextured primitives, so this shading is doing most of the work of
        // making shapes legible.
        static void SetupLighting()
        {
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;
            // Bias tuned for the small, tightly-packed primitives the models
            // are built from — the default bias makes limbs shadow-acne.
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.6f;
            sunGO.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.60f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.44f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.26f, 0.20f);

            // Gentle distance fog hides the hard edge where the ground plane
            // ends and gives the arena a bit of depth.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.44f, 0.52f, 0.46f);
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 78f;
        }

        static void SetupCamera(Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            cam.orthographic = false;
            cam.fieldOfView = 45f;
            cam.transform.rotation = Quaternion.Euler(CameraFollow.PitchDegrees, 0f, 0f);

            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = target;
            follow.SnapTo(target.position + follow.Offset);
        }

    }
}
