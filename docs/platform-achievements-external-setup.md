# Platform achievements external setup

This file captures the work that cannot be completed from the local repo without vendor SDKs, partner dashboards, dev hardware, and production service IDs.

## Required vendor inputs

- Steam:
  - Real Steam App ID.
  - Steamworks.NET package imported into `RoobsWorld/Assets` or `RoobsWorld/Packages`.
  - Steamworks dashboard access for achievements and Steam Cloud.
- PlayStation:
  - PlayStation Unity SDK/plugin installed.
  - Title service ID and trophy pack IDs.
  - Development hardware for trophy verification.
- Xbox:
  - Xbox GDK/XSAPI Unity integration installed.
  - Partner Center title ID and achievement configuration access.
  - Development hardware or configured Xbox services sandbox.

## Local files to update after credentials exist

- `RoobsWorld/Assets/Scripts/Core/PlatformServicesConfig.cs`
  - Replace `SteamAppId`.
  - Replace `PlayStationTitleServiceId`.
  - Replace `XboxTitleId`.
- `RoobsWorld/steam_appid.txt`
  - Replace `480` with the real Steam App ID for local Steam testing.
- `docs/platform-achievements-manifest.csv`
  - Use this as the dashboard/trophy-pack source data.

## Steam Cloud setup target

RoobsWorld writes save files under:

`Application.persistentDataPath/Saves/*.json`

Configure Steam Cloud to sync the save slot JSON files for the Windows build. Keep the same slot filenames across machines so local historical achievements and run records survive reinstall/device changes.

## Completion rule

Do not mark the external setup items complete until the service dashboard confirms the configured ID and at least one development build has unlocked a test achievement on the target platform.

## Local readiness check

Run:

```powershell
.\tools\validate-platform-achievements.ps1
```

The script reports missing SDK packages and placeholder platform IDs before a platform build is attempted.
