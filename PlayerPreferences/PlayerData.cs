using System.Collections.Generic;
using System.Linq;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerData
    {
        private readonly PpPlugin plugin;

        public Player Player { get; }
        private Role role;
        public Role Role
        {
            get => role;
            private set
            {
                Rank = Record?[role] ?? -1;

                role = value;
            }
        }

        public PlayerRecord Record { get; }
        public int Rank { get; private set; }

        public PlayerData(Player player, Role role, PpPlugin plugin)
        {
            this.plugin = plugin;

            Player = player;
            Record = plugin.Preferences.Contains(player.SteamId) ? plugin.Preferences[player.SteamId] : null;

            Role = role;
        }

        public virtual float? Compare(PlayerData checker)
        {
            plugin.Debug($"Comparing {Player.Name} ({Role}) with {checker.Player.Name} ({checker.Role})");

            if (checker.Role == Role) // If this player was just swapped with checker and the checker has the same role we lost from the swap
            {
                return 0;
            }

            float? thisRank = Rank - Record?.RoleRating(checker.Role);
            float? otherRank = checker.Rank - checker.Record?.RoleRating(Role);

            if (thisRank == null)
            {
                if (otherRank == null)
                {
                    return 0;
                }

                if (plugin.DistributeAll)
                {
                    return otherRank - checker.Rank;
                }
                
                return -100;
               
            }

            if (otherRank == null)
            {
                if (plugin.DistributeAll)
                {
                    return thisRank - checker.Rank;
                }

                return -100;
            }

            plugin.Debug(string.Join(", ", Record.Preferences.Select(x => x.ToString())));
            plugin.Debug(string.Join(", ", checker.Record.Preferences.Select(x => x.ToString())));

            plugin.Debug("\n" +
                         $" -P1Rank: {Rank}\n" +
                         $" -P2Rank: {checker.Rank}\n" +
                         $" -P1OtherRank: {thisRank}\n" +
                         $" -P2OtherRank: {otherRank}\n" +
                         $" -P1Avg: {Record.AverageRank}\n" +
                         $" -P2Avg: {checker.Record.AverageRank}\n" +
                         $" -P1Delta: {thisRank}\n" +
                         $" -P2Delta: {otherRank}\n");
            
            float sumDelta = thisRank.Value + otherRank.Value;

            plugin.Debug("\n" +
                         $" -SumDelta: {sumDelta}\n");
            
            // If it is a net gain of rankings or the other player is getting demoted but is equal to or above the other rank
            return sumDelta;
        }

        public virtual void Swap(PlayerData other)
        {
            Role thisRole = Role;
            Role = other.Role;
            other.Role = thisRole;
        }
    }

    public class PlayerSortData : PlayerData
    {
        private readonly Dictionary<PlayerData, Role> recentlyCompared;

        public PlayerSortData(Player player, Role role, PpPlugin plugin) : base(player, role, plugin)
        {
            recentlyCompared = new Dictionary<PlayerData, Role>();
        }

        private void AddComparison(PlayerData data)
        {
            if (recentlyCompared.ContainsKey(data))
            {
                recentlyCompared[data] = data.Role;
            }
            else
            {
                recentlyCompared.Add(data, data.Role);
            }
        }

        private bool JustCompared(PlayerData data)
        {
            return recentlyCompared.ContainsKey(data) && recentlyCompared[data] == data.Role;
        }

        public override float? Compare(PlayerData checker)
        {
            if (JustCompared(checker))
            {
                return -100;
            }

            AddComparison(checker);
            if (checker is PlayerSortData data)
            {
                data.AddComparison(this);
            }

            return base.Compare(checker);
        }
    }
}
