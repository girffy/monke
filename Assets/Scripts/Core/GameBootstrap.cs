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

            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerAttack>();
            go.AddComponent<RoarAbility>();
            go.AddComponent<ChargeAbility>();

            return go;
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
            cam.transform.position = target.position + follow.Offset;
        }

        static void SpawnObstacles()
        {
            // Rocks are no longer purely decorative — every rock in the world
            // comes from AttackableRockManager now, so all of them can be
            // ground-slammed into a boulder instead of some being inert.
            const int treeCount = 18;
            const int bushCount = 26;
            const float minDistanceFromSpawn = 6f;
            const float scatterRadius = 45f;

            for (int i = 0; i < treeCount; i++)
            {
                var pos = RandomScatterPos(minDistanceFromSpawn, scatterRadius);
                var tree = Blocky3DArt.Tree();
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(0.85f, 1.25f);
                tree.transform.localScale = Vector3.one * s;
            }

            for (int i = 0; i < bushCount; i++)
            {
                var pos = RandomScatterPos(minDistanceFromSpawn * 0.6f, scatterRadius);
                var bush = Blocky3DArt.Bush();
                bush.transform.position = pos;
                bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                bush.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
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
