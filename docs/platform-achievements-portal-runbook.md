# Platform achievements portal runbook

Use `docs/platform-achievements-manifest.csv` as the source data for all platform dashboards. Keep the local IDs stable after release.

## Steamworks

1. Confirm Steamworks.NET is installed in `Packages/manifest.json` as `com.rlabrecque.steamworks.net`.
2. Set `PlatformServicesConfig.SteamAppId` to the real Steam App ID.
3. Set `RoobsWorld/steam_appid.txt` to the same App ID for local testing.
4. In Steamworks, create one achievement per manifest row:
   - API Name: `steam_api_name`
   - Display Name: `title`
   - Description: `description`
   - Hidden: `hidden`
5. Configure Steam Cloud for `Application.persistentDataPath/Saves/*.json`.
6. Build a development Windows player and launch through Steam.
7. Unlock one achievement and confirm it appears in Steam client/profile.
8. Repeat enough flows to cover every achievement trigger.

## PlayStation

1. Install the PlayStation Unity SDK/plugin.
2. Set `PlatformServicesConfig.PlayStationTitleServiceId`.
3. Build the trophy pack from the manifest:
   - Trophy ID: `playstation_trophy_id`
   - Name: `title`
   - Description: `description`
   - Hidden: `hidden`
4. Assign grades and icons according to platform design/certification guidance.
5. Add `ROOBSWORLD_PLAYSTATION` to PlayStation player scripting symbols.
6. Replace the local bridge in `PlayStationAchievementProvider` with SDK unlock calls.
7. Build to development hardware.
8. Verify signed-in user binding, offline behavior, hidden trophies, suspend/resume, and network loss.
9. Confirm every trophy unlock in platform tooling.

## Xbox

1. Install the Xbox GDK/XSAPI Unity integration.
2. Set `PlatformServicesConfig.XboxTitleId`.
3. In Partner Center, create one achievement per manifest row:
   - Achievement ID: `xbox_achievement_id`
   - Name: `title`
   - Description: `description`
   - Hidden: `hidden`
   - Gamerscore: derive from `points` or platform-approved allocation.
4. Add `ROOBSWORLD_XBOX` to Xbox player scripting symbols.
5. Replace the local bridge in `XboxAchievementProvider` with Xbox services unlock calls.
6. Build to development hardware or the configured Xbox services sandbox.
7. Verify signed-in user binding, offline behavior, hidden achievements, suspend/resume, sign-out, and network loss.
8. Confirm every achievement unlock in Xbox services.

## Local validation

Run:

```powershell
.\tools\validate-platform-achievements.ps1
```

This checks for imported SDK files, real service IDs, and the achievement manifest. It cannot verify partner dashboard state or hardware unlocks.
