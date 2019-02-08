using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            AssignRoles(ref playerRoles);
            
            foreach (KeyValuePair<Player, Role> playerRole in playerRoles)
            {
                
                plugin.Debug($"{playerRole.Key.Name} is {playerRole.Key.TeamRole.Role} and should be {playerRole.Value}");
                if (playerRole.Key.TeamRole.Role != playerRole.Value && playerRole.Value != Role.UNASSIGNED)
                {
                    playerRole.Key.ChangeRole(playerRole.Value);
                    plugin.Debug($"Setting {playerRole.Key.Name} to {playerRole.Value}");
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

            Dictionary<int,PlayerData> spectators = plugin.Server.GetPlayers()
                .Where(x => x.TeamRole.Role == Role.SPECTATOR)
                .Select(x => new PlayerData(x, Role.SPECTATOR, plugin))
                .ToDictionary(x=>x.Player.PlayerId,x=>x);
            Dictionary<int,Player> defaultUnrankedPlayers = ev.PlayerList
                .Select(x => new PlayerData(x, Role.SPECTATOR, plugin))
                .Where( x=> x.Record == null)
                .Select(x=>x.Player)
                .ToDictionary(x=>x.PlayerId,x=>x);

            plugin.Info("Calculating optimal team respawn roles...");
            if (ev.SpawnChaos)
            {
                ev.PlayerList = RankedPlayers(spectators.Values, defaultUnrankedPlayers.Values, ev.PlayerList, Role.CHAOS_INSURGENCY,ev.PlayerList.Count).Take(ev.PlayerList.Count).ToList();
            }
            else
            {
                List<Player> mtf = new List<Player>();

                Player commander = RankedPlayers(spectators.Values,defaultUnrankedPlayers.Values, ev.PlayerList.Take(1).ToList(), Role.NTF_COMMANDER,1).First();
                mtf.Add(commander);

                spectators.Remove(commander.PlayerId);
                defaultUnrankedPlayers.Remove(commander.PlayerId);

                int remainingMtf = ev.PlayerList.Count - 1;
                if (remainingMtf > 0)
                {
                    int lieutenantCount = Mathf.Min(remainingMtf, 3);

                    Player[] lieutenants = RankedPlayers(spectators.Values,defaultUnrankedPlayers.Values, ev.PlayerList.Skip(1).Take(3).ToList(), Role.NTF_LIEUTENANT,lieutenantCount).Take(lieutenantCount).ToArray();
                    mtf.AddRange(lieutenants);
                    
                    foreach (Player lieutenant in lieutenants)
                    {
                        spectators.Remove(lieutenant.PlayerId);
                        defaultUnrankedPlayers.Remove(lieutenant.PlayerId);
                    }

                    if ((remainingMtf -= lieutenantCount) > 0)
                    {
                        mtf.AddRange(RankedPlayers(spectators.Values,defaultUnrankedPlayers.Values, ev.PlayerList.Skip(4).ToList(), Role.NTF_CADET,remainingMtf).Take(remainingMtf));
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

        private void AssignRoles( ref Dictionary<Player, Role> playerRoles)
        {
            
            Dictionary<Role,int> RoleCounter = new Dictionary<Role, int>();
           
            PlayerData[] players = playerRoles.Select(x => new PlayerData(x.Key,x.Value,plugin)).OrderBy(x=>x.Record==null).ToArray();
            //order unranked to the bottom

            foreach (var player in players)
            {
                if (plugin.DistributeAll || player.Record!=null)
                {
                    if (RoleCounter.ContainsKey(player.Role))
                        RoleCounter[player.Role]++;
                    else
                        RoleCounter.Add(player.Role, 1);
                }
            }
            
            _RData data = new _RData(players,RoleCounter);

            _Rassign(ref data, 0);

            for (int i = 0; i < players.Length; i++)
            {
                playerRoles[players[i].Player] = data.bestAssign[i];
            }
            
            plugin.Info($"Roles set after {data.recursions} comparisons.");
        }

        private IEnumerable<Player> RankedPlayers(IEnumerable<PlayerData> rankablePlayers,IEnumerable<Player> defaultUnrankedPlayers, IReadOnlyCollection<Player> defaultPlayers, Role role, int size)
        {
            Player[] ranked = rankablePlayers
                .ToDictionary(x => x, x => x.Record?.RoleRating(role))
                .Where(x => x.Value.HasValue)
                .OrderByDescending(x => x.Value.Value)
                .Take(defaultPlayers.Count)
                .Select(x => x.Key.Player)
                .ToArray();
            int rankedIndex = 0;
            int unrankedIndex = 0;

            int count = 0;
            
            foreach (Player player in defaultPlayers)
            {
                if(count>size)
                    break;
                count++;
                if (!plugin.Preferences.Contains(player.SteamId) && !plugin.DistributeAll)
                {
                    yield return player;
                }
                else if (rankedIndex < ranked.Length)
                {
                    yield return ranked[rankedIndex++];
                }
                else
                {
                    yield return defaultUnrankedPlayers.ToArray()[unrankedIndex++];
                }
            }
        }
        
        private class _RData
        {
            public int recursions;
            public PlayerData[] players;
            public int size;
            public Dictionary<Role, int> roleCounter;
            public Role[] bestAssign;
            public float bestSum;
            public Role[] actAssign;
            public float actSum;
            public bool result;
            
            public _RData(PlayerData[] players, Dictionary<Role, int> roleCounter)
            {
                this.players = players;
                this.roleCounter = roleCounter;
                bestAssign = Enumerable.Repeat(Role.UNASSIGNED,players.Length).ToArray();
                actAssign = Enumerable.Repeat(Role.UNASSIGNED,players.Length).ToArray();
                bestSum = -1;
                actSum = 0;
                result = false;
                size = players.Length;
            }
        }

        private bool _Rassign(ref _RData data, int deept)
        {
            if (deept == data.size)
            {
                plugin.Debug($"sum={data.actSum}");
                if (data.actSum > data.bestSum)
                {
                    Array.Copy(data.actAssign,data.bestAssign,data.size);
                    data.result = true;
                }
                return false;
            }

            //check max comparsions || at least one result required
            if (data.recursions > plugin.MaxRoundStartComparisons && data.result)
                return true;

            data.recursions++;

            PlayerData curr = data.players[deept];

            Role[] array;
            if (curr.Record != null) // if there are preferences loop starting from the highest ranked
                array = curr.Record.Preferences;
            else
                array = data.roleCounter.Keys.ToArray();
                 

            if (curr.Record != null || plugin.DistributeAll)
            {
                    foreach (var role in array)
                    {
                        if (data.roleCounter.ContainsKey(role) && data.roleCounter[role] > 0)
                        {
                            float val = data.players[deept].Record?.RoleRating(data.actAssign[deept]) ?? 0;
                            
                            plugin.Debug($"{curr.Player.Name} - {role} - {val}");
                            data.actAssign[deept] = role;
                            data.roleCounter[role]--;
                            data.actSum += val;
                            if (_Rassign(ref data, deept + 1)) //if true break directly
                                return true;
                            data.roleCounter[role]++;
                            data.actAssign[deept] = Role.UNASSIGNED;
                            data.actSum -= val;
                        }
                    }
            }
            else
            {
                data.actAssign[deept] = curr.Role;
                if (_Rassign(ref data, deept + 1))
                    return true;
            }

            return false;
        }
    }
}
