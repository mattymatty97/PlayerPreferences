using System.Linq;
using Smod2;
using Smod2.API;
using Smod2.Commands;

namespace PlayerPreferences
{
    public class PlayerPrefCommand : ICommandHandler
    {
        private static Player[] GetPlayers(string arg)
        {
            if (arg == "*")
            {
                return PluginManager.Manager.Server.GetPlayers().ToArray();
            }

            if (!int.TryParse(arg, out int playerId))
            {
                return null;
            }

            return PluginManager.Manager.Server.GetPlayers().Where(x => x.PlayerId == playerId).ToArray();
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if (!(sender is Server) && sender is Player player && !Plugin.ranks.Contains(player.GetRankName()))
            {
                return new[]
                {
                    $"You (rank {player.GetRankName() ?? "NULL"}) do not have permissions to run this command."
                };
            }

            if (args.Length < 2)
            {
                return new[]
                {
                    "Invalid arguments length. Please specify at least 2 arguments."
                };
            }

            Player[] players = GetPlayers(args[1]);

            if (players == null)
            {
                return new[]
                {
                    "Invalid player selector. Please specify a player ID or use a wildcard (*) for all players."
                };
            }

            if (players.Length == 0)
            {
                return new[]
                {
                    "No players found using the selector."
                };
            }

            switch (args[0])
            {
                case "reload":
                    foreach (string steamId in players.Select(x => x.SteamId))
                    {
                        Plugin.preferences[steamId].Read();
                    }

                    return new[]
                    {
                        "Successfully reloaded preferences."
                    };

                default:
                    return new[]
                    {
                        "Invalid action."
                    };
            }
        }

        public string GetUsage()
        {
            return "playerprefs <reload/default> <player ID, or * for all>";
        }

        public string GetCommandDescription()
        {
            return "Deals with reloading or removing player preferences";
        }
    }
}
