namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Governs which player has authority over each class of shared world event.
    ///
    /// Authority model:
    ///   • All authoritative operations (food spawn, organism birth/death, environment events,
    ///     Leviathan spawn) are exclusive to the session host.
    ///   • Clients send requests to the host; the host validates and broadcasts the result.
    ///   • When the session is offline (single-player), all authority checks return true
    ///     so existing single-player code paths require no changes.
    /// </summary>
    public sealed class HostAuthorityController
    {
        private readonly MultiplayerSessionManager _session;

        public HostAuthorityController(MultiplayerSessionManager session)
        {
            _session = session;
        }

        private bool Authoritative =>
            _session == null
            || _session.State == SessionState.Offline
            || _session.IsHost;

        /// <summary>Only the host may spawn food into the shared arena.</summary>
        public bool CanSpawnFood() => Authoritative;

        /// <summary>Only the host may create or destroy organisms in the shared ecosystem.</summary>
        public bool CanSpawnOrganism() => Authoritative;

        /// <summary>Only the host may create shared Cell-stage part pickups.</summary>
        public bool CanSpawnPickup() => Authoritative;

        /// <summary>Only the host may fire environmental events (nutrient bloom, virus outbreak, etc.).</summary>
        public bool CanFireEnvironmentEvent() => Authoritative;

        /// <summary>Only the host may spawn the Leviathan.</summary>
        public bool CanSpawnLeviathan() => Authoritative;
    }
}
