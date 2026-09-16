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

        // Which row of its branch this sits on. Rows are the tree's depth:
        // a node needs SOME node on the row above it taken first, not one
        // specific parent, so a branch forks instead of being a queue.
        public int Row;
        // Which of the branch's columns it occupies, for layout.
        public int Column;

        // Extra gate: this many points spent anywhere in the same branch.
        // How the branch-defining payoffs at the bottom are earned.
        public int RequiresBranchPoints;

        // Set by TechBranch.
        public TechBranch Branch;
    }

    public class TechBranch
    {
        public string Name;
        public string Blurb;
        public Color Tint;
        public int Columns = 2;
        public readonly List<TechNode> Nodes = new List<TechNode>();

        public TechBranch(string name, string blurb, Color tint, int columns, params TechNode[] nodes)
        {
            Name = name;
            Blurb = blurb;
            Tint = tint;
            Columns = columns;
            foreach (var node in nodes)
            {
                node.Branch = this;
                Nodes.Add(node);
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

        static TechNode N(string id, string title, string description, int row, int column,
            Action<GameObject> apply, int maxRank = 1, int requiresBranchPoints = 0)
        {
            return new TechNode
            {
                Id = id, Title = title, Description = description,
                Row = row, Column = column, Apply = apply,
                MaxRank = maxRank, RequiresBranchPoints = requiresBranchPoints,
            };
        }

        static List<TechBranch> Build()
        {
            return new List<TechBranch>
            {
                // ---- Melee: the two mouse buttons --------------------
                new TechBranch("Melee", "Swipe  /  Slam", new Color(0.88f, 0.72f, 0.32f), 2,
                    N("swipe_dmg", "Sharpened Claws", "+4 swipe damage", 0, 0,
                        p => Get<QuickSwipeAttack>(p).BaseDamage += 4f, 3),
                    N("slam_dmg", "Heavy Fists", "+8 slam damage", 0, 1,
                        p => Get<PlayerAttack>(p).BaseDamage += 8f, 3),

                    N("swipe_arc", "Wide Sweep", "The swipe cuts a much wider arc", 1, 0,
                        p => Get<QuickSwipeAttack>(p).ArcDegrees = 155f),
                    N("slam_stun", "Concussive", "Slammed enemies are stunned for 0.8s", 1, 1,
                        p => Get<PlayerAttack>(p).StunSeconds = 0.8f),

                    N("swipe_reach", "Long Arms", "+0.4 swipe reach and a wider band", 2, 0,
                        p => { var s = Get<QuickSwipeAttack>(p); s.Reach += 0.4f; s.BandWidth += 0.15f; }, 2),
                    N("slam_charge", "Wind Up", "Hold RMB to charge: up to 2.2x damage and reach", 2, 1,
                        p => Get<PlayerAttack>(p).ChargeEnabled = true),

                    N("swipe_bleed", "Rake", "Swiped enemies bleed for half the hit again over 2s", 3, 0,
                        p => Get<QuickSwipeAttack>(p).BleedFraction = 0.5f),
                    N("slam_cd", "Follow Through", "-20% slam cooldown", 3, 1,
                        p => Get<PlayerAttack>(p).BaseCooldown *= 0.8f, 2),

                    N("swipe_double", "Flurry", "Every swipe lands a second time for 60%", 4, 0,
                        p => Get<QuickSwipeAttack>(p).SecondHitFraction = 0.6f),
                    N("slam_quake", "Earthshaker", "The slam sends a shockwave out all around for half damage", 4, 1,
                        p => Get<PlayerAttack>(p).QuakeEnabled = true),

                    // Capstone. Deliberately gated on POINTS SPENT IN THE
                    // BRANCH rather than on one parent, so it is earned by
                    // committing to melee rather than by walking one column.
                    N("melee_capstone", "Overwhelm", "+40% damage with both melee attacks", 5, 0,
                        p =>
                        {
                            Get<QuickSwipeAttack>(p).BaseDamage *= 1.4f;
                            Get<PlayerAttack>(p).BaseDamage *= 1.4f;
                        }, 1, 8)),

                // ---- Abilities: the three cooldowns ------------------
                new TechBranch("Abilities", "Dash  /  Beat  /  Toss", new Color(0.45f, 0.72f, 0.90f), 3,
                    N("dash_cd", "Light Feet", "-20% dash cooldown", 0, 0,
                        p => Get<PlayerController>(p).DashCooldown *= 0.8f, 2),
                    N("beat_unlock", "Unlock: Chest Beat", "Q — rear up and pound out shockwaves", 0, 1,
                        p => Get<ChestBeatAbility>(p).Unlocked = true),
                    N("dung_unlock", "Unlock: Dung Toss", "E — hurl dung; splash damage and a slowing patch", 0, 2,
                        p => Get<DungTossAbility>(p).Unlocked = true),

                    N("dash_through", "Barge", "Dash passes straight through enemies", 1, 0,
                        p => Get<PlayerController>(p).DashPassesThrough = true),
                    N("beat_dmg", "Thunderous", "+4 damage per pulse", 1, 1,
                        p => Get<ChestBeatAbility>(p).BaseDamage += 4f, 3),
                    N("dung_dmg", "Packed Tight", "+4 dung damage", 1, 2,
                        p => Get<DungTossAbility>(p).BaseDamage += 4f, 3),

                    N("dash_far", "Ground Eater", "+40% dash distance", 2, 0,
                        p => Get<PlayerController>(p).DashDuration *= 1.4f),
                    N("beat_pulses", "Drum Roll", "+1 pulse", 2, 1,
                        p => Get<ChestBeatAbility>(p).PulseCount += 1, 2),
                    N("dung_charges", "Stockpile", "+1 stored throw", 2, 2,
                        p => Get<DungTossAbility>(p).MaxCharges += 1, 2),

                    N("dash_dmg", "Freight Train", "Dashing deals +14 damage to everything you pass through", 3, 0,
                        p => Get<PlayerController>(p).DashDamage += 14f, 2),
                    N("beat_iron", "Unshakeable", "Invulnerable for the whole chest beat", 3, 1,
                        p => Get<ChestBeatAbility>(p).InvulnerableWhileBeating = true),
                    N("dung_spread", "Handful", "+1 extra clod thrown in a spread", 3, 2,
                        p => Get<DungTossAbility>(p).ExtraProjectiles += 1, 2),

                    N("dash_refresh", "Momentum", "Finishing a dash instantly readies the slam", 4, 0,
                        p => Get<PlayerController>(p).DashRefreshesAttacks = true),
                    N("beat_march", "Rolling Thunder", "Walk at half speed while beating instead of being rooted", 4, 1,
                        p => Get<ChestBeatAbility>(p).MoveFraction = 0.5f),
                    N("dung_rot", "Foul", "Hit enemies rot, taking the impact damage again over 3s", 4, 2,
                        p => Get<DungTossAbility>(p).RotFraction = 1f),

                    N("ability_capstone", "Second Wind", "-30% cooldown on every ability", 5, 1,
                        p => Get<PlayerStats>(p).AddPermanentCooldownReduction(0.3f), 1, 9)),

                // ---- Hide: staying alive ----------------------------
                new TechBranch("Hide", "Body  /  Instinct", new Color(0.55f, 0.78f, 0.48f), 2,
                    N("hide_hp", "Thicker Hide", "+30 max HP, heal to full", 0, 0,
                        p => Get<PlayerStats>(p).AddPermanentMaxHP(30f), 4),
                    N("hide_speed", "Long Strides", "+12% move speed", 0, 1,
                        p => Get<PlayerStats>(p).AddPermanentMoveSpeedBonus(0.12f), 3),

                    N("hide_armor", "Scarred", "-12% damage taken", 1, 0,
                        p => Get<PlayerHealth>(p).AddDamageReduction(0.12f), 3),
                    N("hide_greed", "Forager", "+30% XP and +50% pickup range", 1, 1,
                        p =>
                        {
                            Get<PlayerStats>(p).AddXPBonus(0.3f);
                            Get<PlayerStats>(p).AddPermanentPickupRadiusBonus(0.5f);
                        }, 2),

                    N("hide_regen", "Old Wounds Close", "Regenerate 1.5% of max HP per second", 2, 0,
                        p => Get<PlayerHealth>(p).RegenPerSecondFraction += 0.015f),
                    N("hide_iframes", "Hard to Pin", "+0.3s of invulnerability after being hit", 2, 1,
                        p => Get<PlayerHealth>(p).BonusHitInvulnerability += 0.3f, 2),

                    N("hide_thorns", "Bristling", "Anything that touches you takes 12 damage", 3, 0,
                        p => Get<PlayerHealth>(p).ThornsDamage += 12f, 2),
                    N("hide_knock", "Immovable", "Being hit no longer knocks you back", 3, 1,
                        p => Get<PlayerController>(p).IgnoreKnockback = true),

                    N("hide_capstone", "Last Stand", "Once a round, a lethal hit leaves you at 1 HP and invulnerable for 3s", 4, 0,
                        p => Get<PlayerHealth>(p).HasLastStand = true, 1, 8)),
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

        // Points spent anywhere in one branch — what the capstones are gated
        // on, so committing to a branch is what earns its payoff rather than
        // walking one specific column to the bottom.
        public int PointsIn(TechBranch branch)
        {
            int total = 0;
            foreach (var node in branch.Nodes) total += RankOf(node.Id);
            return total;
        }

        // A node opens once ANY node on the row above it has been taken. That
        // is what makes this a tree rather than a queue: a row is a rank of
        // choices, and reaching the next rank costs one of them, not a
        // specific one.
        public bool IsUnlocked(TechNode node)
        {
            if (node.RequiresBranchPoints > 0 && PointsIn(node.Branch) < node.RequiresBranchPoints) return false;
            if (node.Row == 0) return true;

            foreach (var other in node.Branch.Nodes)
            {
                if (other.Row == node.Row - 1 && RankOf(other.Id) > 0) return true;
            }
            return false;
        }

        // What a locked node is still waiting on, for the panel to show.
        public string LockReason(TechNode node)
        {
            if (node.RequiresBranchPoints > 0 && PointsIn(node.Branch) < node.RequiresBranchPoints)
            {
                return $"needs {node.RequiresBranchPoints} points in {node.Branch.Name} ({PointsIn(node.Branch)}/{node.RequiresBranchPoints})";
            }
            return node.Row == 0 ? null : "needs anything on the row above";
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
