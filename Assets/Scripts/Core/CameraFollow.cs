using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Fixed-angle follow camera: stays at a constant offset/pitch above-behind
    // the target on the XZ ground plane. Never orbits, so world-space UI
    // labels (damage popups, pickup labels) can use one constant rotation
    // instead of billboarding every frame.
    public class CameraFollow : MonoBehaviour
    {
        public const float PitchDegrees = 45f;
        // Matches the camera's own rotation exactly — the camera never
        // rotates (fixed pitch, position-only follow), so every world-space
        // label can share this one constant instead of billboarding per-frame.
        public static readonly Quaternion LabelRotation = Quaternion.Euler(PitchDegrees, 0f, 0f);

        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 8f, -8f);
        public float SmoothTime = 0.15f;

        Vector3 _velocity;

        void LateUpdate()
        {
            if (Target == null) return;

            var targetPos = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, SmoothTime);
        }
    }
}
