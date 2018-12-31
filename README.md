# PlayerPreferences
Allows players to rank their favorite roles in order to be more likely to spawn as them.

# Installation
**[Smod2](https://github.com/Grover-c13/Smod2) must be installed for this to work.**

Place the `PlayerPreferences.dll` file in your sm_plugins folder.

# Commands
Note: to use client commands, prefix them by a dot. E.g: `.prefs`.

| Command | Console | Description |
| :-----: | :-----: | :---------- |
| prefs | Client | Lists rank number and corresponding role name. |
| prefs help | Client | Lists all client console commands. |
| prefs create | Client | Creates random preferences to allow the user to use all the other preferences commands (requires DNT off). |
| prefs delete | Client | Deletes all Player Preferences data about a user. They will have to use the `prefs create` command above to use Player Preferences again. |
| prefs [rank number] [role name] | Client | Swaps the current role in said rank out with the role specified. |
| prefs reload [selector] | RA / Server | Reloads all Player Preferences files of players selected. |
| prefs delete [selector] | RA / Server | Deletes Player Preferences of the players selected. |

| Parameter | Value Type | Description |
| :-------: | :--------: | :---------- |
| rank number | Integer | The rank of a role, shown by the number on the left side of the `prefs` list. |
| role name | String | The name of a role, shown by the text on the right side of the `prefs` list. |
| selector | String / Integer | The filter when using an RA command. Can be wildcard (`*`) to select all, or to select one a player ID or SteamID can be used. |

# Configs

| Config | Value Type | Default | Description |
| :----: | :--------: | :-----: |:----------- |
| playerprefs_rank | String List | owner | Ranks allowed to run the RA commands. |
| playerprefs_aliases | String List | prefs, playerprefs | Aliases for all Player Preferences commands (server, RA, and client console). |
