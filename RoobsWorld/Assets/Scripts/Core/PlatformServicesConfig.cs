namespace RoobsWorld.Core
{
    public static class PlatformServicesConfig
    {
        public const uint SteamDevelopmentAppId = 480;
        public const uint SteamAppId = SteamDevelopmentAppId;
        public const string SteamRichPresenceDisplayToken = "#Status";

        public const string PlayStationTitleServiceId = "REPLACE_WITH_PLAYSTATION_TITLE_SERVICE_ID";
        public const string XboxTitleId = "REPLACE_WITH_XBOX_TITLE_ID";

        public static bool UsesDevelopmentSteamAppId => SteamAppId == SteamDevelopmentAppId;
        public static bool UsesPlaceholderPlayStationServiceId =>
            PlayStationTitleServiceId == "REPLACE_WITH_PLAYSTATION_TITLE_SERVICE_ID";
        public static bool UsesPlaceholderXboxTitleId => XboxTitleId == "REPLACE_WITH_XBOX_TITLE_ID";
    }
}
