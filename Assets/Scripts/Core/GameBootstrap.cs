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

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatureArt.Gorilla();
            renderer.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

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

            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(target.position.x, target.position.y, -10f);

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = target;
        }
    }
}
