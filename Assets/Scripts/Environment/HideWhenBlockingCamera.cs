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
        public float BlockRadius = 2.6f;

        readonly List<Renderer> _watched = new List<Renderer>();
        readonly List<bool> _hidden = new List<bool>();

        public void Register(GameObject piece)
        {
            foreach (var r in piece.GetComponentsInChildren<Renderer>())
            {
                _watched.Add(r);
                _hidden.Add(false);
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
                bool blocking = projected > 0.5f && projected < length
                                && Vector3.Distance(toPiece, along * projected) < BlockRadius;

                if (blocking == _hidden[i]) continue;
                _hidden[i] = blocking;
                r.enabled = !blocking;
            }
        }
    }
}
