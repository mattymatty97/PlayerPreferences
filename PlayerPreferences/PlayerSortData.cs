using Smod2.API;
using UnityEngine;

namespace PlayerPreferences
{
    public class PlayerSortData
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

        public PlayerSortData(Player player, Role role, PpPlugin plugin)
        {
            this.plugin = plugin;
            Player = player;
            Record = plugin.Preferences.Contains(player.SteamId) ? plugin.Preferences[player.SteamId] : null;

            Role = role;
        }

        public bool ShouldSwap(PlayerSortData other)
        {
            plugin.Debug($"Comparing {Player.Name} with {other.Player.Name}:");

            int newThisRank = Record[other.Role];
            bool result;

            if (other.Record == null)
            {
                result = plugin.DistributeAll && newThisRank < Rank;

                plugin.Debug( " -P2Preferences: null\n" +
                             $" -DistributeAll: {plugin.DistributeAll}\n" +
                             $" -P1Rank: {newThisRank}\n" +
                             $" -P2Rank: {Rank}\n" +
                             $" -Should swap: {result}");
                return result;
            }

            int newOtherRank = other.Record[Role];
            
            float thisDelta = Rank - newThisRank + Record.AverageRank * plugin.RankWeightMultiplier;
            float otherDelta = other.Rank - newOtherRank + other.Record.AverageRank * plugin.RankWeightMultiplier;
            float sumDelta = thisDelta + otherDelta;

            result = sumDelta > 0;
            plugin.Debug(" -P2Preferences: exists\n" +
                         $" -P1Avg: {Record.AverageRank}\n" +
                         $" -P2Avg: {other.Record.AverageRank}\n" +
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
