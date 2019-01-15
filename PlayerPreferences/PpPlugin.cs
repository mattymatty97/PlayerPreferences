using System;
using System.Collections.Generic;
using System.Linq;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Events;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlayerPreferences
{
    [PluginDetails(
        name = "Player Preferences",
        author = "4aiur",
        description = "Allows players to set their respawn role Preferences.",
        id = "4aiur.custom.playerpreferences",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 0,
        version = "1.2.0"
            )]
    public class PpPlugin : Plugin
    {
        public static Dictionary<string, Role> Roles { get; private set; }
        public static Dictionary<Role, string> RoleNames { get; private set; }

        public static Dictionary<Role, int> RoleToInt { get; private set; }
        public static Dictionary<int, Role> IntToRole { get; private set; }
        
        public string[] RaRanks { get; private set; }
        public bool DistributeAll { get; private set; }
        public float RankWeightMultiplier { get; private set; }

        public Preferences Preferences { get; private set; }
        public EventHandlers Handlers { get; private set; }

        public void LoadRoleData()
        {
            Roles = new Dictionary<string, Role>
            {
                {
                    "classd",
                    Role.CLASSD
                },
                {
                    "scientist",
                    Role.SCIENTIST
                },
                {
                    "facilityguard",
                    Role.FACILITY_GUARD
                },
                {
                    "mtf.cadet",
                    Role.NTF_CADET
                },
                {
                    "mtf.lieutenant",
                    Role.NTF_LIEUTENANT
                },
                {
                    "mtf.commander",
                    Role.NTF_COMMANDER
                },
                {
                    "chaosinsurgency",
                    Role.CHAOS_INSURGENCY
                },
                {
                    "scp.049",
                    Role.SCP_049
                },
                {
                    "scp.079",
                    Role.SCP_079
                },
                {
                    "scp.096",
                    Role.SCP_096
                },
                {
                    "scp.106",
                    Role.SCP_106
                },
                {
                    "scp.173",
                    Role.SCP_173
                },
                {
                    "scp.939-53",
                    Role.SCP_939_53
                },
                {
                    "scp.939-89",
                    Role.SCP_939_89
                }
            };
            RoleNames = Roles.ToDictionary(x => x.Value, x => x.Key);

            int i = 0;
            RoleToInt = Roles.ToDictionary(x => x.Value, x => i++);
            IntToRole = RoleToInt.ToDictionary(x => x.Value, x => x.Key);
        }

        public override void Register()
        {
            LoadRoleData();
            Preferences = new Preferences("PlayerPrefs", Error);
            Info("Loaded preference files.");

            string[] defaultAliases =
            {
                "prefs",
                "playerprefs",
                "preferences"
            };
            AddConfig(new ConfigSetting("prefs_rank", new[] {"owner"}, SettingType.LIST, true,
                "Ranks allowed to adjust player Preferences."));
            AddConfig(new ConfigSetting("prefs_aliases", defaultAliases, SettingType.LIST, true,
                "Client console commands that can be used to run the Player Preferences."));
            AddConfig(new ConfigSetting("prefs_distribute_all", false, SettingType.BOOL, true,
                "Whether or not to swap roles with people who do not have preferences set."));
            AddConfig(new ConfigSetting("prefs_rank_weight_multiplier", 1f, SettingType.FLOAT, true,
                "The multiplier of the rank weight difference."));

            Handlers = new EventHandlers(this)
            {
                CommandAliases = defaultAliases
            };

            AddEventHandlers(Handlers, Priority.High);
            AddCommands(defaultAliases, new PlayerPrefCommand(this));
        }

        public void RefreshConfig()
        {
            RaRanks = GetConfigList("prefs_rank");
            Handlers.CommandAliases = GetConfigList("prefs_aliases");
            DistributeAll = GetConfigBool("prefs_distribute_all");
            RankWeightMultiplier = GetConfigFloat("prefs_rank_weight_multiplier");
        }

        public override void OnEnable()
        {
            Info("Player preferences enabled!");
        }

        public override void OnDisable()
        {
            Info("Player preferences disabled!");
        }

        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = t[j - 1] == s[i - 1] ? 0 : 1;

                    d[i, j] = Mathf.Min(
                        Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static Role GetRole(string name)
        {
            return Roles
                .Select(x => (role: x.Value, distance: LevenshteinDistance(name, x.Key)))
                .OrderBy(x => x.distance).First()
                .role;
        }

        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }
    }
}
