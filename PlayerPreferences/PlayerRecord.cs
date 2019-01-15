using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerRecord
    {
        private readonly string path;
        private readonly Action<string> log;
        
        public float AverageRank { get; private set; }
        public uint AverageCounter { get; private set; }
        public string SteamId { get; }
        
        public PlayerRecord(string path, string steamId, Action<string> log)
        {
            SteamId = steamId;
            this.path = path;
            this.log = log;
        }

        public static PlayerRecord Load(string path, string steamId, Action<string> log)
        {
            PlayerRecord player = new PlayerRecord(path, steamId, log);

            try
            {
                player.Read();
                return player;
            }
            catch
            {
                return null;
            }
        }

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
                Write();
            }
        }

        public int this[Role role]
        {
            get
            {
                for (int i = 0; i < preferences.Length; i++)
                {
                    if (preferences[i] == role)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public void UpdateAverage(int rankAddition)
        {
            AverageRank = (AverageRank * AverageCounter + rankAddition + 1) / ++AverageCounter;
        }

        public void Read()
        {
            string content = File.ReadAllText(path);
            string[] fields = content.Split(':');
            bool write = false;

            const int partsNeeded = 2;
            if (fields.Length < partsNeeded)
            {
                log?.Invoke($"Error while parsing preference file {SteamId}: Less than {partsNeeded} parts found. Setting it to default.");
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
                log?.Invoke($"Error while parsing preference file {SteamId}: Less than {avgPartsNeeded} average parts found. Setting average to default.");
                write = true;

                averages = new[]
                {
                    $"{(PpPlugin.Roles.Count + 1) / 2}",
                    "1"
                };
            }

            if (!float.TryParse(averages[0], out float avgRank))
            {
                log?.Invoke($"Error while parsing preference file {SteamId}: No average rank found. Setting it to average.");
                write = true;

                avgRank = (float)PpPlugin.Roles.Count / 2;
            }
            AverageRank = avgRank;

            if (!uint.TryParse(averages[1], out uint avgCounter))
            {
                log?.Invoke($"Error while parsing preferences file {SteamId}: No average counter found. Setting it to 1.");
                write = true;

                avgCounter = 1;
            }
            AverageCounter = avgCounter;

            string[] strPreferences = fields[1].Split(',');
            int[] intPreferences = new int[PpPlugin.Roles.Count];
            Role[] validRoles = new Role[intPreferences.Length];

            for (int i = 0; i < intPreferences.Length; i++)
            {
                if (i < strPreferences.Length && int.TryParse(strPreferences[i], out int preference) && validRoles.Contains((Role)preference))
                {
                    validRoles[i] = (Role)preference;
                }
                else
                {
                    validRoles[i] = Role.UNASSIGNED;
                }
            }

            // Too many or not enough roles, or error while parsing string into int
            if (strPreferences.Length < validRoles.Length || validRoles.Any(x => x == Role.UNASSIGNED))
            {
                log?.Invoke($"Error while parsing preference file {SteamId}: Too little roles or invalid role numbers. Attempting to fix.");
                write = true;

                for (int i = 0; i < validRoles.Length; i++)
                {
                    // Not set
                    if (validRoles[i] == Role.UNASSIGNED)
                    {
                        validRoles[i] = PpPlugin.Roles.FirstOrDefault(x => !validRoles.Contains(x.Value)).Value;
                    }
                }
            }

            if (strPreferences.Length > intPreferences.Length)
            {
                log?.Invoke($"Error while parsing preference file {SteamId}: Too many roles. Taking the first {PpPlugin.Roles.Count} and writing to file to prevent further errors.");
                write = true;
            }

            preferences = validRoles;

            if (write)
            {
                Write();
            }
        }

        public void Write()
        {
            File.WriteAllText(path, $"{AverageRank},{AverageCounter}:{string.Join(",", preferences.Select(x => (int)x))}");
        }

        public void Delete()
        {
            File.Delete(path);
        }
    }
}
