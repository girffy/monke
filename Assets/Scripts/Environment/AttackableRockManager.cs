using System.Collections;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Environment
{
    // Owns respawning attackable rocks after they're consumed (launched).
    // Initial placement (via SpawnInitial) can be anywhere in the scatter
    // zone; respawns are specifically required to land offscreen so a rock
    // doesn't just pop into view.
    public class AttackableRockManager : MonoBehaviour
    {
        public static AttackableRockManager Instance { get; private set; }

        public float RespawnDelay = 4f;
        public float MinSpawnDistance = 6f;
        public float MaxSpawnDistance = 14f;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SpawnInitial(int count, float minDistance, float maxDistance)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir2D = Random.insideUnitCircle.normalized;
                float dist = Random.Range(minDistance, maxDistance);
                var pos = new Vector3(dir2D.x * dist, 0f, dir2D.y * dist);
                CreateRockAt(pos);
            }
        }

        public void NotifyRockConsumed()
        {
            StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(RespawnDelay);
            CreateRockAt(FindOffscreenPosition());
        }

        Vector3 FindOffscreenPosition()
        {
            var cam = Camera.main;
            Vector3 origin = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector2 dir2D = Random.insideUnitCircle.normalized;
                float dist = Random.Range(MinSpawnDistance, MaxSpawnDistance);
                var candidate = origin + new Vector3(dir2D.x * dist, 0f, dir2D.y * dist);

                if (cam == null) return candidate;

                Vector3 viewport = cam.WorldToViewportPoint(candidate);
                bool behindCamera = viewport.z < 0f;
                bool outsideView = viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f;
                if (behindCamera || outsideView) return candidate;
            }

            return origin + new Vector3(0f, 0f, MaxSpawnDistance);
        }

        static void CreateRockAt(Vector3 pos)
        {
            var rock = Blocky3DArt.Rock(Random.Range(0.8f, 1.2f));
            rock.transform.position = pos;
            rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            rock.AddComponent<AttackableRock>();
        }
    }
}
