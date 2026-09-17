using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Player;
using GorillaSurvivors.Player.Abilities;

namespace GorillaSurvivors.Core
{
    // What you spend round-clear points on.
    //
    // This is a real tree, not a grid of columns. The version before it gated
    // a node on "anything in the row above", which meant no node was ever
    // downstream of a PARTICULAR choice — every leaf was reachable from every
    // path, so the whole thing read as a shopping list you picked an order
    // for. Here each node names its own parents, so taking Wind Up is what
    // puts Earthshaker in reach and skipping it closes that limb off.
    //
    // The shape per branch: a trunk splits into one limb per skill, each limb
    // forks into its two distinct ideas, and those forks REJOIN into that
    // skill's own payoff. The limb payoffs rejoin again into the branch
    // capstone, and the three branch capstones feed one final node.
    public class TechNode
    {
        public string Id;
        public string Title;
        public string Description;
        // A short tag naming which skill this belongs to, for the panel.
        public string Skill;

        // Ranks beyond the first re-apply the same effect. Kept to trunks and
        // limb roots, where flat filler does the least harm.
        public int MaxRank = 1;
        public Action<GameObject> Apply;

        // Parents. ANY ONE of them being owned opens this node — a join is a
        // place two paths meet, not a toll requiring both. Empty means this
        // is a root and is open from the start.
        public string[] Parents = EmptyParents;
        // The single exception: the grand capstone, which is the one place
        // all three branches are meant to actually converge.
        public bool RequiresAllParents;

        public int Row;
        public int Column;
        public TechBranch Branch;

        static readonly string[] EmptyParents = new string[0];
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

        static TechNode N(string id, string title, string skill, string description,
            int row, int column, Action<GameObject> apply, int maxRank = 1, params string[] parents)
        {
            return new TechNode
            {
                Id = id, Title = title, Skill = skill, Description = description,
                Row = row, Column = column, Apply = apply, MaxRank = maxRank,
                Parents = parents ?? new string[0],
            };
        }

        // Column positions are in HALF-columns so a trunk can sit centred
        // between its limbs: a two-limb branch is 4 half-columns wide with
        // limbs at 1 and 3, and the trunk at 2.
        static List<TechBranch> Build()
        {
            return new List<TechBranch>
            {
                // ================= MELEE =================
                new TechBranch("Melee", "Swipe (LMB)  ·  Slam (RMB)", new Color(0.88f, 0.72f, 0.32f), 4,
                    N("brawler", "Brawler", "Trunk", "+12% damage with both melee attacks", 0, 2,
                        p =>
                        {
                            Get<QuickSwipeAttack>(p).BaseDamage *= 1.12f;
                            Get<PlayerAttack>(p).BaseDamage *= 1.12f;
                        }, 3),

                    N("swipe_dmg", "Sharpened Claws", "Swipe", "+25% swipe damage", 1, 1,
                        p => Get<QuickSwipeAttack>(p).BaseDamage *= 1.25f, 3, "brawler"),
                    // The slam limb's gate. Everything below it needs the
                    // charge, so taking the limb IS taking Wind Up — the flat
                    // damage node that used to sit here was filler, and it
                    // let you reach the charge payoffs without the charge.
                    N("slam_charge", "Wind Up", "Slam", "Hold RMB to charge the slam: up to +40% radius", 1, 3,
                        p => Get<PlayerAttack>(p).ChargeEnabled = true, 1, "brawler"),

                    // Widens and thickens; deliberately does NOT extend. Reach
                    // used to grow here too, which pushed the inner edge of
                    // the swing forward and made it whiff enemies standing on
                    // the gorilla — an upgrade that covered less than before.
                    N("swipe_arc", "Wide Sweep", "Swipe", "Swipe sweeps +26° wider", 2, 0,
                        p => Get<QuickSwipeAttack>(p).ArcDegrees += 26f, 2, "swipe_dmg"),
                    N("swipe_bleed", "Rake", "Swipe",
                        "Swiped enemies are Vulnerable for 4s: they take +50% damage from everything", 2, 2,
                        p => Get<QuickSwipeAttack>(p).VulnerableSeconds = 4f, 1, "swipe_dmg"),
                    N("slam_core", "Focal Impact", "Slam",
                        "The inner third of the slam hits for +50%, marked out while you charge", 2, 4,
                        p => Get<PlayerAttack>(p).CoreImpactEnabled = true, 1, "slam_charge"),
                    N("slam_stun", "Concussive", "Slam", "Slammed enemies are stunned for 0.5s, +0.3s per rank", 2, 6,
                        p =>
                        {
                            var a = Get<PlayerAttack>(p);
                            a.StunSeconds += a.StunSeconds > 0f ? 0.3f : 0.5f;
                        }, 2, "slam_charge"),

                    N("swipe_double", "Ambidextrous", "Swipe join", "Every swipe lands a second time for 60%, off the other hand", 3, 1,
                        p => Get<QuickSwipeAttack>(p).SecondHitFraction = 0.6f, 1, "swipe_arc", "swipe_bleed"),
                    N("slam_braced", "Braced", "Slam join", "Take 45% less damage while holding a charge", 3, 3,
                        p => Get<PlayerAttack>(p).ChargeDamageReduction = 0.45f, 1, "slam_core", "slam_stun"),

                    N("melee_capstone", "Silverback", "Capstone",
                        "Melee kills build Frenzy: +8% melee damage each, up to 5, fading 3s after your last kill", 4, 2,
                        p => Get<PlayerPerks>(p).FrenzyEnabled = true, 1, "swipe_double", "slam_braced")),

                // ================= ABILITIES =================
                new TechBranch("Abilities", "Dash (SPC)  ·  Chest Beat (Q)  ·  Dung Toss (E)", new Color(0.45f, 0.72f, 0.90f), 6,
                    N("instinct", "Instinct", "Trunk", "-12% cooldown on every ability", 0, 3,
                        p => Get<PlayerStats>(p).AddPermanentCooldownReduction(0.12f), 2),

                    N("dash_cd", "Light Feet", "Dash", "-20% dash cooldown", 1, 1,
                        p => Get<PlayerController>(p).DashCooldown *= 0.8f, 2, "instinct"),
                    N("beat_unlock", "Chest Beat", "Unlock Q", "Unlocks Chest Beat (Q): rear up and pound out shockwaves", 1, 3,
                        p => Get<ChestBeatAbility>(p).Unlocked = true, 1, "instinct"),
                    N("dung_unlock", "Dung Toss", "Unlock E", "Unlocks Dung Toss (E): splash damage and a slowing patch", 1, 5,
                        p => Get<DungTossAbility>(p).Unlocked = true, 1, "instinct"),

                    N("dash_stamina", "Stamina", "Dash", "+1 stored dash", 2, 0,
                        p => Get<PlayerController>(p).MaxDashCharges += 1, 2, "dash_cd"),
                    N("dash_far", "Ground Eater", "Dash", "+40% dash distance", 2, 2,
                        p => Get<PlayerController>(p).DashDuration *= 1.4f, 1, "dash_cd"),
                    N("beat_dmg", "Thunderous", "Beat", "+30% chest beat damage per pulse", 2, 4,
                        p => Get<ChestBeatAbility>(p).BaseDamage *= 1.3f, 3, "beat_unlock"),
                    N("beat_pulses", "Drum Roll", "Beat", "+1 chest beat pulse", 2, 6,
                        p => Get<ChestBeatAbility>(p).PulseCount += 1, 2, "beat_unlock"),
                    // Was flat damage; it is the rot now, moved off Foul so
                    // that node isn't carrying two effects at once.
                    N("dung_dmg", "Packed Tight", "Toss",
                        "Dung hits rot for the impact damage again over 3s", 2, 8,
                        p => Get<DungTossAbility>(p).RotFraction = 1f, 1, "dung_unlock"),
                    N("dung_charges", "Stockpile", "Toss", "+1 stored dung throw", 2, 10,
                        p => Get<DungTossAbility>(p).MaxCharges += 1, 3, "dung_unlock"),

                    N("dash_through", "Barge", "Dash join", "Dash passes straight through enemies", 3, 1,
                        p => Get<PlayerController>(p).DashPassesThrough = true, 1, "dash_stamina", "dash_far"),
                    N("beat_march", "Rolling Thunder", "Beat join", "Walk at half speed during a chest beat, and stay invulnerable throughout", 3, 3,
                        p =>
                        {
                            var b = Get<ChestBeatAbility>(p);
                            b.MoveFraction = 0.5f;
                            b.InvulnerableWhileBeating = true;
                        }, 1, "beat_dmg", "beat_pulses"),
                    // The spread alone. Carrying the rot as well made this one
                    // node the whole Toss limb's payoff twice over.
                    N("dung_rot", "Foul", "Toss join", "+1 clod per throw, fanned either side", 3, 5,
                        p => Get<DungTossAbility>(p).ExtraProjectiles += 1, 3, "dung_dmg", "dung_charges"),

                    N("ability_capstone", "Harvesting", "Capstone",
                        "-30% cooldown on everything, and every kill has a 50% chance to return a dung charge", 4, 3,
                        p =>
                        {
                            Get<PlayerStats>(p).AddPermanentCooldownReduction(0.3f);
                            Get<PlayerPerks>(p).HarvestingEnabled = true;
                        }, 1, "dash_through", "beat_march", "dung_rot")),

                // ================= HIDE =================
                new TechBranch("Hide", "Body  ·  Instinct", new Color(0.55f, 0.78f, 0.48f), 4,
                    N("hide_hp", "Thicker Hide", "Trunk", "+20% max HP, heal to full", 0, 2,
                        p => Get<PlayerStats>(p).AddPermanentMaxHPFraction(0.2f), 3),

                    N("hide_armor", "Scarred", "Body", "-12% damage taken", 1, 1,
                        p => Get<PlayerHealth>(p).AddDamageReduction(0.12f), 3, "hide_hp"),
                    N("hide_speed", "Long Strides", "Instinct", "+12% move speed", 1, 3,
                        p => Get<PlayerStats>(p).AddPermanentMoveSpeedBonus(0.12f), 3, "hide_hp"),

                    N("hide_regen", "Old Wounds", "Body", "Regenerate 1.5% of max HP per second", 2, 0,
                        p => Get<PlayerHealth>(p).RegenPerSecondFraction += 0.015f, 2, "hide_armor"),
                    N("hide_thorns", "Bristling", "Body", "Anything that touches you takes 12 damage", 2, 2,
                        p => Get<PlayerHealth>(p).ThornsDamage += 12f, 2, "hide_armor"),
                    N("hide_iframes", "Hard to Pin", "Instinct", "+0.3s of invulnerability after being hit", 2, 4,
                        p => Get<PlayerHealth>(p).BonusHitInvulnerability += 0.3f, 2, "hide_speed"),
                    N("hide_greed", "Forager", "Instinct", "+30% XP and +50% pickup range", 2, 6,
                        p =>
                        {
                            Get<PlayerStats>(p).AddXPBonus(0.3f);
                            Get<PlayerStats>(p).AddPermanentPickupRadiusBonus(0.5f);
                        }, 2, "hide_speed"),

                    N("hide_laststand", "Last Stand", "Body join",
                        "Once a round, a lethal hit leaves you at 1 HP and invulnerable for 3s", 3, 1,
                        p => Get<PlayerHealth>(p).HasLastStand = true, 1, "hide_regen", "hide_thorns"),
                    N("hide_knock", "Immovable", "Instinct join", "Being hit no longer knocks you back", 3, 3,
                        p => Get<PlayerController>(p).IgnoreKnockback = true, 1, "hide_iframes", "hide_greed"),

                    N("hide_capstone", "Carnivore", "Capstone",
                        "Every kill has a 30% chance to heal 5 HP", 4, 2,
                        p => Get<PlayerPerks>(p).CarnivoreEnabled = true, 1, "hide_laststand", "hide_knock")),
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

        // Points spent in one branch — shown on its tab so the player can
        // see at a glance where they've committed.
        public int PointsIn(TechBranch branch)
        {
            int total = 0;
            foreach (var node in branch.Nodes) total += RankOf(node.Id);
            return total;
        }

        public void GrantPoints(int count)
        {
            AvailablePoints += count;
            OnChanged?.Invoke();
        }

        // ANY ONE parent opens a node. A join is a place two paths meet, not
        // a toll requiring both of them — requiring both would force you to
        // buy the fork you didn't want, which is the opposite of branching.
        // The grand capstone is the single exception.
        public bool IsUnlocked(TechNode node)
        {
            if (node.Parents == null || node.Parents.Length == 0) return true;

            if (node.RequiresAllParents)
            {
                foreach (var parent in node.Parents)
                {
                    if (RankOf(parent) == 0) return false;
                }
                return true;
            }

            foreach (var parent in node.Parents)
            {
                if (RankOf(parent) > 0) return true;
            }
            return false;
        }

        public string LockReason(TechNode node)
        {
            if (node.Parents == null || node.Parents.Length == 0) return null;

            var names = new List<string>();
            foreach (var parent in node.Parents)
            {
                var found = TechTree.Find(parent);
                if (found != null) names.Add(found.Title);
            }
            if (names.Count == 0) return null;

            string joiner = node.RequiresAllParents ? " + " : " or ";
            return "needs " + string.Join(joiner, names);
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
