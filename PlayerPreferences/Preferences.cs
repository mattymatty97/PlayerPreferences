using System.Collections.Generic;
using System.IO;
using Smod2.API;

namespace PlayerPreferences
{
    public class Preferences
    {
        private readonly PpPlugin plugin;
        private readonly string directory;
        private readonly Dictionary<string, PlayerRecord> records;

        public IEnumerable<PlayerRecord> Records => records.Values;

        public Preferences(string directory, PpPlugin plugin)
        {
            this.directory = directory;
            this.plugin = plugin;
            records = new Dictionary<string, PlayerRecord>();

            Read();
        }

        public PlayerRecord Add(string steamId, Role[] preferences)
        {
            PlayerRecord record = new PlayerRecord($"{directory}/{steamId}.txt", steamId, plugin)
            {
                Preferences = preferences
            };
            record.Write();

            records.Add(steamId, record);

            return record;
        }

        public bool Remove(string steamId)
        {
            if (!records.ContainsKey(steamId))
            {
                return false;
            }

            records[steamId].Delete();
            records.Remove(steamId);

            return true;
        }

        public bool Contains(string steamId)
        {
            return records.ContainsKey(steamId);
        }

        public void Read()
        {
            if (Directory.Exists(directory))
            {
                foreach (string file in Directory.GetFiles(directory, "*.txt"))
                {
                    string steamId = Path.GetFileNameWithoutExtension(file);
                    PlayerRecord record = PlayerRecord.Load(file, steamId, plugin);

                    if (record == null)
                    {
                        plugin.Error($"Preference record {file} is either corrupt or out of date.");
                    }
                    else
                    {
                        if (records.ContainsKey(steamId))
                        {
                            records[steamId] = record;
                        }
                        else
                        {
                            records.Add(steamId, record);
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(directory);
            }
        }

        public PlayerRecord this[string steamId] => records[steamId];
    }
}
