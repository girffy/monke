using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Pickups
{
    public class XPOrb : MonoBehaviour
    {
        public float XPAmount = 3f;
        public float MagnetRadius = 4.5f;
        public float MagnetSpeed = 9f;
        // Measured from the gorilla's centre, and the gorilla is wide — the
        // old 0.45 meant an orb could visibly touch its fur and not count.
        public float PickupRadius = 0.9f;

        Transform _player;
        PlayerStats _stats;
        bool _magnetised;
        float _magnetSpeed;

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
            CachePlayer();
        }

        void CachePlayer()
        {
            if (PlayerController.Instance == null) return;
            _player = PlayerController.Instance.transform;
            _stats = PlayerController.Instance.GetComponent<PlayerStats>();
        }

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

            if (_player == null)
            {
                CachePlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= PickupRadius)
            {
                _stats?.AddXP(XPAmount);
                Sfx.XPPickup(transform.position);
                Destroy(gameObject);
                return;
            }

            float radius = MagnetRadius * (_stats != null ? _stats.PickupRadiusMultiplier : 1f);

            // Once an orb has been pulled it stays pulled and keeps
            // accelerating, so backing off mid-collection doesn't strand a
            // trail of half-gathered orbs behind you.
            if (!_magnetised && dist <= radius)
            {
                _magnetised = true;
                _magnetSpeed = MagnetSpeed * 0.35f;
            }

            if (_magnetised)
            {
                _magnetSpeed = Mathf.MoveTowards(_magnetSpeed, MagnetSpeed * 2.2f, MagnetSpeed * 3f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, _player.position, _magnetSpeed * Time.deltaTime);
            }
        }
    }
}
