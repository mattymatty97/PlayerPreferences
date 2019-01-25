using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public string Dump()
	    {
		    DateTime time = DateTime.Now;
		    string path = Path.Combine(plugin.DumpDirectory, $"{time.Year}-{time.Month:00}-{time.Day:00}_{time.Hour:00}_{time.Minute:00}.txt");

			File.WriteAllLines(path, records
				.Select(x => 
					x.Key + ":" + 
					x.Value.OutputData() + ":" + 
					string.Join(",", x.Value.Preferences.Select(y => x.Value.RoleRating(y)))
				)
			);

		    return path;
	    }

        public PlayerRecord this[string steamId] => records[steamId];
    }
}
