using UnityEngine;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Environment
{
    // Keeps the ground plane centred on the player so it never runs out.
    // The plane is finite geometry, and with props now streaming endlessly a
    // long run used to walk right off its edge into open sky.
    //
    // The position is snapped to whole texture tiles rather than following
    // exactly: the grass texture repeats every TileWorldSize units, so
    // snapping to that grid makes the plane's motion invisible. Following
    // the player smoothly would drag the texture along with them and the
    // ground would look like it was sliding underfoot.
    public class GroundFollower : MonoBehaviour
    {
        public float TileWorldSize = 4f;

        void LateUpdate()
        {
            if (PlayerController.Instance == null) return;

            Vector3 p = PlayerController.Instance.transform.position;
            transform.position = new Vector3(
                Mathf.Round(p.x / TileWorldSize) * TileWorldSize,
                0f,
                Mathf.Round(p.z / TileWorldSize) * TileWorldSize);
        }
    }
}
