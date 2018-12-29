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
            get => preferences;
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

        public void Read()
        {
            string content = File.ReadAllText(path);
            string[] strPreferences = content.Split(',');
            int[] intPreferences = new int[Plugin.Roles.Count];
            int[] validRoles = Plugin.Roles.Select(x => (int)x.Value).ToArray();

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
                        intPreferences[i] = (int)Plugin.Roles.FirstOrDefault(x => !intPreferences.Contains((int) x.Value + 1)).Value + 1;
                    }
                }
            }

            if (strPreferences.Length > intPreferences.Length)
            {
                log?.Invoke($"Error while parsing preference file {SteamId} (too many roles). Unable to fix, overwriting with random roles.");
                Role[] myRoles = validRoles.Cast<Role>().ToArray();
                Plugin.Shuffle(myRoles);

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
            File.WriteAllText(path, string.Join(",", preferences.Select(x => (int) x)));
        }

        public void Delete()
        {
            File.Delete(path);
        }
    }
}
