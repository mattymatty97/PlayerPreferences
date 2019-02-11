using System;
using System.Collections;
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
            AssignRoles(playerRoles,Role.CLASSD);
            int changes = 0;
            foreach (KeyValuePair<Player, Role> playerRole in playerRoles)
            {
                
                plugin.Debug($"{playerRole.Key.Name} is {playerRole.Key.TeamRole.Role} and should be {playerRole.Value}");
                if (playerRole.Key.TeamRole.Role != playerRole.Value && playerRole.Value != Role.UNASSIGNED)
                {
                    changes++;
                    playerRole.Key.ChangeRole(playerRole.Value);
                    plugin.Debug($"Setting {playerRole.Key.Name} to {playerRole.Value}");
                }
            }
            plugin.Info($"Changed {changes} roles");
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
                                                   "Lunghezza invalida dell'hash. Sei sicuro che sia l'intero Hash? E' un Hash proveniente da un server obsoleto";
                                return;

                            case "hash":
                                if (ev.Player.DoNotTrack)
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Sembra che tu abbia \"do not track\" abilitato. Se si vuole utilizzare le preferenze di ruolo, assicurati di disabilitare il do not track.";
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
                                                       "Hash invalido. E' un Hash proveniente da un server obsoleto?";
                                    return;
                                }

                                if (plugin.Preferences.Contains(ev.Player.SteamId))
                                {
                                    plugin.Preferences[ev.Player.SteamId].Preferences = roles;
                                    ev.ReturnMessage = "\n" +
                                                       "Le preferenze sono state settate.";
                                }
                                else
                                {
                                    plugin.Preferences.Add(ev.Player.SteamId, roles);
                                    ev.ReturnMessage = "\n" +
                                                       "Preferenze create e settate.";
                                }
                                return;

                            default:
                                if (!plugin.Preferences.Contains(ev.Player.SteamId))
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Non hai nessuna preferenza di ruolo. Avvia \".prefs create\" per rigenerare le tue preferenze e utilizzare nuovamente le preferenze di ruolo.";
                                    return;
                                }

                                if (!int.TryParse(args[0], out int rank) || rank > PpPlugin.Roles.Count)
                                {
                                    ev.ReturnMessage = "\n" +
                                                       "Numero della posizione non valido.";
                                    return;
                                }
                                // Correct index (since menu starts at 1)
                                rank = rank - 1;

                                PlayerRecord record = plugin.Preferences[ev.Player.SteamId];

                                Role newRole = PpPlugin.GetRole(args[1]);

                                int oldPos = 0;
                                for(int i=0; i<record.Preferences.Length;i++)
                                    if (record[i] == newRole)
                                        oldPos = i;

                                Role existingRole = record[rank];

                                record[rank] = newRole;
                                record[oldPos] = existingRole;

                                ev.ReturnMessage = "\n" +
                                                   $"Posizione del ruolo aggiornata da {PpPlugin.RoleNames[newRole]} a {rank + 1} e spostato {PpPlugin.RoleNames[existingRole]} a {oldPos + 1}.";
                                return;
                        }
                    }

                    switch (args[0])
                    {
                        case "help":
                            ev.ReturnMessage = "\n" +" // Player Preferences by Androx //\n" +
                                               "\n" +
                                               ".prefs             - Ottieni tutte le posizioni con i loro ruoli corrispondenti.\n" +
                                               ".prefs help        - Ti mostra questa pagina idiota.\n" +
                                               ".prefs create      - Genera preferenze casuali e sblocca i comandi.\n" +
                                               ".prefs delete      - Elimina i dati delle preferenze dal tuo account.\n" +
                                               ".prefs [#] [ruolo] - Imposta la posizione del ruolo (1 è il più alto).\n" +
                                               ".prefs hash        - Fornisce hash delle preferenze correnti.\n" +
                                               ".prefs hash [hash] - Imposta le preferenze sull'hash specificato." + 
                                               ".prefs average     - Mostra la tua media di soddisfazione";
                            return;

                        case "create" when !plugin.Preferences.Contains(ev.Player.SteamId): {
                            if (ev.Player.DoNotTrack)
                            {
                                ev.ReturnMessage = "\n" +
                                                   "Sembra che tu abbia \"do not track\" abilitato. Se vuoi utilizzare le preferenze di ruolo, disabilita do not track.";
                                return;
                            }

                            Role[] myRoles = PpPlugin.Roles.Select(x => x.Value).ToArray();
                            PpPlugin.Shuffle(myRoles);

                            plugin.Preferences.Add(ev.Player.SteamId, myRoles);

                            ev.ReturnMessage = "\n" +
                                               "Preferenze casuali create. Utilizzare \".Prefs delete\" per eliminarle o \".prefs [#] [role]\" per riordinarle";
                            break;
                        }

                        case "create":
                            ev.ReturnMessage = "\n" +
                                               "Hai già preferenze di ruolo. Usa \".prefs delete\" per eliminarle  o \".prefs [#] [role]\" per riordinarle";
                            break;

                        case "delete" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "Non hai nessuna preferenza di ruolo. Esegui \".prefs create\" per rigenerare le tue preferenze e utilizzare nuovamente le preferenze di ruolo.";
                            return;

                        case "delete":
                            plugin.Preferences.Remove(ev.Player.SteamId);

                            ev.ReturnMessage = "\n" +
                                               "Preferenze di ruolo cancellate. Avvia \".prefs create\" per rigenerare le tue preferenze e utilizzare nuovamente le preferenze di ruolo.";
                            return;

                        case "hash" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "Non hai nessuna preferenza di ruolo. Se intendi settare le tue preferenze con un hash, usa invece \".prefs hash [generated hash]\".";
                            return;

                        case "hash":
                            ev.ReturnMessage = "\n" +
                                               $"Il tuo hash delle preferenze di ruolo: {string.Join("", plugin.Preferences[ev.Player.SteamId].Preferences.Select(x => PpPlugin.RoleToInt[x].ToString("X"))).ToLower()}";
                            return;
                            
                        case "average" when !plugin.Preferences.Contains(ev.Player.SteamId):
                            ev.ReturnMessage = "\n" +
                                               "Non hai nessuna preferenza di ruolo. Esegui \".prefs create\" per rigenerare le tue preferenze e utilizzare nuovamente le preferenze di ruolo.";
                            return;
                            
                        case "average":
                            ev.ReturnMessage = "\n" +
                                               $"La tua media è {Mathf.Round(plugin.Preferences[ev.Player.SteamId].AverageRank * 100) / 100}.";
                            return;

                        default:
                            ev.ReturnMessage = "\n" +
                                               "Argomento non valido. Esegui \".prefs help\" per una lista completa di comandi.";
                            return;
                    }
                }

                if (plugin.Preferences.Contains(ev.Player.SteamId))
                {
                    int i = 1;
                    ev.ReturnMessage = "\n" +
                                       $"{string.Join("\n", plugin.Preferences[ev.Player.SteamId].Preferences.Select(x => $"{i++} - {PpPlugin.RoleNames[x]}"))}\n" +
                                       "Usa \".prefs help\" per ulteriori informazioni sui comandi.";
                    return;
                }

                ev.ReturnMessage = "\n" +
                                   "Non hai ancora creato le tue preferenze. Per farlo, usa \".prefs create\"";
            }
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            if (ev.PlayerList.Count == 0)
            {
                return;
            }

            plugin.Info("Calculating optimal team respawn roles...");
            if (ev.SpawnChaos)
            {
                plugin.Info("Chaos Insurgency spawn, nothing to do");
            }
            else
            {
                plugin.Info("NTF spawn, Calculating...");
                Dictionary<Player, Role> playerRoles = new Dictionary<Player, Role>();
                int i = 0;
                foreach (Player player in ev.PlayerList)
                {
                    if(i==0)
                        playerRoles.Add(player,Role.NTF_COMMANDER);
                    else if(i<=3)
                        playerRoles.Add(player,Role.NTF_LIEUTENANT);
                    else
                        playerRoles.Add(player,Role.NTF_CADET);
                    i++;
                }
                
                AssignRoles(playerRoles,Role.NTF_CADET);

                ev.PlayerList = playerRoles.OrderBy(x =>
                {
                    switch (x.Value)
                    {
                        case Role.NTF_COMMANDER:
                            return 0;
                        case Role.NTF_LIEUTENANT:
                            return 1;
                        case Role.NTF_CADET:
                            return 2;
                        default:
                            return 3;
                    }
                }).Select(x => x.Key).ToList();
                plugin.Info("Player roles set!");
            }
        }

        public void OnSetConfig(SetConfigEvent ev)
        {
            if (ev.Key == "smart_class_picker" && plugin.DisableSmartClassPicker)
            {
                ev.Value = false;
            }
        }

        private void AssignRoles(Dictionary<Player, Role> playerRoles,Role defaultRole)
        {
           
            List<PlayerData> players = playerRoles.Where(x=>!x.Key.OverwatchMode).Where(x=>x.Value!=Role.UNASSIGNED).Select(x => new PlayerData(x.Key,x.Value,plugin)).ToList();
           
            
            PlayerData[] ranked = players.Where(x => x.Record != null).DefaultIfEmpty(null).ToArray();
            PlayerData[] unranked = players.Where(x => x.Record == null).DefaultIfEmpty(null).ToArray();

            if (ranked[0] != null)
            {
                var swappables = plugin.DistributeAll ? players.ToArray() : ranked;

                int[] RoleCounter = new int[19];

                foreach (var player in swappables)
                {
                    //add role to rolecount if the player can be swapped
                    RoleCounter[(int) player.Role + 1]++;
                }

                plugin.Debug($"Role counter = {{{string.Join(",", RoleCounter)}}}");
                //calculate best assign for ranked players

                _RData data = new _RData(ranked, RoleCounter);
                
                if(ranked.Length > 0 )
                    if (ranked.Length > 1 || plugin.DistributeAll)
                    {
                        _Rassign(ref data, 0);
                    }

                if (data.result) //if players might have been changed
                {
                    plugin.Debug($"Best Assign = {{{string.Join(",", data.bestAssign)}}}");
                    for (int i = 0; i < ranked.Length; i++) //for each ranked player remove it's corresponding role
                    {
                        if (playerRoles.ContainsKey(ranked[i].Player))
                        {
                            Role assign = data.bestAssign[i];
                            playerRoles[ranked[i].Player] = assign;
                            RoleCounter[(int) assign + 1]--;
                        }
                    }

                    plugin.Debug($"Role counter = {{{string.Join(",", RoleCounter)}}}");
                    
                    //if there are unranked players
                    if (unranked[0] != null)
                    {
                        //if there are still roles to assign ( DistributeAll )
                        if (plugin.DistributeAll)
                        {
                            //if the unranked can have it's original role leave it
                            bool[] assigned = new bool[unranked.Length];
                            for (int i = 0; i < unranked.Length; i++)
                            {
                                var player = unranked[i];
                                if (RoleCounter[(int) player.Role + 1] > 0 && !assigned[i])
                                {
                                    assigned[i] = true;
                                    RoleCounter[(int) player.Role + 1]--;
                                }
                            }

                            //for the remains give them the first non empty role in the list
                            for (int i = 0; i < unranked.Length; i++)
                            {
                                var player = unranked[i];
                                if (!assigned[i])
                                {
                                    Role role = defaultRole;
                                    //ignore Role.UNASSIGNED
                                    for (int j = 1; j < 19; j++)
                                    {
                                        if (RoleCounter[j] > 0)
                                        {
                                            role = (Role) j - 1;
                                            break;
                                        }

                                    }

                                    if (playerRoles.ContainsKey(player.Player))
                                    {
                                        playerRoles[player.Player] = role;
                                        if (RoleCounter[(int) role + 1] > 0)
                                        {
                                            RoleCounter[(int) role + 1]--;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                float rating = 100;
                if (data.result)
                {
                    rating = data.bestSum * 100f / 14f;
                    if (plugin.DistributeAll)
                        rating /= players.Count;
                    else
                        rating /= ranked.Length;

                }

                plugin.Info($"Roles set after {data.tryes} tests.");
                plugin.Info($"Ranked players {ranked.Length}");
                plugin.Info($"Rating: {rating}%");

            }
            else
            {
                plugin.Info("Nothing to do, no ranked players");
            }
        }

        private class _RData
        {
            public int tryes;
            public PlayerData[] players;
            public int size;
            public int[] bestRoleCounter;
            public Role[] bestAssign;
            public int bestSum;
            public int[] roleCounter;
            public Role[] actAssign;
            public int actSum;
            public bool result;
            
            public _RData(PlayerData[] players, int[] roleCounter)
            {
                this.players = players;
                this.roleCounter = new int[19];
                this.bestRoleCounter = new int[19];
                Array.Copy(roleCounter,this.roleCounter,roleCounter.Length);
                Array.Copy(roleCounter,this.bestRoleCounter,roleCounter.Length);
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
                    Array.Copy(data.roleCounter,data.bestRoleCounter,data.roleCounter.Length);
                    data.bestSum = data.actSum;
                    data.result = true;
                }
                //check max comparisons || at least one result required
                if (data.tryes++ > plugin.MaxRoundStartComparisons)
                    return true;
                return false;
            }
            
            PlayerData curr = data.players[deept];
            

            foreach (var role in curr.Record.Preferences.Reverse())
            {
                if (data.roleCounter[(int) role +1]>0)
                {
                    int val = curr.Record[role]??0;
                    plugin.Debug($"{curr.Player.Name} - {role} - {val}");
                    data.actAssign[deept] = role;
                    data.roleCounter[(int) role + 1]--;
                    data.actSum += val;
                    if (_Rassign(ref data, deept + 1)) //if true break directly
                        return true;
                    data.roleCounter[(int) role + 1]++;
                    data.actAssign[deept] = Role.UNASSIGNED;
                    data.actSum -= val;
                }
            }

            return false;
        }
    }
}
