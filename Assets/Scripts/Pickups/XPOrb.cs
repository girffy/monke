using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Pickups
{
    public class XPOrb : MonoBehaviour
    {
        public float XPAmount = 3f;
        public float MagnetRadius = 2.5f;
        public float MagnetSpeed = 9f;
        public float PickupRadius = 0.45f;

        Transform _player;

        public static XPOrb Spawn(Vector3 position, float xpAmount)
        {
            var go = Blocky3DArt.Gem();
            go.name = "XPOrb";
            go.transform.position = position + Vector3.up * 0.3f;

            var orb = go.AddComponent<XPOrb>();
            orb.XPAmount = xpAmount;
            return orb;
        }

        void Start()
        {
            if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
        }

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

            if (_player == null)
            {
                if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= PickupRadius)
            {
                var stats = _player.GetComponent<PlayerStats>();
                stats?.AddXP(XPAmount);
                Destroy(gameObject);
                return;
            }

            if (dist <= MagnetRadius)
            {
                transform.position = Vector3.MoveTowards(transform.position, _player.position, MagnetSpeed * Time.deltaTime);
            }
        }
    }
}
