using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Screen shake, applied by CameraFollow when it positions the camera each
    // frame (rather than as its own LateUpdate) so the two can't fight over
    // the transform depending on script execution order.
    //
    // Static state, so it must be cleared at the start of each session — the
    // same trap that silently broke audio once already (see Sfx).
    public static class CameraShake
    {
        static float _timeLeft;
        static float _totalTime;
        static float _magnitude;
        static float _seed;

        public static void Reset()
        {
            _timeLeft = 0f;
            _totalTime = 0f;
            _magnitude = 0f;
            _seed = Random.value * 100f;
        }

        // Overlapping shakes don't stack — the strongest one wins, which
        // keeps a burst of small hits from adding up into a screen-destroying
        // earthquake.
        public static void Shake(float magnitude, float duration)
        {
            if (_timeLeft > 0f && magnitude < _magnitude) return;

            _magnitude = magnitude;
            _totalTime = duration;
            _timeLeft = duration;
            _seed = Random.value * 100f;
        }

        public static Vector3 Advance(float deltaTime)
        {
            if (_timeLeft <= 0f) return Vector3.zero;

            _timeLeft -= deltaTime;
            float falloff = Mathf.Clamp01(_timeLeft / Mathf.Max(0.0001f, _totalTime));
            float amplitude = _magnitude * falloff * falloff;

            // Perlin rather than Random so the motion is a smooth wobble
            // instead of per-frame jitter that reads as a broken framebuffer.
            float t = Time.unscaledTime * 24f;
            float x = (Mathf.PerlinNoise(_seed + t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, _seed + t) - 0.5f) * 2f;
            return new Vector3(x, y, 0f) * amplitude;
        }
    }
}
