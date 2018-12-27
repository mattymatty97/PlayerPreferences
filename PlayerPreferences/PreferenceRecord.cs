using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Smod2.API;

namespace PlayerPreferences
{
    public class PlayerRecord
    {
        private readonly string path;
        private readonly Action<string> log;
        
        public string SteamId { get; private set; }
        
        public PlayerRecord(string path, string steamId, Action<string> log)
        {
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
            set
            {
                if (value.Length != Plugin.Roles.Count)
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

        public void Read()
        {
            string content = File.ReadAllText(path);
            string[] strPreferences = content.Split(',');
            int[] intPreferences = new int[Plugin.Roles.Count];

            for (int i = 0; i < strPreferences.Length; i++)
            {
                if (int.TryParse(strPreferences[i], out int preference))
                {
                    intPreferences[i] = preference + 1;
                }
            }

            // Too many or not enough roles, or error while parsing string into int
            if (strPreferences.Length != intPreferences.Length || intPreferences.Any(x => x == 0))
            {
                log?.Invoke($"Error while parsing preference file {SteamId}. Attempting to fix.");
                for (int i = 0; i < intPreferences.Length; i++)
                {
                    // Not set
                    if (intPreferences[i] == 0)
                    {
                        intPreferences[i] = (int)Plugin.Roles.FirstOrDefault(x => !intPreferences.Contains((int) x.Value + 1)).Value + 1;
                    }
                }
            }

            preferences = intPreferences.Select(x => (Role) (x - 1)).ToArray();
        }

        public void Write()
        {
            File.WriteAllText(path, string.Join(",", preferences.Select(x => (int) x)));
        }

        public void Delete()
        {
            File.Delete(path);
        }
    }
}
