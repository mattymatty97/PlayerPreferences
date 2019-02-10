using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerRecord
    {
        private readonly PpPlugin plugin;
        private readonly string path;
        
        public float AverageRank { get; private set; }
        public int AverageCounter { get; private set; }
        public string SteamId { get; }

        public PlayerRecord(string path, string steamId, PpPlugin plugin)
        {
            SteamId = steamId;
            this.path = path;
            this.plugin = plugin;
        }

        public static PlayerRecord Load(string path, string steamId, PpPlugin plugin)
        {
            PlayerRecord player = new PlayerRecord(path, steamId, plugin);

            try
            {
                player.Read();
                return player;
            }
            catch (Exception e)
            {
                plugin.Error($"Preference record {steamId} threw an exception while loading:\n{e}");
                return null;
            }
        }
        
        [CanBeNull] private Dictionary<Role,int> Rpreferences;

        private Role[] preferences;
        public Role[] Preferences
        {
            get => preferences;
            set
            {
                if (value.Length != PpPlugin.Roles.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Length does not match the length of all preference roles.");
                }
                Rpreferences = new Dictionary<Role, int>();
                for (int i = 0; i < value.Length; i++)
                {
                    Rpreferences.Add(value[i],i+1);
                }
                preferences = value;
                Write();
            }
        }

        public Role this[int rank]
        {
            get => preferences[rank];
            set
            {
                preferences[rank] = value;
                if (Rpreferences == null)
                {
                    Rpreferences = new Dictionary<Role, int> {{value, rank}};
                }
                else
                    Rpreferences[value] = rank;
                Write();
            }
        }

        public int? this[Role role] => Rpreferences?[role];

        public void UpdateAverage(int rankAddition)
        {
	        int rankWeight = AverageCounter < plugin.MaxAverageCount ? AverageCounter : AverageCounter++;
			AverageRank = (AverageRank * rankWeight + rankAddition) / AverageCounter;
            Write();
        }

        private static void FixRoles(IList<Role> roles)
        {
            Role[] fillerRoles = PpPlugin.Roles.Values.Except(roles).ToArray();
            int fillerI = 0;
            for (int i = 0; i < roles.Count; i++)
            {
                // Not set
                if (roles[i] == Role.UNASSIGNED)
                {
                    roles[i] = fillerRoles[fillerI++];
                }
            }
        }

        public void Read()
        {
            string content = File.ReadAllText(path);
            string[] fields = content.Split(':');
            bool write = false;

            const int partsNeeded = 2;
            if (fields.Length < partsNeeded)
            {
                plugin.Error($"Error while parsing preference file {SteamId}: Less than {partsNeeded} parts found. Setting it to default.");
                write = true;

                Role[] randomRoles = PpPlugin.Roles.Select(x => x.Value).ToArray();
                PpPlugin.Shuffle(randomRoles);
                fields = new[]
                {
                    $"{(PpPlugin.Roles.Count + 1) / 2},1",
                    string.Join("", randomRoles)
                };
            }

            string[] averages = fields[0].Split(',');

            const int avgPartsNeeded = 2;
            if (averages.Length < avgPartsNeeded)
            {
                plugin.Error($"Error while parsing preference file {SteamId}: Less than {avgPartsNeeded} average parts found. Setting average to default.");
                write = true;

                averages = new[]
                {
                    $"{(PpPlugin.Roles.Count + 1) / 2}",
                    "1"
                };
            }

            if (!float.TryParse(averages[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float avgRank))
            {
                plugin.Error($"Error while parsing preference file {SteamId}: No average rank found. Setting it to average.");
                write = true;

                avgRank = (float)(PpPlugin.Roles.Count + 1) / 2;
            }
            AverageRank = avgRank;

            if (!int.TryParse(averages[1], out int avgCounter))
            {
                plugin.Error($"Error while parsing preferences file {SteamId}: No average counter found. Setting it to 1.");
                write = true;

                avgCounter = 1;
            }
            AverageCounter = avgCounter;

            string[] strPreferences = fields[1].Split(',');
            int[] intPreferences = new int[PpPlugin.Roles.Count];
            Role[] validRoles = new Role[intPreferences.Length];

            for (int i = 0; i < strPreferences.Length && i < intPreferences.Length; i++)
            {
                if (int.TryParse(strPreferences[i], out int preference) && PpPlugin.IntToRole.ContainsKey(preference))
                {
                    validRoles[i] = PpPlugin.IntToRole[preference];
                }
                else
                {
                    validRoles[i] = Role.UNASSIGNED;
                }
            }

            // Too many or not enough roles, or error while parsing string into int
            if (strPreferences.Length < validRoles.Length)
            {
                plugin.Error($"Error while parsing preference file {SteamId}: Too little roles. Attempting to fix.");
                write = true;

                FixRoles(validRoles);
            }
            else if (validRoles.Any(x => x == Role.UNASSIGNED))
            {
                plugin.Error($"Error while parsing preference file {SteamId}: Invalid role numbers. Attempting to fix.");
                write = true;

                FixRoles(validRoles);
            }

            if (strPreferences.Length > intPreferences.Length)
            {
                plugin.Error($"Error while parsing preference file {SteamId}: Too many roles. Taking the first {PpPlugin.Roles.Count} and writing to file to prevent further errors.");
                write = true;
            }

            preferences = validRoles;

            if (write)
            {
                Write();
            }
        }

	    public float? RoleRating(Role newRole)
	    {
		    int? roleRank = this[newRole];

		    if (roleRank == null)
		    {
			    return null;
		    }

		    return roleRank.Value + (AverageRank - roleRank.Value) * plugin.RankWeightMultiplier;
	    }

		public void Write()
        {
            File.WriteAllText(path, OutputData());
        }

        public void Delete()
        {
            File.Delete(path);
        }

	    public string OutputData()
	    {
		    return $"{AverageRank},{AverageCounter.ToString(CultureInfo.InvariantCulture).Replace(",", "")}:{string.Join(",", preferences.Select(x => PpPlugin.RoleToInt[x]))}";
	    }

	    public override string ToString()
	    {
		    return $"{AverageRank} (count {AverageCounter}) {string.Join(",", preferences.Select(x => PpPlugin.RoleToInt[x]))} ({SteamId})";
	    }
    }
}
