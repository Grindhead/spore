# Steamworks Integration

The Unity project includes Steamworks.NET through `Packages/manifest.json` and the runtime assembly is referenced by `Assets/Scripts/RoobsWorld.asmdef`.

## Runtime Features

- `SteamClientIntegration` is created before scene load and owns Steam API init, callback pumping, rich presence updates, and shutdown.
- `SteamPlatformProvider` reports game achievements to Steam using the `SteamApiName` values in `AchievementCatalog`.
- Existing local achievement unlocks are replayed to Steam after the provider binds, so offline/local unlocks are not stranded.
- Steam Cloud mirrors save JSON under `saves/{slot}.json` when Steam Remote Storage is available.
- If a local save is missing, `SaveManager.Load` attempts to restore the slot from Steam Cloud before falling back to a new run.
- Stage transitions update rich presence keys: `steam_display`, `status`, and `stage`.

## Release Checklist

- Replace `PlatformServicesConfig.SteamAppId` with the real Steam app id. It currently uses Steam's development app id `480`.
- Keep `steam_appid.txt` only for local developer runs. Steam-launched builds do not need it beside the executable.
- Configure matching achievements in Steamworks partner backend using the `SteamApiName` values from `AchievementCatalog`.
- Configure Steam Cloud for the remote path `saves/*.json`.
- Configure the rich presence token referenced by `PlatformServicesConfig.SteamRichPresenceDisplayToken` (`#Status` by default), or change the token to match the Steamworks backend localization.
- Run a packaged build through the Steam client before release; editor validation can prove compile safety, but not overlay, cloud, or entitlement behavior.
