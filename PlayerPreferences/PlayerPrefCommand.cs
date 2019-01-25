using System.Linq;
using Smod2;
using Smod2.API;
using Smod2.Commands;

namespace PlayerPreferences
{
    public class PlayerPrefCommand : ICommandHandler
    {
        private readonly PpPlugin plugin;

        public PlayerPrefCommand(PpPlugin plugin)
        {
            this.plugin = plugin;
        }

        private static Player[] GetPlayers(string arg)
        {
            if (arg == "*")
            {
                return PluginManager.Manager.Server.GetPlayers().ToArray();
            }

            if (!int.TryParse(arg, out int playerId))
            {
                if (!long.TryParse(arg, out long steamId))
                {
                    return null;
                }

                string steamIdStr = steamId.ToString();
                return new[]
                {
                    PluginManager.Manager.Server.GetPlayers().FirstOrDefault(x => x.SteamId == steamIdStr)
                };
            }

            return new[]
            {
                PluginManager.Manager.Server.GetPlayers().FirstOrDefault(x => x.PlayerId == playerId)
            };
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if (!(sender is Server) && sender is Player player && !plugin.RaRanks.Contains(player.GetRankName()))
            {
                return new[]
                {
                    $"You (rank {player.GetRankName() ?? "NULL"}) do not have permissions to run this command."
                };
            }

            if (args.Length < 1)
            {
                return new[]
                {
                    "Invalid arguments length. Please specify at least 2 arguments."
                };
            }

            switch (args[0])
            {
				case "reload" when args.Length < 2:
					return new[]
					{
						"Invalid arguments length. Please specify at least 2 arguments."
					};

	            case "reload":
	            {
		            Player[] players = GetPlayers(args[1]);

		            if (players == null)
		            {
			            return new[]
			            {
				            "Invalid player selector. Please specify a player ID, a SteamID, or use a wildcard (*) for all players."
			            };
		            }

		            if (players.Length == 0)
		            {
			            return new[]
			            {
				            "No players found using the selector."
			            };
		            }

		            foreach (string steamId in players.Select(x => x.SteamId))
		            {
			            plugin.Preferences[steamId].Read();
		            }

		            return new[]
		            {
			            "Successfully reloaded Preferences."
		            };
				}

	            case "delete" when args.Length < 2:
		            return new[]
		            {
			            "Invalid arguments length. Please specify at least 2 arguments."
		            };

				case "delete":
	            {
		            Player[] players = GetPlayers(args[1]);

		            if (players == null)
		            {
			            return new[]
			            {
				            "Invalid player selector. Please specify a player ID, a SteamID, or use a wildcard (*) for all players."
			            };
		            }

		            if (players.Length == 0)
		            {
			            return new[]
			            {
				            "No players found using the selector."
			            };
		            }

		            foreach (string steamId in players.Select(x => x.SteamId))
		            {
			            plugin.Preferences.Remove(steamId);
		            }

		            return new[]
		            {
			            "Successfully deleted Preferences"
		            };
				}

				case "dump":
					return new[]
					{
						$"Data dumped to: \"{plugin.Preferences.Dump()}\""
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
            return "prefs <reload/default> <player ID, or * for all>";
        }

        public string GetCommandDescription()
        {
            return "Deals with reloading or removing player Preferences";
        }
    }
}
