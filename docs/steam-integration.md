# Steam integration

RoobsWorld now has a Steam platform layer for desktop builds:

- Steam lifecycle bootstrap before the first scene loads.
- Steamworks callback pumping and clean shutdown.
- App restart through the Steam client when required.
- Persona name, Steam ID, overlay availability, Friends overlay, and store overlay helpers.
- Achievement reporting through `SteamUserStats.SetAchievement` / `StoreStats`.
- Existing locally unlocked achievements are synced to Steam once the provider binds to `GameManager`.
- `steam_appid.txt` is written beside standalone builds for local Steam testing.

## Enable Steamworks.NET

1. Steamworks.NET is installed through Unity Package Manager as `com.rlabrecque.steamworks.net#2025.162.1`.
2. The package supplies `STEAMWORKS_NET`; RoobsWorld references the Steamworks.NET assembly directly.
3. Replace the development App ID `480` in:
   - `Assets/Scripts/Core/PlatformServicesConfig.cs`
   - `steam_appid.txt`
4. Configure matching achievement API names in Steamworks. The current default mapping is the upper-case game achievement id, for example `cell_first_eat` maps to `CELL_FIRST_EAT`.
5. In Steamworks, enable Cloud saves against the game's persistent save directory pattern. RoobsWorld saves JSON slots under Unity's `Application.persistentDataPath/Saves`.

## Steam achievement API names

Current RoobsWorld achievements:

| Game id | Steam API name |
| --- | --- |
| `cell_first_eat` | `CELL_FIRST_EAT` |
| `cell_first_goal` | `CELL_FIRST_GOAL` |
| `cell_grow_large` | `CELL_GROW_LARGE` |
| `cell_survive_predator` | `CELL_SURVIVE_PREDATOR` |
| `cell_all_parts` | `CELL_ALL_PARTS` |
| `cell_evolve` | `CELL_EVOLVE` |
| `cell_first_speciation` | `CELL_FIRST_SPECIATION` |
| `cell_first_extinction` | `CELL_FIRST_EXTINCTION` |
| `cell_10_species` | `CELL_10_SPECIES` |
| `cell_survived_leviathan` | `CELL_SURVIVED_LEVIATHAN` |
| `cell_defeated_leviathan` | `CELL_DEFEATED_LEVIATHAN` |
| `creature_first_kill` | `CREATURE_FIRST_KILL` |
| `creature_pack_breaker` | `CREATURE_PACK_BREAKER` |
| `creature_apex_streak` | `CREATURE_APEX_STREAK` |

Keep Steam's dashboard names stable after players ship with them.
