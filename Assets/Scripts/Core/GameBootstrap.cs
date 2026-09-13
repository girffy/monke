using UnityEngine;
using GorillaSurvivors.Player;
using GorillaSurvivors.Enemies;
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

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = target;
            cam.transform.position = target.position + follow.Offset;
        }
    }
}
