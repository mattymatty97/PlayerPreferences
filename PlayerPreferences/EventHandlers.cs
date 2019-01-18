using System;
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
            AssignRoles(playerRoles);
            
            foreach (KeyValuePair<Player, Role> playerRole in playerRoles)
            {
                playerRole.Key.ChangeRole(playerRole.Value);
            }
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
                                               ".prefs average     - Displays your average rank.";
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
                            
                        case "average" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "You have no role preferences. Run \".prefs create\" to regenerate your preferences and use role preferences again.";
                            return;
                            
                        case "average":
                            ev.ReturnMessage = "\n" +
                                               $"Your average rank is {Mathf.Round(plugin.Preferences[ev.Player.SteamId].AverageRank * 100) / 100}.";
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

            List<KeyValuePair<int, PlayerData>> spectatorList = plugin.Server.GetPlayers()
                .Where(x => x.TeamRole.Role == Role.SPECTATOR)
                .Select(x => new KeyValuePair<int, PlayerData>(x.PlayerId, new PlayerData(x, Role.SPECTATOR, plugin)))
                .ToList();
            PpPlugin.Shuffle(spectatorList);
            Dictionary<int, PlayerData> spectators = spectatorList.ToDictionary(x => x.Key, x => x.Value);

            plugin.Info("Calculating optimal team respawn roles...");
            if (ev.SpawnChaos)
            {
                ev.PlayerList = RankedPlayers(spectators.Values, ev.PlayerList, Role.CHAOS_INSURGENCY).Take(ev.PlayerList.Count).ToList();
            }
            else
            {
                List<Player> mtf = new List<Player>();

                Player commander = RankedPlayers(spectators.Values, ev.PlayerList.Take(1).ToList(), Role.NTF_COMMANDER).First();
                mtf.Add(commander);

                spectators.Remove(commander.PlayerId);

                int remainingMtf = ev.PlayerList.Count - 1;
                if (remainingMtf > 0)
                {
                    int lieutenantCount = Mathf.Min(remainingMtf, 3);

                    Player[] lieutenants = RankedPlayers(spectators.Values, ev.PlayerList.Skip(1).Take(3).ToList(), Role.NTF_LIEUTENANT).Take(lieutenantCount).ToArray();
                    mtf.AddRange(lieutenants);
                    
                    foreach (Player lieutenant in lieutenants)
                    {
                        spectators.Remove(lieutenant.PlayerId);
                    }

                    if ((remainingMtf -= lieutenantCount) > 0)
                    {
                        mtf.AddRange(RankedPlayers(spectators.Values, ev.PlayerList.Skip(4).ToList(), Role.NTF_CADET).Take(remainingMtf));
                    }
                }

                ev.PlayerList = mtf;
            }
            plugin.Info("Player roles set!");
        }

        public void OnSetConfig(SetConfigEvent ev)
        {
            if (ev.Key == "smart_class_picker" && plugin.DisableSmartClassPicker)
            {
                ev.Value = false;
            }
        }

        private void AssignRoles(IDictionary<Player, Role> playerRoles)
        {
            PlayerData[] players = playerRoles.Select(x => (PlayerData) new PlayerSortData(x.Key, x.Value, plugin)).ToArray();

            int comparisons = 0;

            for (int i = 0; i < players.Length; i++)
            {
                if (comparisons + players.Length > plugin.MaxRoundStartComparisons)
                {
                    plugin.Error($"Maximum comparison limit exceeded ({comparisons} + {players.Length} to be performed with limit of {plugin.MaxRoundStartComparisons}). Sorting stopped to prevent halting the server further.");
                    break;
                }

                PlayerData player = players[i];
                plugin.Debug($"Checking {player.Player.Name}");

                // Find a player that is willing to swap for role of current player
                PlayerData match = players.FirstOrDefault(x =>
                {
                    comparisons++;
                    return player.Compare(x);
                });

                // If the player exists, swap the current player and the match's roles
                if (match != null)
                {
                    plugin.Debug($"Found match for {player.Player.Name}: {match.Player.Name}");
                    player.Swap(match);

                    // Go back to start to check if anyone wants to swap because of the newrole
                    i = -1;
                }
            }

            foreach (PlayerData player in players)
            {
                playerRoles[player.Player] = player.Role;
                player.Record?.UpdateAverage(player.Rank);
            }

            plugin.Info($"Roles set after {comparisons} comparisons.");
        }

        private IEnumerable<Player> RankedPlayers(IEnumerable<PlayerData> rankablePlayers, IReadOnlyCollection<Player> defaultPlayers, Role role)
        {
            Player[] ranked = rankablePlayers
                .ToDictionary(x => x, x => x.RoleRating(role))
                .Where(x => x.Value.HasValue)
                .OrderByDescending(x => x.Value.Value)
                .Take(defaultPlayers.Count)
                .Select(x => x.Key.Player)
                .ToArray();
            int rankedIndex = 0;
            
            foreach (Player player in defaultPlayers)
            {
                if (!plugin.Preferences.Contains(player.SteamId) && !plugin.DistributeAll)
                {
                    yield return player;
                }
                else if (rankedIndex < ranked.Length)
                {
                    yield return ranked[rankedIndex++];
                }
            }
        }
    }
}
