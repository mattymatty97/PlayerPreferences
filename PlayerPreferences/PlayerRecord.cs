using System;
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
            player.Read();

            return player;
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
            AverageRank = (AverageRank * AverageCounter + rankAddition) / ++AverageCounter;
        }

        public void Read()
        {
            string content = File.ReadAllText(path);
            string[] fields = content.Split(':');

            const int partsNeeded = 2;
            if (fields.Length < partsNeeded)
            {
                log?.Invoke($"Error while parsing preference file {SteamId} (less than {partsNeeded} parts found). Setting it to default.");

                fields = new[]
                {
                    "7,1",
                    string.Join("", PpPlugin.Roles.Select(x => (int)x.Value))
                };
            }

            string[] averages = fields[0].Split(',');

            if (!float.TryParse(averages[0], out float avgRank))
            {
                log?.Invoke($"Error while parsing preference file {SteamId} (no average rank found). Setting it to average.");

                avgRank = (float)PpPlugin.Roles.Count / 2;
            }
            AverageRank = avgRank;

            if (!uint.TryParse(averages[1], out uint avgCounter))
            {
                log?.Invoke($"Error while parsing preferences file {SteamId} (no average counter found). Setting it to 1.");

                avgCounter = 1;
            }
            AverageCounter = avgCounter;

            string[] strPreferences = fields[1].Split(',');
            int[] intPreferences = new int[PpPlugin.Roles.Count];
            int[] validRoles = PpPlugin.Roles.Select(x => (int)x.Value).ToArray();

            for (int i = 0; i < intPreferences.Length; i++)
            {
                if (i < strPreferences.Length && int.TryParse(strPreferences[i], out int preference) && validRoles.Contains(preference))
                {
                    intPreferences[i] = preference + 1;
                }
            }

            // Too many or not enough roles, or error while parsing string into int
            if (strPreferences.Length < intPreferences.Length || intPreferences.Any(x => x == 0))
            {
                log?.Invoke($"Error while parsing preference file {SteamId} (too little roles or invalid role numbers). Attempting to fix.");
                for (int i = 0; i < intPreferences.Length; i++)
                {
                    // Not set
                    if (intPreferences[i] == 0)
                    {
                        intPreferences[i] = (int)PpPlugin.Roles.FirstOrDefault(x => !intPreferences.Contains((int) x.Value + 1)).Value + 1;
                    }
                }
            }

            if (strPreferences.Length > intPreferences.Length)
            {
                log?.Invoke($"Error while parsing preference file {SteamId} (too many roles). Unable to fix, overwriting with random roles.");
                Role[] myRoles = validRoles.Cast<Role>().ToArray();
                PpPlugin.Shuffle(myRoles);

                preferences = myRoles;
                Write();
            }
            else
            {
                preferences = intPreferences.Select(x => (Role)(x - 1)).ToArray();
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
