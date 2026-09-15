using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GorillaSurvivors.Player;
using GorillaSurvivors.Player.Abilities;

namespace GorillaSurvivors.Core
{
    public struct RoundReward
    {
        public string Title;
        public string Description;
        public Action<GameObject> Apply;
    }

    // The pool of round-clear rewards. Ability unlocks drop out of the pool
    // once already unlocked; everything else can be picked repeatedly.
    public static class RoundRewardPool
    {
        public static List<RoundReward> RollChoices(GameObject player, int count = 3)
        {
            var pool = BuildPool(player);

            // Fisher-Yates shuffle, then take `count`.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = pool[i];
                pool[i] = pool[j];
                pool[j] = temp;
            }

            return pool.Take(count).ToList();
        }

        static List<RoundReward> BuildPool(GameObject player)
        {
            var stats = player.GetComponent<PlayerStats>();
            var roar = player.GetComponent<RoarAbility>();
            var charge = player.GetComponent<ChargeAbility>();

            var pool = new List<RoundReward>
            {
                new RoundReward
                {
                    Title = "Thicker Hide",
                    Description = "+25 Max HP, full heal",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentMaxHP(25f),
                },
                new RoundReward
                {
                    Title = "Heavy Hands",
                    Description = "+15% damage",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentDamageBonus(0.15f),
                },
                new RoundReward
                {
                    Title = "Fast Twitch",
                    Description = "+15% attack speed",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentAttackSpeedBonus(0.15f),
                },
                new RoundReward
                {
                    Title = "Sprinter",
                    Description = "+15% move speed",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentMoveSpeedBonus(0.15f),
                },
                new RoundReward
                {
                    Title = "Quick Recovery",
                    Description = "-15% ability cooldowns (Dash/Roar/Charge/LMB)",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentCooldownReduction(0.15f),
                },
                new RoundReward
                {
                    Title = "Wide Reach",
                    Description = "+20% attack area size",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentAreaBonus(0.2f),
                },
                new RoundReward
                {
                    Title = "Scavenger",
                    Description = "+40% XP pickup range",
                    Apply = p => p.GetComponent<PlayerStats>().AddPermanentPickupRadiusBonus(0.4f),
                },
            };

            if (roar != null && !roar.Unlocked)
            {
                pool.Add(new RoundReward
                {
                    Title = "Unlock: Roar",
                    Description = "Press Q — AoE damage + knockback around you",
                    Apply = p => p.GetComponent<RoarAbility>().Unlocked = true,
                });
            }

            if (charge != null && !charge.Unlocked)
            {
                pool.Add(new RoundReward
                {
                    Title = "Unlock: Charge",
                    Description = "Press E — dash-attack that damages everything in your path",
                    Apply = p => p.GetComponent<ChargeAbility>().Unlocked = true,
                });
            }

            return pool;
        }
    }
}
