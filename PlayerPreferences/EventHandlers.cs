using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.RefreshConfig();

            List<Player> players = ev.Server.GetPlayers().Where(x => x.SteamId != "0").ToList();
            Shuffle(players);

            Dictionary<Role, int> roleCounts = players.GroupBy(x => x.TeamRole.Role).ToDictionary(x => x.Key, x => x.Count());

            foreach (Player player in players)
            {
                foreach (Role role in Plugin.preferences[player.SteamId].Preferences)
                {
                    if (roleCounts.ContainsKey(role) && roleCounts[role] > 0)
                    {
                        roleCounts[role]--;
                        player.ChangeRole(role);
                        break;
                    }
                }
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
                    if (!int.TryParse(args[0], out int rank))
                    {
                        if (args[0] == "help")
                        {
                            ev.ReturnMessage = "\n" +
                                               "\"playerprefs\" - Gets all ranks with their corresponding roles\n" +
                                               "\"playerprefs help\" - Shows you this page you big dumb.\n" +
                                               $"\"playerprefs [role name] [rank]\", with rank as 1 (highest) to {Plugin.Roles.Count} - Sets the priority of making you that role.";
                        }
                        if (rank > Plugin.Roles.Count)
                        {
                            ev.ReturnMessage = "\n" +
                                               "Invalid rank number.";
                        }
                    }
                    // Correct index (since menu starts at 1)
                    rank = rank - 1;

                    PlayerRecord record = Plugin.preferences[ev.Player.SteamId];

                    if (args.Length > 1)
                    {
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
            IEnumerable<Player> spectators = PluginManager.Manager.Server.GetPlayers().Where(x => x.TeamRole.Role == Role.SPECTATOR);

            int count = ev.PlayerList.Count;
            if (ev.SpawnChaos)
            {
                ev.PlayerList = RankByRole(spectators, Role.CHAOS_INSURGENCY).Take(count).ToList();
            }
            else
            {
                Player[] spectatorArray = spectators.ToArray();

                List<Player> commander = new List<Player>
                {
                    RankByRole(spectatorArray, Role.NTF_COMMANDER).First()
                };
                if (count > 1)
                {
                    IEnumerable<Player> otherSpawns = RankByRole(spectatorArray.Skip(1), Role.NTF_LIEUTENANT).Take(Mathf.Min(count - 1, 3));

                    if (count > 4)
                    {
                        otherSpawns = otherSpawns.Concat(RankByRole(spectatorArray, Role.NTF_CADET)
                            .Take(Mathf.Min(count - 4, 5)));
                    }

                    ev.PlayerList = commander.Concat(otherSpawns).ToList();
                }
            }
        }
    }
}
