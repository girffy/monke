using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;
using GorillaSurvivors.UI;

namespace GorillaSurvivors.Pickups
{
    public enum PowerupType
    {
        Banana,     // instant heal
        Adrenaline, // temporary move speed + attack speed
        Rampage,    // temporary damage multiplier
        Haste,      // temporary cooldown reduction (Dash/Roar/Charge/LMB)
        AreaBoost   // temporary AoE size increase
    }

    public class PowerupPickup : MonoBehaviour
    {
        public PowerupType Type;
        public float PickupRadius = 0.55f;

        Transform _player;
        Transform _visual;

        struct Info
        {
            public string Label;
            public string ToastText;
        }

        static readonly System.Collections.Generic.Dictionary<PowerupType, Info> InfoTable = new System.Collections.Generic.Dictionary<PowerupType, Info>
        {
            { PowerupType.Banana, new Info { Label = "Banana", ToastText = "Banana! +30 HP" } },
            { PowerupType.Adrenaline, new Info { Label = "Adrenaline", ToastText = "Adrenaline! Move & attack speed up (8s)" } },
            { PowerupType.Rampage, new Info { Label = "Rampage", ToastText = "Rampage! Damage up (8s)" } },
            { PowerupType.Haste, new Info { Label = "Haste", ToastText = "Haste! Cooldowns down (10s)" } },
            { PowerupType.AreaBoost, new Info { Label = "Area Boost", ToastText = "Area Boost! AoE size up (10s)" } },
        };

        public static PowerupPickup SpawnRandom(Vector3 position)
        {
            var type = (PowerupType)Random.Range(0, System.Enum.GetValues(typeof(PowerupType)).Length);
            return Spawn(position, type);
        }

        public static PowerupPickup Spawn(Vector3 position, PowerupType type)
        {
            GameObject visual = type switch
            {
                PowerupType.Banana => Blocky3DArt.Banana(),
                PowerupType.Adrenaline => Blocky3DArt.Adrenaline(),
                PowerupType.Rampage => Blocky3DArt.Rampage(),
                PowerupType.Haste => Blocky3DArt.Haste(),
                PowerupType.AreaBoost => Blocky3DArt.AreaBoost(),
                _ => Blocky3DArt.Banana(),
            };

            var root = new GameObject($"Powerup_{type}");
            root.transform.position = position;
            visual.transform.SetParent(root.transform, false);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(root.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelGO.transform.rotation = CameraFollow.LabelRotation;
            var textMesh = labelGO.AddComponent<TextMesh>();
            textMesh.text = InfoTable[type].Label;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            var pickup = root.AddComponent<PowerupPickup>();
            pickup.Type = type;
            pickup._visual = visual.transform;
            return pickup;
        }

        void Start()
        {
            if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
        }

        void Update()
        {
            if (_visual != null) _visual.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);

            if (_player == null)
            {
                if (PlayerController.Instance != null) _player = PlayerController.Instance.transform;
                return;
            }

            if (Vector3.Distance(transform.position, _player.position) <= PickupRadius)
            {
                Apply(_player);
                Destroy(gameObject);
            }
        }

        void Apply(Transform player)
        {
            var health = player.GetComponent<PlayerHealth>();
            var stats = player.GetComponent<PlayerStats>();

            switch (Type)
            {
                case PowerupType.Banana:
                    health?.Heal(30f);
                    break;
                case PowerupType.Adrenaline:
                    stats?.ApplyTemporaryMoveSpeedBuff(0.5f, 8f);
                    stats?.ApplyTemporaryAttackSpeedBuff(0.6f, 8f);
                    break;
                case PowerupType.Rampage:
                    stats?.ApplyTemporaryDamageBuff(1f, 8f);
                    break;
                case PowerupType.Haste:
                    stats?.ApplyTemporaryCooldownBuff(0.4f, 10f);
                    break;
                case PowerupType.AreaBoost:
                    stats?.ApplyTemporaryAreaBuff(0.5f, 10f);
                    break;
            }

            HUDController.Instance?.ShowToast(InfoTable[Type].ToastText);
            Sfx.PowerupPickup(transform.position);
        }
    }
}
