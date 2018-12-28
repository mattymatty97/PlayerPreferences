﻿using System.Collections.Generic;
using System.Linq;
using scp4aiur;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Events;
using UnityEngine;

namespace PlayerPreferences
{
    [PluginDetails(
        name = "Player Preferences",
        author = "4aiur",
        description = "Allows players to set their respawn role preferences.",
        id = "4aiur.custom.playerpreferences",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 0,
        version = "1.0.0"
            )]
    public class Plugin : Smod2.Plugin
    {
        public static readonly Dictionary<string, Role> Roles = new Dictionary<string, Role>
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
                "mtf.scientist",
                Role.NTF_SCIENTIST
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
        public static Dictionary<Role, string> RoleNames { get; private set; }

        internal static Preferences preferences;
        internal static string[] ranks;

        public override void Register()
        {
            RoleNames = Roles.ToDictionary(x => x.Value, x => x.Key);

            Timing.Init(this);
            preferences = new Preferences("PlayerPrefs", Info);

            AddConfig(new ConfigSetting("playerprefs_rank", new[] {"owner"}, SettingType.LIST, true, "Ranks allowed to adjust player preferences."));
            AddConfig(new ConfigSetting("playerprefs_aliases", new[]
            {
                "pp",
                "prefs",
                "playerprefs"
            }, SettingType.LIST, true, "Client console commands that can be used to run the Player Preferences."));

            string[] aliases = GetConfigList("playerprefs_aliases");
            AddEventHandlers(new EventHandlers(this)
            {
                CommandAliases = aliases
            }, Priority.High);
            AddCommands(aliases, new PlayerPrefCommand());
        }

        public void RefreshConfig()
        {
            ranks = GetConfigList("playerprefs_rank");
        }

        public override void OnEnable() { }
        public override void OnDisable() { }

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
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Mathf.Min(
                        Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static Role GetRole(string name)
        {
            return Roles.Select(x => (x.Value, LevenshteinDistance(name, x.Key))).OrderBy(x => x.Item2).First().Item1;
        }
    }
}