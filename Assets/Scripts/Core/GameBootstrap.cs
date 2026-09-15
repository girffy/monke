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
            Blocky3DArt.Ground();
            SpawnObstacles();

            var rockManagerGO = new GameObject("AttackableRockManager");
            var rockManager = rockManagerGO.AddComponent<AttackableRockManager>();
            rockManager.SpawnInitial(24, 6f, 45f);

            var player = CreatePlayer();
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
            // A knuckle-walker: big arm swing, heavy body bob, modest lean.
            anim.StrideFrequency = 2.9f;
            anim.LegSwing = 30f;
            anim.ArmSwing = 42f;
            anim.BobHeight = 0.09f;
            anim.LeanDegrees = 10f;

            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerAttack>();
            go.AddComponent<QuickSwipeAttack>();
            go.AddComponent<RoarAbility>();
            go.AddComponent<ChargeAbility>();

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

        static void SpawnObstacles()
        {
            // Rocks are no longer purely decorative — every rock in the world
            // comes from AttackableRockManager now, so all of them can be
            // ground-slammed into a boulder instead of some being inert.
            const float minDistanceFromSpawn = 6f;
            const float scatterRadius = 52f;

            for (int i = 0; i < 26; i++)
            {
                var pos = RandomScatterPos(minDistanceFromSpawn, scatterRadius);
                var tree = Blocky3DArt.Tree();
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * Random.Range(0.85f, 1.3f);
                tree.AddComponent<FellableTree>();
            }

            for (int i = 0; i < 30; i++)
            {
                var bush = Blocky3DArt.Bush();
                bush.transform.position = RandomScatterPos(minDistanceFromSpawn * 0.6f, scatterRadius);
                bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                bush.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            }

            // Ground dressing — no colliders, purely to keep the plane from
            // reading as an empty field between the gameplay props.
            for (int i = 0; i < 220; i++)
            {
                var tuft = Blocky3DArt.GrassTuft();
                tuft.transform.position = RandomScatterPos(2f, scatterRadius);
                tuft.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
            }

            for (int i = 0; i < 60; i++)
            {
                var flower = Blocky3DArt.Flower();
                flower.transform.position = RandomScatterPos(3f, scatterRadius);
                flower.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                flower.transform.localScale = Vector3.one * Random.Range(0.85f, 1.25f);
            }

            for (int i = 0; i < 10; i++)
            {
                var stump = Blocky3DArt.Stump();
                stump.transform.position = RandomScatterPos(8f, scatterRadius);
                stump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            for (int i = 0; i < 24; i++)
            {
                var shroom = Blocky3DArt.Mushroom();
                shroom.transform.position = RandomScatterPos(5f, scatterRadius);
                shroom.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                shroom.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
            }
        }

        static Vector3 RandomScatterPos(float minRadius, float maxRadius)
        {
            var dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minRadius, maxRadius);
            return new Vector3(dir.x * dist, 0f, dir.y * dist);
        }
    }
}
