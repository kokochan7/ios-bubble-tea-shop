using System;
using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Staff skill trees. Each team (baristas, cashiers, cleaners, stockers) earns experience as it
    /// works; every level gives a skill point to spend on that team's perks (three perks, three ranks
    /// each). Each shop has its own teams, kept in the shop's save file.
    /// </summary>
    public static class TeamSkills
    {
        public enum Role { Barista, Cashier, Cleaner, Stocker }

        public class Perk
        {
            public string Name;
            public string Description; // what one rank does
        }

        public const int MaxRank = 3;
        public const int MaxLevel = 9;

        public static readonly string[] RoleNames = { "BARISTAS", "CASHIERS", "CLEANERS", "STOCKERS" };

        public static readonly Perk[][] Perks =
        {
            new[]
            {
                new Perk { Name = "Quick Feet", Description = "Baristas walk 10% faster" },
                new Perk { Name = "Tireless", Description = "Breaks come half as often (rank 3: no breaks)" },
                new Perk { Name = "Jam Fixer", Description = "Baristas fix jammed bars on their own, sooner each rank" },
            },
            new[]
            {
                new Perk { Name = "Speedy Till", Description = "Cashiers work 15% faster" },
                new Perk { Name = "Charm", Description = "Customers tip 8% more" },
                new Perk { Name = "Lucky Register", Description = "5% chance a sale pays double" },
            },
            new[]
            {
                new Perk { Name = "Hustle", Description = "Cleaners work 15% faster" },
                new Perk { Name = "Spotless", Description = "Tables are wiped 50% faster" },
                new Perk { Name = "Tidy Tips", Description = "Tables leave 20% more money" },
            },
            new[]
            {
                new Perk { Name = "Hustle", Description = "Stockers walk 15% faster" },
                new Perk { Name = "Big Crates", Description = "Each crate holds 3 more scoops" },
                new Perk { Name = "Fresh Stock", Description = "Stocked bars work 5% faster" },
            },
        };

        private class Team
        {
            public int Xp;
            public readonly int[] Ranks = new int[3];
            public int Workers;
        }

        private static readonly Team[] teams = { new(), new(), new(), new() };

        /// <summary>Raised when a team gains a level or a perk.</summary>
        public static event Action Changed;

        // ---------------------------------------------------------------- reading

        public static int Rank(Role role, int perk) => teams[(int)role].Ranks[perk];
        public static int Workers(Role role) => teams[(int)role].Workers;
        public static int Xp(Role role) => teams[(int)role].Xp;

        /// <summary>Experience needed to go from this level to the next.</summary>
        public static int XpForNext(int level) => 20 + 15 * level;

        public static int Level(Role role)
        {
            int xp = teams[(int)role].Xp;
            int level = 0;
            while (level < MaxLevel && xp >= XpForNext(level))
            {
                xp -= XpForNext(level);
                level++;
            }
            return level;
        }

        /// <summary>Experience earned towards the next level (and how much it needs).</summary>
        public static int XpIntoLevel(Role role)
        {
            int xp = teams[(int)role].Xp;
            for (int level = 0; level < Level(role); level++) xp -= XpForNext(level);
            return xp;
        }

        public static int PointsLeft(Role role)
        {
            int spent = 0;
            foreach (int rank in teams[(int)role].Ranks) spent += rank;
            return Level(role) - spent;
        }

        // ---------------------------------------------------------------- what the perks do

        /// <summary>Speed for a worker of this type (walking and working), from Quick Feet / Speedy Till / Hustle.</summary>
        public static float SpeedFor(Component worker)
        {
            switch (worker)
            {
                case BubbleTea.Staff.Barista _: return 1f + 0.10f * Rank(Role.Barista, 0);
                case BubbleTea.Staff.Cashier _: return 1f + 0.15f * Rank(Role.Cashier, 0);
                case BubbleTea.Staff.Cleaner _: return 1f + 0.15f * Rank(Role.Cleaner, 0);
                case BubbleTea.Staff.Stocker _: return 1f + 0.15f * Rank(Role.Stocker, 0);
                default: return 1f;
            }
        }

        /// <summary>How much longer baristas work between breaks (infinite once Tireless is maxed).</summary>
        public static float BreakSpacing
        {
            get
            {
                int rank = Rank(Role.Barista, 1);
                return rank >= MaxRank ? float.PositiveInfinity : 1f + rank;
            }
        }

        /// <summary>Seconds before a jammed bar with a barista at it fixes itself (infinite without Jam Fixer).</summary>
        public static float JamFixSeconds
        {
            get
            {
                int rank = Rank(Role.Barista, 2);
                return rank == 0 ? float.PositiveInfinity : 16f - 4f * rank;
            }
        }

        public static float TipBonus => 0.08f * Rank(Role.Cashier, 1);
        public static float DoubleSaleChance => 0.05f * Rank(Role.Cashier, 2);
        public static float CleanSpeed => 1f + 0.5f * Rank(Role.Cleaner, 1);
        public static float TableCashBonus => 0.2f * Rank(Role.Cleaner, 2);
        public static int ExtraCrateScoops => 3 * Rank(Role.Stocker, 1);
        public static float StockedBarSpeed => 1f + 0.05f * Rank(Role.Stocker, 2);

        // ---------------------------------------------------------------- changing

        /// <summary>Staff call this as they start (+1) and stop (-1) working in the shop.</summary>
        public static void CountWorker(Role role, int change) => teams[(int)role].Workers = Mathf.Max(0, teams[(int)role].Workers + change);

        /// <summary>Gives a team experience if it has anyone working.</summary>
        public static void AddXp(Role role, int amount)
        {
            Team team = teams[(int)role];
            if (team.Workers <= 0 || amount <= 0 || Level(role) >= MaxLevel) return;
            int before = Level(role);
            team.Xp += amount;
            int after = Level(role);
            if (after > before)
            {
                GameEvents.RaiseAnnouncement($"{Title(role)} team reached level {after}!\nSpend the skill point in TEAM.", new Color(0.62f, 0.45f, 0.88f));
                Changed?.Invoke();
            }
        }

        /// <summary>Spends a skill point on a perk. False if there's no point left or it's maxed.</summary>
        public static bool Learn(Role role, int perk)
        {
            Team team = teams[(int)role];
            if (PointsLeft(role) <= 0 || team.Ranks[perk] >= MaxRank) return false;
            team.Ranks[perk]++;
            Changed?.Invoke();
            return true;
        }

        public static string Title(Role role)
        {
            string name = RoleNames[(int)role];
            return name.Substring(0, 1) + name.Substring(1, name.Length - 2).ToLowerInvariant();
        }

        // ---------------------------------------------------------------- saving

        [Serializable]
        public class TeamSave
        {
            public int role;
            public int xp;
            public int[] ranks = new int[3];
        }

        public static List<TeamSave> Snapshot()
        {
            var list = new List<TeamSave>();
            for (int i = 0; i < teams.Length; i++)
                list.Add(new TeamSave { role = i, xp = teams[i].Xp, ranks = (int[])teams[i].Ranks.Clone() });
            return list;
        }

        /// <summary>Puts a shop's teams back (or starts them fresh when there's nothing saved).</summary>
        public static void Restore(List<TeamSave> saved)
        {
            foreach (Team team in teams)
            {
                team.Xp = 0;
                Array.Clear(team.Ranks, 0, team.Ranks.Length);
            }
            if (saved != null)
            {
                foreach (TeamSave entry in saved)
                {
                    if (entry == null || entry.role < 0 || entry.role >= teams.Length) continue;
                    teams[entry.role].Xp = Mathf.Max(0, entry.xp);
                    if (entry.ranks == null) continue;
                    for (int p = 0; p < 3 && p < entry.ranks.Length; p++) teams[entry.role].Ranks[p] = Mathf.Clamp(entry.ranks[p], 0, MaxRank);
                }
            }
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            foreach (Team team in teams)
            {
                team.Xp = 0;
                team.Workers = 0;
                Array.Clear(team.Ranks, 0, team.Ranks.Length);
            }
            Changed = null;
        }
    }
}
