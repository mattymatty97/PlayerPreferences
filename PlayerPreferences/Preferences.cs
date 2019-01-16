using System;
using System.Collections.Generic;
using System.IO;
using Smod2.API;

namespace PlayerPreferences
{
    public class Preferences
    {
        private readonly string directory;
        private readonly Action<string> log;
        private readonly Dictionary<string, PlayerRecord> records;

        public IEnumerable<PlayerRecord> Records => records.Values;

        public Preferences(string directory, Action<string> log)
        {
            this.directory = directory;
            this.log = log;
            records = new Dictionary<string, PlayerRecord>();

            Read();
        }

        public void Add(string steamId, Role[] preferences)
        {
            PlayerRecord record = new PlayerRecord($"{directory}/{steamId}.txt", steamId, log)
            {
                Preferences = preferences
            };
            record.Write();

            records.Add(steamId, record);
        }

        public void Remove(string steamId)
        {
            if (!records.ContainsKey(steamId))
            {
                return;
            }

            records[steamId].Delete();
            records.Remove(steamId);
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
                    PlayerRecord record = PlayerRecord.Load(file, steamId, log);

                    if (record == null)
                    {
                        log?.Invoke($"Preference record {file} is either corrupt or out of date. Take a look at it and make sure all the roles are valid, and split by comma.");
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
