using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Smod2.EventHandlers;
using Smod2.Events;
using System.Text.RegularExpressions;
using Smod2;
using Smod2.API;
using Smod2.EventSystem.Events;
using UnityEngine;

namespace PlayerPreferences
{
    public class EventHandlers : IEventHandlerPlayerJoin, IEventHandlerCallCommand, IEventHandlerSetConfig, IEventHandlerWaitingForPlayers, IEventHandlerRoundStart, IEventHandlerTeamRespawn
    {
        private readonly PpPlugin plugin;

        public string[] CommandAliases { get; set; }

        public EventHandlers(PpPlugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            if (ev.Player.DoNotTrack && plugin.Preferences.Contains(ev.Player.SteamId))
            {
                plugin.Preferences.Remove(ev.Player.SteamId);
            }
        }

        private void AssignPlayers(IDictionary<Player, Role> playerRoles)
        {
            PlayerSortData[] players = playerRoles.Select(x => new PlayerSortData(x.Key, x.Value, plugin)).ToArray();
            
            for (int i = 0; i < players.Length; i++)
            {
                PlayerSortData player = players[i];
                plugin.Debug($"Checking {player.Player.Name}");

                // Find a player that is willing to swap for role of current player
                PlayerSortData match = players.FirstOrDefault(x => player.Compare(x));

                // If the player exists, swap the current player and the match's roles
                if (match != null)
                {
                    plugin.Debug($"Found match for {player.Player.Name}: {match.Player.Name}");
                    player.Swap(match);

                    // Go back to start to check if anyone wants to swap because of the newrole
                    i = -1;
                }
            }

            foreach (PlayerSortData player in players)
            {
                playerRoles[player.Player] = player.Role;
                player.Record?.UpdateAverage(player.Rank);
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            plugin.RefreshConfig();
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            Player[] players = ev.Server.GetPlayers().Where(x => x.SteamId != "0").ToArray();
            PpPlugin.Shuffle(players);
            
            Dictionary<Player, Role> playerRoles = players.ToDictionary(x => x, x => x.TeamRole.Role);

            plugin.Info("Calculating optimal starting player roles...");
            AssignPlayers(playerRoles);
            foreach (KeyValuePair<Player, Role> playerRole in playerRoles)
            {
                playerRole.Key.ChangeRole(playerRole.Value);
            }
            plugin.Info("Player roles set!");
        }

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            string command = ev.Command.ToLower();

            if (CommandAliases?.Any(x => command.StartsWith(x)) ?? false)
            {
                MatchCollection collection = new Regex("[^\\s\"\']+|\"([^\"]*)\"|\'([^\']*)\'").Matches(command);
                string[] args = new string[collection.Count - 1];

                for (int i = 1; i < collection.Count; i++)
                {
                    // If it's wrapped in quotes, 
                    if (collection[i].Value[0] == '\"' && collection[i].Value[collection[i].Value.Length - 1] == '\"')
                    {
                        args[i - 1] = collection[i].Value.Substring(1, collection[i].Value.Length - 2);
                    }
                    else
                    {
                        args[i - 1] = collection[i].Value;
                    }
                }

                if (args.Length > 0)
                {
                    if (args.Length > 1)
                    {
                        switch (args[0])
                        {
                            case "hash" when args[1].Length != PpPlugin.Roles.Count:
                                ev.ReturnMessage = "\n" +
                                                   "Invalid hash length. Are you sure thats the full hash? Is the hash from an outdated server?";
                                return;

                            case "hash":
                                if (ev.Player.DoNotTrack)
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Looks like you've got \"do not track\" enabled. If you want to use role preferences, please disable do not track.";
                                    return;
                                }

                                char[] cRoles = args[1].ToCharArray();
                                bool invalid = false;

                                Role[] roles = cRoles.Select(x =>
                                {
                                    if (!int.TryParse(x.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int roleInt))
                                    {
                                        invalid = true;
                                        return Role.UNASSIGNED;
                                    }
                                    
                                    if (!PpPlugin.IntToRole.ContainsKey(roleInt))
                                    {
                                        invalid = true;
                                        return Role.UNASSIGNED;
                                    }

                                    return PpPlugin.IntToRole[roleInt];
                                }).ToArray();

                                if (invalid || roles.Length != roles.Distinct().Count())
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Invalid hash. Is the hash from an outdated server?";
                                    return;
                                }

                                if (plugin.Preferences.Contains(ev.Player.SteamId))
                                {
                                    plugin.Preferences[ev.Player.SteamId].Preferences = roles;
                                    ev.ReturnMessage = "\n" +
                                                       "Preferences set.";
                                }
                                else
                                {
                                    plugin.Preferences.Add(ev.Player.SteamId, roles);
                                    ev.ReturnMessage = "\n" +
                                                       "Preferences created and set.";
                                }
                                return;

                            default:
                                if (!plugin.Preferences.Contains(ev.Player.SteamId))
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "You have no role preferences. Run \".prefs create\" to regenerate your preferences and use role preferences again.";
                                    return;
                                }

                                if (!int.TryParse(args[0], out int rank) || rank > PpPlugin.Roles.Count)
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Invalid rank number.";
                                    return;
                                }
                                // Correct index (since menu starts at 1)
                                rank = rank - 1;

                                PlayerRecord record = plugin.Preferences[ev.Player.SteamId];

                                Role newRole = PpPlugin.GetRole(args[1]);

                                int curIndex = -1;
                                for (int i = 0; i < record.Preferences.Length; i++)
                                {
                                    if (record.Preferences[i] == newRole)
                                    {
                                        curIndex = i;
                                        break;
                                    }
                                }

                                Role existingRole = record[rank];

                                record[rank] = newRole;
                                record[curIndex] = existingRole;

                                ev.ReturnMessage = "\n" +
                                                   $"Updated role rank of {PpPlugin.RoleNames[newRole]} to {rank + 1} and moved {PpPlugin.RoleNames[existingRole]} to {curIndex + 1}.";
                                return;
                        }
                    }

                    switch (args[0])
                    {
                        case "help":
                            ev.ReturnMessage = "\n" +
                                               " // Player Preferences by Androx //\n" +
                                               "\n" +
                                               ".prefs             - Gets all ranks with their corresponding roles.\n" +
                                               ".prefs help        - Shows you this page you big dumb.\n" +
                                               ".prefs create      - Generates preferences and unlocks commands.\n" +
                                               ".prefs delete      - Deletes preference data with your account.\n" +
                                               ".prefs [#] [role]  - Sets role respawn priority (1 is the highest).\n" +
                                               ".prefs hash        - Gives hash of current preferences.\n" +
                                               ".prefs hash [hash] - Sets preferences to the specified hash.\n" +
                                               ".pres weight       - Displays your current preference weight.";
                            return;

                        case "create" when !plugin.Preferences.Contains(ev.Player.SteamId): {
                            if (ev.Player.DoNotTrack)
                            {
                                ev.ReturnMessage = "\n" +
                                                   "Looks like you've got \"do not track\" enabled. If you want to use role preferences, please disable do not track.";
                                return;
                            }

                            Role[] myRoles = PpPlugin.Roles.Select(x => x.Value).ToArray();
                            PpPlugin.Shuffle(myRoles);

                            plugin.Preferences.Add(ev.Player.SteamId, myRoles);

                            ev.ReturnMessage = "\n" +
                                               "Created random role preferences. Use \".prefs delete\" to delete them.";
                            return;
                        }

                        case "create":
                            ev.ReturnMessage = "\n" +
                                               "You already have role preferences. Use \".prefs delete\" to delete them.";
                            return;

                        case "delete" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "You have no role preferences. Run \".prefs create\" to regenerate your preferences and use role preferences again.";
                            return;

                        case "delete":
                            plugin.Preferences.Remove(ev.Player.SteamId);

                            ev.ReturnMessage = "\n" +
                                               "Deleted role preferences. Run \".prefs create\" command to regenerate your preferences and use role preferences again.";
                            return;

                        case "hash" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "You have no role preferences. If you mean to set your preferences with a hash, please use \".prefs hash [generated hash]\" instead.";
                            return;

                        case "hash":
                            ev.ReturnMessage = "\n" +
                                               $"Your role preferences hash: {string.Join("", plugin.Preferences[ev.Player.SteamId].Preferences.Select(x => PpPlugin.RoleToInt[x].ToString("X"))).ToLower()}";
                            return;

                        case "weight" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "You have no role preferences. Run \".prefs create\" to regenerate your preferences and use role preferences again.";
                            return;

                        case "weight":
                            ev.ReturnMessage = "\n" +
                                               $"Your average rank weight is {Mathf.Round(plugin.Preferences[ev.Player.SteamId].AverageRank * 100) / 100}.";
                            return;

                        default:
                            ev.ReturnMessage = "\n" +
                                               "Invalid argument. Please run \".prefs help\" for a full list of commands.";
                            return;
                    }
                }

                if (plugin.Preferences.Contains(ev.Player.SteamId))
                {
                    int i = 1;
                    ev.ReturnMessage = "\n" +
                                       $"{string.Join("\n", plugin.Preferences[ev.Player.SteamId].Preferences.Select(x => $"{i++} - {PpPlugin.RoleNames[x]}"))}\n" +
                                       "Use \".prefs help\" for additional command info.";
                    return;
                }

                ev.ReturnMessage = "\n" +
                                   "You have not created your preferences yet. To do so, use \".prefs create\"";
            }
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            if (ev.PlayerList.Count == 0)
            {
                return;
            }

            Player[] spectators = PluginManager.Manager.Server.GetPlayers().Where(x => x.TeamRole.Role == Role.SPECTATOR).ToArray();
            PpPlugin.Shuffle(spectators);
            
            Dictionary<Player, Role> players = ev.SpawnChaos ?
                ev.PlayerList
                    .Select(x => new KeyValuePair<Player, Role>(x, Role.CHAOS_INSURGENCY))
                    .Concat(spectators.Except(ev.PlayerList).Select(x => new KeyValuePair<Player, Role>(x, Role.SPECTATOR)))
                    .ToDictionary(x => x.Key, x => x.Value) : 
                new Dictionary<Player, Role>
                {
                    {
                        ev.PlayerList[0],
                        Role.NTF_COMMANDER
                    }
                }
                    .Concat(ev.PlayerList.Skip(1).Take(3).Select(x => new KeyValuePair<Player, Role>(x, Role.NTF_LIEUTENANT)))
                    .Concat(ev.PlayerList.Skip(4).Select(x => new KeyValuePair<Player, Role>(x, Role.NTF_CADET)))
                    .Concat(spectators.Except(ev.PlayerList).Select(x => new KeyValuePair<Player, Role>(x, Role.SPECTATOR)))
                    .ToDictionary(x => x.Key, x => x.Value);

            plugin.Info("Calculating optimal team respawn roles...");
            AssignPlayers(players);
            plugin.Info("Player roles set!");

            if (ev.SpawnChaos)
            {
                ev.PlayerList = players.Where(x => x.Value == Role.CHAOS_INSURGENCY).Select(x => x.Key).ToList();
            }
            else
            {
                ev.PlayerList = players.Where(x => x.Value == Role.NTF_COMMANDER).Take(1).Select(x => x.Key)
                    .Concat(players.Where(x => x.Value == Role.NTF_LIEUTENANT).Take(3).Select(x => x.Key))
                    .Concat(players.Where(x => x.Value == Role.NTF_CADET).Select(x => x.Key)).ToList();
            }
        }

        public void OnSetConfig(SetConfigEvent ev)
        {
            if (ev.Key == "smart_class_picker")
            {
                ev.Value = plugin.UseSmartClassPicker;
            }
        }
    }
}
