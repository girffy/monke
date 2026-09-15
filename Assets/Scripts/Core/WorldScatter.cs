using UnityEngine;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Core
{
    // Shared placement helper for world props that come back after being
    // consumed (rocks that get launched, trees that get felled). Replacements
    // deliberately land off-camera so props never visibly pop into existence.
    public static class WorldScatter
    {
        public static Vector3 OffscreenPosition(float minDistance, float maxDistance)
        {
            var cam = Camera.main;
            Vector3 origin = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 dir2D = Random.insideUnitCircle.normalized;
                float dist = Random.Range(minDistance, maxDistance);
                var candidate = origin + new Vector3(dir2D.x * dist, 0f, dir2D.y * dist);

                if (cam == null) return candidate;

                Vector3 viewport = cam.WorldToViewportPoint(candidate);
                bool behindCamera = viewport.z < 0f;
                bool outsideView = viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f;
                if (behindCamera || outsideView) return candidate;
            }

            return origin + new Vector3(0f, 0f, maxDistance);
        }
    }
}
