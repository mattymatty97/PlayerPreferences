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
    public class EventHandlers : IEventHandlerCallCommand, IEventHandlerRoundStart, IEventHandlerTeamRespawn
    {
        private readonly Plugin plugin;

        public string[] CommandAliases { get; set; }

        public EventHandlers(Plugin plugin)
        {
            this.plugin = plugin;
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

        private class PlayerSortData
        {
            public Player Player { get; }
            private Role role;
            public Role Role
            {
                get => role;
                set
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
                return (newThisRank + newOtherRank) < (Rank + other.Rank);
            }

            public void Swap(PlayerSortData other)
            {
                Role thisRole = Role;
                Role = other.Role;
                other.Role = thisRole;
            }
        }

        private void AssignPlayers(IDictionary<Player, Role> playerRoles)
        {
            PlayerSortData[] players = playerRoles.Keys.Select(x => new PlayerSortData(x, playerRoles[x])).ToArray();
            PlayerSortData[] recordPlayers = players.Where(x => x.Record != null).ToArray();

            bool swapped;
            do
            {
                swapped = false;

                foreach (PlayerSortData player in recordPlayers)
                {
                    // If player is not already satisfied with their role.
                    if (player.Rank > 0)
                    {
                        // Find a player that is not of the same rank and willing to swap for role of current player
                        PlayerSortData match = players.FirstOrDefault(x => x.Role != player.Role && player.ShouldSwap(x));

                        // If the player exists, swap the current player and the match's roles
                        if (match != null)
                        {
                            player.Swap(match);

                            // Register the swap to see if the match would now like to swap with anyone else
                            swapped = true;
                        }
                    }
                }
            } while (swapped);

            plugin.Info($"Overall happiness rating: {(recordPlayers.Length > 0 ? recordPlayers.Average(x => 1 - x.Rank / 15).ToString() : "1 (no preferences set)")}");

            foreach (PlayerSortData player in players)
            {
                playerRoles[player.Player] = player.Role;
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.RefreshConfig();

            List<Player> players = ev.Server.GetPlayers().Where(x => x.SteamId != "0").ToList();
            Shuffle(players);

            Dictionary<Player, Role> playerRoles = players.ToDictionary(x => x, x => x.TeamRole.Role);
            AssignPlayers(playerRoles);

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

                if (!Plugin.preferences.Contains(ev.Player.SteamId))
                {
                    List<Role> myRoles = Plugin.Roles.Select(x => x.Value).ToList();
                    Shuffle(myRoles);

                    Plugin.preferences.Add(ev.Player.SteamId, myRoles.ToArray());
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
                                           "Player Preferences: Allows you to set your favorite roles so you have much higher chance of spawning as them." +
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
            return players.OrderBy(x =>
            {
                int index = Plugin.preferences[x.SteamId][role];

                return index == -1 ? Plugin.Roles.Count : index;
            });
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            List<Player> spectators = PluginManager.Manager.Server.GetPlayers().Where(x => x.TeamRole.Role == Role.SPECTATOR).ToList();
            Shuffle(spectators);
            
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

            AssignPlayers(players);

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
    }
}
