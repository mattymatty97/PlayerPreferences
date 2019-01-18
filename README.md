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
| prefs [#] [role] | Client | Swaps the current role in said rank out with the role specified. |
| prefs hash | Client | Gives hash of current preferences. |
| prefs hash [hash] | Client | Sets preferences to the specified hash. |
| prefs average | Client | Gets the server-stored average rankings used for weighting purposes. |
| prefs reload [selector] | RA / Server | Reloads all Player Preferences files of players selected. |
| prefs delete [selector] | RA / Server | Deletes Player Preferences of the players selected. |

| Parameter | Value Type | Description |
| :-------: | :--------: | :---------- |
| # | Integer | The rank of a role, shown by the number on the left side of the `prefs` list. |
| role | String | The name of a role, shown by the text on the right side of the `prefs` list. |
| selector | String / Integer | The filter when using an RA command. Can be wildcard (`*`) to select all players, or to select one player a player ID or SteamID can be used. |
| hash | String | The hash outputted by running `prefs hash`. |

# Configs

| Config | Value Type | Default | Description |
| :----: | :--------: | :-----: |:----------- |
| prefs_rank | String List | owner | Ranks allowed to run the RA commands. |
| prefs_aliases | String List | prefs, playerprefs | Aliases for all Player Preferences client console commands. |
| prefs_distribute_all | Boolean | false | Whether or not to swap roles with those who don't have their preferences set. |
| prefs_weight_multiplier | Float | 1 | The multiplier used on weights in the swap calculations. Higher means weights will have a greater effect. Lower means they wont have as much as an effect. Negative might mean people with high average ranks get higher ranks and those with lower get lower ranks (unconfirmed). |
| prefs_weight_max | Integer | 5 | The amount of average ranks to lossily store in the players (all are saved as a single value in a 32-bit float rather than several floats). These are also used in weights, so the less to store the more weights will fluctuate, and conversely the more to store the less they will fluctuate. |
| prefs_smart_class_picker | Boolean | false | Whether or not to allow Smart Class Picker. Disabled by default because all the work done by Smart Class Picker is overridden by Player Preferences so it is a waste of processing power. |
