using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Player;
using GorillaSurvivors.Player.Abilities;

namespace GorillaSurvivors.Core
{
    // What you spend round-clear points on.
    //
    // This replaced a pool of three random rewards drawn between rounds,
    // which had become redundant with levelling: both handed out the same
    // flat "+15% damage" style bonuses, so clearing a round felt like a
    // slower, less frequent level-up rather than a decision.
    //
    // A tree fixes that by making the choices about your abilities and about
    // each other. Every branch is a single ability, nodes unlock strictly
    // left-to-right along their branch, and points can be banked, so going
    // deep on one ability costs you breadth across the others.
    public class TechNode
    {
        public string Id;
        public string Title;
        public string Description;
        // Ranks beyond the first re-apply the same effect, which is how the
        // plain "+4 damage" filler nodes stack.
        public int MaxRank = 1;
        public Action<GameObject> Apply;
        // Filled in by TechBranch: every node needs the one before it.
        public string Requires;
    }

    public class TechBranch
    {
        public string Name;
        public Color Tint;
        public readonly List<TechNode> Nodes = new List<TechNode>();

        public TechBranch(string name, Color tint, params TechNode[] nodes)
        {
            Name = name;
            Tint = tint;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i > 0) nodes[i].Requires = nodes[i - 1].Id;
                Nodes.Add(nodes[i]);
            }
        }
    }

    public static class TechTree
    {
        static List<TechBranch> _branches;

        public static List<TechBranch> Branches => _branches ?? (_branches = Build());

        public static TechNode Find(string id)
        {
            foreach (var branch in Branches)
            {
                foreach (var node in branch.Nodes)
                {
                    if (node.Id == id) return node;
                }
            }
            return null;
        }

        static T Get<T>(GameObject p) where T : Component => p.GetComponent<T>();

        static List<TechBranch> Build()
        {
            return new List<TechBranch>
            {
                // ---- LMB: the fast poke -------------------------------
                new TechBranch("Swipe (LMB)", new Color(0.86f, 0.78f, 0.30f),
                    new TechNode
                    {
                        Id = "swipe_dmg", Title = "Sharpened Claws", MaxRank = 3,
                        Description = "+4 swipe damage",
                        Apply = p => Get<QuickSwipeAttack>(p).BaseDamage += 4f,
                    },
                    new TechNode
                    {
                        Id = "swipe_arc", Title = "Wide Sweep",
                        Description = "The swipe cuts a much wider arc",
                        Apply = p => Get<QuickSwipeAttack>(p).ArcDegrees = 150f,
                    },
                    new TechNode
                    {
                        Id = "swipe_reach", Title = "Long Arms", MaxRank = 2,
                        Description = "+0.45 swipe reach",
                        Apply = p => Get<QuickSwipeAttack>(p).Reach += 0.45f,
                    },
                    new TechNode
                    {
                        Id = "swipe_bleed", Title = "Rake",
                        Description = "Swiped enemies bleed for half the hit again over 2s",
                        Apply = p => Get<QuickSwipeAttack>(p).BleedFraction = 0.5f,
                    },
                    new TechNode
                    {
                        Id = "swipe_double", Title = "Flurry",
                        Description = "Every swipe lands a second time for 60%",
                        Apply = p => Get<QuickSwipeAttack>(p).SecondHitFraction = 0.6f,
                    }),

                // ---- RMB: the heavy, committed hit --------------------
                new TechBranch("Slam (RMB)", new Color(0.82f, 0.45f, 0.30f),
                    new TechNode
                    {
                        Id = "slam_dmg", Title = "Heavy Fists", MaxRank = 3,
                        Description = "+8 slam damage",
                        Apply = p => Get<PlayerAttack>(p).BaseDamage += 8f,
                    },
                    new TechNode
                    {
                        Id = "slam_stun", Title = "Concussive",
                        Description = "Slammed enemies are stunned for 0.8s",
                        Apply = p => Get<PlayerAttack>(p).StunSeconds = 0.8f,
                    },
                    new TechNode
                    {
                        Id = "slam_charge", Title = "Wind Up",
                        Description = "Hold RMB to charge: up to 2.2x damage and a longer reach",
                        Apply = p => Get<PlayerAttack>(p).ChargeEnabled = true,
                    },
                    new TechNode
                    {
                        Id = "slam_cd", Title = "Follow Through", MaxRank = 2,
                        Description = "-20% slam cooldown",
                        Apply = p => Get<PlayerAttack>(p).BaseCooldown *= 0.8f,
                    },
                    new TechNode
                    {
                        Id = "slam_quake", Title = "Earthshaker",
                        Description = "The slam sends out a wide shockwave for half damage",
                        Apply = p => Get<PlayerAttack>(p).QuakeEnabled = true,
                    }),

                // ---- SPACE: the dash ---------------------------------
                new TechBranch("Dash (SPC)", new Color(0.40f, 0.70f, 0.86f),
                    new TechNode
                    {
                        Id = "dash_cd", Title = "Light Feet", MaxRank = 2,
                        Description = "-20% dash cooldown",
                        Apply = p => Get<PlayerController>(p).DashCooldown *= 0.8f,
                    },
                    new TechNode
                    {
                        Id = "dash_through", Title = "Barge",
                        Description = "Dash passes straight through enemies instead of being stopped",
                        Apply = p => Get<PlayerController>(p).DashPassesThrough = true,
                    },
                    new TechNode
                    {
                        Id = "dash_far", Title = "Ground Eater",
                        Description = "+40% dash distance",
                        Apply = p => Get<PlayerController>(p).DashDuration *= 1.4f,
                    },
                    new TechNode
                    {
                        Id = "dash_dmg", Title = "Freight Train", MaxRank = 2,
                        Description = "Dashing deals +14 damage to everything you pass through",
                        Apply = p => Get<PlayerController>(p).DashDamage += 14f,
                    },
                    new TechNode
                    {
                        Id = "dash_refresh", Title = "Momentum",
                        Description = "Finishing a dash instantly readies the swipe and the slam",
                        Apply = p => Get<PlayerController>(p).DashRefreshesAttacks = true,
                    }),

                // ---- Q: chest beat -----------------------------------
                new TechBranch("Chest Beat (Q)", new Color(0.78f, 0.55f, 0.82f),
                    new TechNode
                    {
                        Id = "beat_unlock", Title = "Unlock: Chest Beat",
                        Description = "Q — rear up and pound out shockwaves (roots you)",
                        Apply = p => Get<ChestBeatAbility>(p).Unlocked = true,
                    },
                    new TechNode
                    {
                        Id = "beat_dmg", Title = "Thunderous", MaxRank = 3,
                        Description = "+4 damage per pulse",
                        Apply = p => Get<ChestBeatAbility>(p).BaseDamage += 4f,
                    },
                    new TechNode
                    {
                        Id = "beat_pulses", Title = "Drum Roll", MaxRank = 2,
                        Description = "+1 pulse",
                        Apply = p => Get<ChestBeatAbility>(p).PulseCount += 1,
                    },
                    new TechNode
                    {
                        Id = "beat_iron", Title = "Unshakeable",
                        Description = "Invulnerable for the whole chest beat",
                        Apply = p => Get<ChestBeatAbility>(p).InvulnerableWhileBeating = true,
                    },
                    new TechNode
                    {
                        Id = "beat_march", Title = "Rolling Thunder",
                        Description = "Walk at half speed while beating instead of being rooted",
                        Apply = p => Get<ChestBeatAbility>(p).MoveFraction = 0.5f,
                    }),

                // ---- E: dung toss ------------------------------------
                new TechBranch("Dung Toss (E)", new Color(0.62f, 0.52f, 0.34f),
                    new TechNode
                    {
                        Id = "dung_unlock", Title = "Unlock: Dung Toss",
                        Description = "E — hurl dung; splash damage and a slowing patch",
                        Apply = p => Get<DungTossAbility>(p).Unlocked = true,
                    },
                    new TechNode
                    {
                        Id = "dung_dmg", Title = "Packed Tight", MaxRank = 3,
                        Description = "+4 dung damage",
                        Apply = p => Get<DungTossAbility>(p).BaseDamage += 4f,
                    },
                    new TechNode
                    {
                        Id = "dung_charges", Title = "Stockpile", MaxRank = 2,
                        Description = "+1 stored throw",
                        Apply = p => Get<DungTossAbility>(p).MaxCharges += 1,
                    },
                    new TechNode
                    {
                        Id = "dung_spread", Title = "Handful", MaxRank = 2,
                        Description = "+1 extra clod thrown in a spread",
                        Apply = p => Get<DungTossAbility>(p).ExtraProjectiles += 1,
                    },
                    new TechNode
                    {
                        Id = "dung_rot", Title = "Foul",
                        Description = "Hit enemies rot, taking the impact damage again over 3s",
                        Apply = p => Get<DungTossAbility>(p).RotFraction = 1f,
                    }),

                // ---- Passives ----------------------------------------
                new TechBranch("Hide (passive)", new Color(0.55f, 0.72f, 0.48f),
                    new TechNode
                    {
                        Id = "hide_hp", Title = "Thicker Hide", MaxRank = 4,
                        Description = "+30 max HP, heal to full",
                        Apply = p => Get<PlayerStats>(p).AddPermanentMaxHP(30f),
                    },
                    new TechNode
                    {
                        Id = "hide_speed", Title = "Long Strides", MaxRank = 3,
                        Description = "+12% move speed",
                        Apply = p => Get<PlayerStats>(p).AddPermanentMoveSpeedBonus(0.12f),
                    },
                    new TechNode
                    {
                        Id = "hide_armor", Title = "Scarred", MaxRank = 3,
                        Description = "-12% damage taken",
                        Apply = p => Get<PlayerHealth>(p).AddDamageReduction(0.12f),
                    },
                    new TechNode
                    {
                        Id = "hide_greed", Title = "Forager", MaxRank = 2,
                        Description = "+30% XP and +50% pickup range",
                        Apply = p =>
                        {
                            Get<PlayerStats>(p).AddXPBonus(0.3f);
                            Get<PlayerStats>(p).AddPermanentPickupRadiusBonus(0.5f);
                        },
                    },
                    new TechNode
                    {
                        Id = "hide_regen", Title = "Old Wounds Close",
                        Description = "Regenerate 1.5% of max HP per second",
                        Apply = p => Get<PlayerHealth>(p).RegenPerSecondFraction += 0.015f,
                    }),
            };
        }
    }

    // Per-run state: how many points are banked and what has been taken.
    public class TechTreeState : MonoBehaviour
    {
        public int AvailablePoints { get; private set; }

        readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();

        public event Action OnChanged;

        public int RankOf(string id) => _ranks.TryGetValue(id, out int r) ? r : 0;

        public void GrantPoints(int count)
        {
            AvailablePoints += count;
            OnChanged?.Invoke();
        }

        // A node is available once its predecessor has at least one rank and
        // it hasn't been maxed out.
        public bool IsUnlocked(TechNode node)
        {
            return node.Requires == null || RankOf(node.Requires) > 0;
        }

        public bool CanTake(TechNode node)
        {
            return AvailablePoints > 0 && IsUnlocked(node) && RankOf(node.Id) < node.MaxRank;
        }

        public bool Take(TechNode node)
        {
            if (!CanTake(node)) return false;

            AvailablePoints--;
            _ranks[node.Id] = RankOf(node.Id) + 1;
            node.Apply?.Invoke(gameObject);
            OnChanged?.Invoke();
            return true;
        }
    }
}
