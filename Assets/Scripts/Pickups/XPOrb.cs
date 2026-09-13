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
        public float PickupRadius = 0.35f;

        Transform _player;

        public static XPOrb Spawn(Vector3 position, float xpAmount)
        {
            var go = new GameObject("XPOrb");
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatureArt.Gem();
            renderer.sortingOrder = 3;

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
            if (_player == null)
            {
                if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
                return;
            }

            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= PickupRadius)
            {
                var stats = _player.GetComponent<PlayerStats>();
                stats?.AddXP(XPAmount);
                Destroy(gameObject);
                return;
            }

            if (dist <= MagnetRadius)
            {
                transform.position = Vector2.MoveTowards(transform.position, _player.position, MagnetSpeed * Time.deltaTime);
            }
        }
    }
}
