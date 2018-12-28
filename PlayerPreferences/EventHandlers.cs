using System.Collections.Generic;
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
    public class EventHandlers : IEventHandlerPlayerJoin, IEventHandlerCallCommand, IEventHandlerRoundStart, IEventHandlerTeamRespawn
    {
        private readonly Plugin plugin;

        public string[] CommandAliases { get; set; }

        public EventHandlers(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            if (!Plugin.preferences.Contains(ev.Player.SteamId))
            {
                Plugin.preferences.Add(ev.Player.SteamId, Plugin.Roles.Select(x => x.Value).ToArray());
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private static Dictionary<Player, Role> AssignPlayers(IReadOnlyList<Player> players, IDictionary<Role, int> maxRoles)
        {
            Dictionary<Player, Role> playerRoles = players.ToDictionary(x => x, x => Role.UNASSIGNED);

            foreach (Player player in players)
            {
                foreach (Role role in Plugin.preferences[player.SteamId].Preferences)
                {
                    if (maxRoles.ContainsKey(role) && maxRoles[role] > 0)
                    {
                        maxRoles[role]--;
                        playerRoles[player] = role;
                        break;
                    }
                }
            }

            return playerRoles;
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.RefreshConfig();

            List<Player> players = ev.Server.GetPlayers().Where(x => x.SteamId != "0").ToList();
            Shuffle(players);

            Dictionary<Player, Role> assignedPlayers = AssignPlayers(players, players.GroupBy(x => x.TeamRole.Role).ToDictionary(x => x.Key, x => x.Count()));

            foreach (KeyValuePair<Player, Role> playerRole in assignedPlayers)
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
                        if (!int.TryParse(args[0], out int rank) || rank > Plugin.Roles.Count)
                        {
                            ev.ReturnMessage = "\n" +
                                               "Invalid rank number.";
                            return;
                        }
                        // Correct index (since menu starts at 1)
                        rank = rank - 1;

                        PlayerRecord record = Plugin.preferences[ev.Player.SteamId];
                        
                        Role newRole = Plugin.GetRole(args[1]);

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
                                           "Updated role rank.";
                    }
                    else if (args[0] == "help")
                    {
                        ev.ReturnMessage = "\n" +
                                           "\"playerprefs\" - Gets all ranks with their corresponding roles\n" +
                                           "\"playerprefs help\" - Shows you this page you big dumb.\n" +
                                           $"\"playerprefs [rank] [role name]\", with rank as 1 (highest) to {Plugin.Roles.Count} - Sets the priority of making you that role.";
                    }
                }
                else
                {
                    int i = 1;
                    ev.ReturnMessage = "\n" +
                                       $"{string.Join("\n", Plugin.preferences[ev.Player.SteamId].Preferences.Select(x => $"{i++} - {Plugin.RoleNames[x]}"))}\n" +
                                       "Use \"help\" as an argument for additional command info.";
                }
            }
        }

        private static IEnumerable<Player> RankByRole(IEnumerable<Player> players, Role role)
        {
            return players.OrderByDescending(x =>
            {
                PlayerRecord record = Plugin.preferences[x.SteamId];

                int index = record.Preferences.Length;
                for (int i = 0; i < record.Preferences.Length; i++)
                {
                    if (record[i] == role)
                    {
                        index = i;
                        break;
                    }
                }

                return index;
            });
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            List<Player> spectators = PluginManager.Manager.Server.GetPlayers().Where(x => x.TeamRole.Role == Role.SPECTATOR).ToList();
            Shuffle(spectators);
            
            Dictionary<Role, int> roleCounts = ev.SpawnChaos ?
                new Dictionary<Role, int>
                {
                    {
                        Role.CHAOS_INSURGENCY,
                        ev.PlayerList.Count
                    }
                } :
                new Dictionary<Role, int>
                {
                    {
                        Role.NTF_COMMANDER,
                        1
                    },
                    {
                        Role.NTF_LIEUTENANT,
                        Mathf.Min(3, ev.PlayerList.Count - 1)
                    },
                    {
                        Role.NTF_CADET,
                        Mathf.Max(0, ev.PlayerList.Count - 4)
                    }
                };
            
            Dictionary<Player, Role> newPlayers = AssignPlayers(spectators, roleCounts);

            if (ev.SpawnChaos)
            {
                ev.PlayerList = newPlayers.Keys.ToList();
            }
            else
            {
                ev.PlayerList = newPlayers.Where(x => x.Value == Role.NTF_COMMANDER).Select(x => x.Key)
                    .Concat(newPlayers.Where(x => x.Value == Role.NTF_LIEUTENANT).Select(x => x.Key))
                    .Concat(newPlayers.Where(x => x.Value == Role.NTF_CADET).Select(x => x.Key)).ToList();
            }
        }
    }
}
