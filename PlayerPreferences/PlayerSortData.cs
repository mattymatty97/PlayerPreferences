using System.Collections.Generic;
using System.Linq;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerSortData
    {
        private readonly PpPlugin plugin;
        private readonly Dictionary<PlayerSortData, Role> recentlyCompared;

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

        public PlayerSortData(Player player, Role role, PpPlugin plugin)
        {
            this.plugin = plugin;
            recentlyCompared = new Dictionary<PlayerSortData, Role>();

            Player = player;
            Record = plugin.Preferences.Contains(player.SteamId) ? plugin.Preferences[player.SteamId] : null;

            Role = role;
        }

        private void AddComparison(PlayerSortData data)
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

        private bool JustCompared(PlayerSortData data)
        {
            return recentlyCompared.ContainsKey(data) && recentlyCompared[data] == data.Role;
        }

        public bool Compare(PlayerSortData checker)
        {
            plugin.Debug($"Comparing {Player.Name} ({Role}) with {checker.Player.Name} ({checker.Role})");

            if (JustCompared(checker))
            {
                return false;
            }

            AddComparison(checker);
            checker.AddComparison(this);

            if (checker.Role == Role) // If this player was just swapped with checker and the checker has the same role we lost from the swap
            {
                return false;
            }

            int? newThisRank = Record?[checker.Role];
            int? newOtherRank = checker.Record?[Role];

            if (newThisRank == null)
            {
                if (newOtherRank == null)
                {
                    return false;
                }

                return plugin.DistributeAll && newOtherRank < checker.Rank;
            }

            if (newOtherRank == null)
            {
                return plugin.DistributeAll && newThisRank < Rank;
            }
            
            plugin.Debug(string.Join(", ", Record.Preferences.Select(x => x.ToString())));
            plugin.Debug(string.Join(", ", checker.Record.Preferences.Select(x => x.ToString())));

            float thisDelta = Rank - newThisRank.Value + (Record.AverageRank - newThisRank.Value) * plugin.RankWeightMultiplier;
            float otherDelta = checker.Rank - newOtherRank.Value + (checker.Record.AverageRank - newOtherRank.Value) * plugin.RankWeightMultiplier;
            float sumDelta = thisDelta + otherDelta;

            bool result = sumDelta > 0;
            plugin.Debug( " \n" +
                         $" -P1Rank: {Rank}\n" +
                         $" -P2Rank: {checker.Rank}\n" +
                         $" -P1OtherRank: {newThisRank}\n" +
                         $" -P2OtherRank: {newOtherRank}\n" +
                         $" -P1Avg: {Record.AverageRank}\n" +
                         $" -P2Avg: {checker.Record.AverageRank}\n" +
                         $" -P1Delta: {thisDelta}\n" +
                         $" -P2Delta: {otherDelta}\n" +
                         $" -SumDelta: {sumDelta}\n" +
                         $" -Should swap: {result}");
            
            // If it is a net gain of rankings or the other player is getting demoted but is equal to or above the other rank
            return result;
        }

        public void Swap(PlayerSortData other)
        {
            Role thisRole = Role;
            Role = other.Role;
            other.Role = thisRole;
        }
    }
}
