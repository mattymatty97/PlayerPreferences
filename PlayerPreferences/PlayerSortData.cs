using Smod2.API;

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
            int newThisRank = Record[other.Role];

            if (other.Record == null)
            {
                return plugin.DistributeAll && newThisRank < Rank;
            }

            int newOtherRank = other.Record[Role];

            float lowestAverageRank = Record.AverageRank > other.Record.AverageRank
                ? other.Record.AverageRank
                : Record.AverageRank;

            float thisDelta = Rank - newThisRank + 
                              Record.AverageRank - lowestAverageRank;
            float otherDelta = other.Rank - newOtherRank + 
                               other.Record.AverageRank - lowestAverageRank;

            float sumDelta = thisDelta + otherDelta;
            
            // If it is a net gain of rankings or the other player is getting demoted but is equal to or above the other rank
            return sumDelta > 0;
        }

        public void Swap(PlayerSortData other)
        {
            Role thisRole = Role;
            Role = other.Role;
            other.Role = thisRole;

            Record.UpdateAverage(Rank);
            other.Record.UpdateAverage(other.Rank);
        }
    }
}
