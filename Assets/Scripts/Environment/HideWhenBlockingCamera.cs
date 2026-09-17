using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Environment
{
    // Hides scenery that gets between the camera and the player.
    //
    // The arena's tall pieces — gate archways, the roofs over the tunnels —
    // are a problem only at the near edge of the ring, where the camera sits
    // OUTSIDE the arena looking in and anything overhead lands squarely in
    // front of the gorilla. Earlier passes solved that by refusing to build
    // anything tall at all (no lintel over the gates), which cost the arena
    // its archways.
    //
    // This keeps the geometry and hides it only in the moment it would
    // actually block the shot: a piece is culled when it sits along the
    // segment from the camera to the player and close to that line.
    public class HideWhenBlockingCamera : MonoBehaviour
    {
        // How far from the camera-to-player line a piece can be and still be
        // considered in the way. Roughly the on-screen width of the gorilla.
        // Generous, because the things it hides are now whole tiers of
        // seating several metres wide: clearing a narrow corridor through a
        // stand leaves its neighbours still standing in the shot.
        public float BlockRadius = 4.2f;

        // Hysteresis. A single threshold made this a coin flip for anything
        // sitting exactly at it: standing by a gate, the camera-to-player
        // line grazed the roof and the near stands, and the test came out
        // differently on consecutive frames as the gorilla breathed. The
        // result was the archway strobing black against the stone behind it.
        //
        // So a piece hides at BlockRadius and does not come back until it is
        // clearly outside — and either way it has to hold still for a
        // moment first.
        public float UnblockRadius = 5.3f;
        public float MinHoldSeconds = 0.25f;

        readonly List<Renderer> _watched = new List<Renderer>();
        readonly List<bool> _hidden = new List<bool>();
        readonly List<float> _changedAt = new List<float>();

        public void Register(GameObject piece)
        {
            foreach (var r in piece.GetComponentsInChildren<Renderer>())
            {
                _watched.Add(r);
                _hidden.Add(false);
                _changedAt.Add(0f);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var player = PlayerController.Instance;
            if (cam == null || player == null) return;

            Vector3 from = cam.transform.position;
            Vector3 to = player.transform.position + Vector3.up * 0.8f;
            Vector3 along = to - from;
            float length = along.magnitude;
            if (length < 0.01f) return;
            along /= length;

            for (int i = 0; i < _watched.Count; i++)
            {
                var r = _watched[i];
                if (r == null) continue;

                Vector3 toPiece = r.bounds.center - from;
                float projected = Vector3.Dot(toPiece, along);

                // Only pieces actually BETWEEN the two count — something
                // behind the camera or past the player is never in the way.
                bool between = projected > 0.5f && projected < length;

                // Plain world distance from the sight line.
                //
                // A screen-relative version of this was tried and was worse
                // on both counts: it let the near gate sit in shot because
                // the gate is close enough that dividing by distance shrank
                // its offset, while distant stands still qualified. The
                // aggressiveness was never the measurement's fault — it was
                // that far too much geometry was registered in the first
                // place, which is fixed at the registration end instead.
                float offset = Vector3.Distance(toPiece, along * projected);

                // The threshold to cross depends on which side it is already
                // on, so a piece hovering on the line stays where it is.
                bool blocking = _hidden[i]
                    ? between && offset < UnblockRadius
                    : between && offset < BlockRadius;

                if (blocking == _hidden[i]) continue;
                if (Time.time - _changedAt[i] < MinHoldSeconds) continue;

                _hidden[i] = blocking;
                _changedAt[i] = Time.time;
                r.enabled = !blocking;
            }
        }
    }
}
