using UnityEngine;
using UnityEngine.SceneManagement;
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
            // RuntimeInitializeOnLoadMethod fires once per application start,
            // NOT per scene load — so "Press R to restart" (which reloads the
            // scene) used to drop the player into a completely empty world
            // with no ground, player or spawner. Subscribing here rebuilds on
            // every subsequent load. The unsubscribe guards against a double
            // subscription if this ever runs twice.
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            Build();
        }

        static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Build();
        }

        static void Build()
        {
            // sceneLoaded ALSO fires for the very first scene, after the
            // AfterSceneLoad callback above — so both paths ran for the same
            // scene and the entire world was built twice (two gorillas, two
            // arenas, two HUDs). Building is idempotent per scene: if the
            // player already exists, this scene is already dressed.
            //
            // A reload clears this by itself, since the old player is
            // destroyed (nulling Instance) before sceneLoaded fires.
            if (PlayerController.Instance != null) return;

            Sfx.ResetForNewSession();
            Sfx.WarmUp();
            CameraShake.Reset();
            SetupLighting();
            Blocky3DArt.Ground();

            // The fight is confined to a walled arena, so the ground plane no
            // longer needs to chase the player to stay under them.
            var arenaGO = new GameObject("Arena");
            var arena = arenaGO.AddComponent<Arena>();
            arena.Build();

            // Hills on the horizon. Outside the walls was a flat green plane
            // meeting a flat blue sky in a hard line, and fog can only soften
            // what is actually out there.
            Blocky3DArt.Backdrop();

            var player = CreatePlayer();

            // Seeds props across the arena and refills anything the player
            // consumes (felled trees, launched boulders).
            var streamerGO = new GameObject("EnvironmentStreamer");
            streamerGO.AddComponent<EnvironmentStreamer>().PopulateInitial();

            SetupCamera(player.transform);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();

            // Keyboard-only cheats for reaching the late game; see the file.
            gameManagerGO.AddComponent<DebugControls>();

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
            // Set BEFORE CharacterAnimator is added: the animator snapshots
            // the model's local position in Awake as the pose it animates
            // around, so lifting it afterwards would be undone on the first
            // frame.
            model.transform.localPosition = new Vector3(0f, Blocky3DArt.GorillaGroundLift, 0f);

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
            // Holds the run's spent tech points; the nodes mutate the
            // ability components added below, so it must sit on the player.
            go.AddComponent<TechTreeState>();
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
            // Reaches past the arena wall so the colosseum reads clearly
            // and the fog only softens what is beyond it.
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 120f;
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
