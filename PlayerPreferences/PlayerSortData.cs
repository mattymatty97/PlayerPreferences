using Smod2;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerSortData
    {
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

        public PlayerSortData(Player player, Role role)
        {
            Player = player;
            Record = Plugin.preferences.Contains(player.SteamId) ? Plugin.preferences[player.SteamId] : null;

            Role = role;
        }

        public bool ShouldSwap(PlayerSortData other)
        {
            int newThisRank = Record[other.Role];

            if (other.Record == null)
            {
                return newThisRank < Rank;
            }

            int newOtherRank = other.Record[Role];

            int thisDelta = Rank - newThisRank;
            int otherDelta = other.Rank - newOtherRank;
            int sumDelta = thisDelta + otherDelta;
            
            // If it is a net gain of rankings or the other player is getting demoted but is equal to or above the other rank
            return sumDelta > 0;
        }

        public void Swap(PlayerSortData other)
        {
            Role thisRole = Role;
            Role = other.Role;
            other.Role = thisRole;
        }
    }
}
