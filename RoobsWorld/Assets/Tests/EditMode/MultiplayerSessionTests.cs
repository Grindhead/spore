using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>EditMode tests for MultiplayerSessionManager, HostAuthorityController, and GenomeChecksum.</summary>
    public class MultiplayerSessionTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        // ---- session creation ----

        [Test]
        public void CreateSession_Returns_SixChar_Code()
        {
            var mgr  = new MultiplayerSessionManager();
            string code = mgr.CreateSession();
            Assert.AreEqual(MultiplayerSessionManager.SessionCodeLength, code.Length);
        }

        [Test]
        public void CreateSession_Sets_Hosting_State()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.AreEqual(SessionState.Hosting, mgr.State);
        }

        [Test]
        public void CreateSession_LocalPlayer_Is_Host()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.IsTrue(mgr.IsHost);
        }

        [Test]
        public void CreateSession_Publishes_SessionStartedEvent()
        {
            MultiplayerSessionStartedEvent? received = null;
            EventBus.Subscribe<MultiplayerSessionStartedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            string code = mgr.CreateSession();

            Assert.IsNotNull(received);
            Assert.AreEqual(code, received.Value.SessionCode);
        }

        // ---- joining ----

        [Test]
        public void JoinSession_Valid_Code_Sets_Connected_State()
        {
            var mgr = new MultiplayerSessionManager();
            bool result = mgr.JoinSession("ABCD12", localPlayerId: 1);
            Assert.IsTrue(result);
            Assert.AreEqual(SessionState.Connected, mgr.State);
        }

        [Test]
        public void JoinSession_Invalid_Short_Code_Returns_False()
        {
            var mgr = new MultiplayerSessionManager();
            bool result = mgr.JoinSession("XY", localPlayerId: 1);
            Assert.IsFalse(result);
            Assert.AreEqual(SessionState.Offline, mgr.State);
        }

        [Test]
        public void JoinSession_Null_Code_Returns_False()
        {
            var mgr = new MultiplayerSessionManager();
            Assert.IsFalse(mgr.JoinSession(null, localPlayerId: 1));
        }

        [Test]
        public void JoinSession_Allows_PlayerIds_Above_Four()
        {
            var joiner = new MultiplayerSessionManager();
            bool result = joiner.JoinSession("ABCD12", localPlayerId: 4);

            Assert.IsTrue(result);
            Assert.AreEqual(4, joiner.LocalPlayerId);
        }

        // ---- adding/removing players ----

        [Test]
        public void AddRemotePlayer_Increments_PlayerCount()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.AreEqual(1, mgr.PlayerCount);

            mgr.AddRemotePlayer(1, "Alice");
            Assert.AreEqual(2, mgr.PlayerCount);
        }

        [Test]
        public void AddRemotePlayer_Allows_More_Than_Four_Players()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsTrue(mgr.AddRemotePlayer(1, "P1"));
            Assert.IsTrue(mgr.AddRemotePlayer(2, "P2"));
            Assert.IsTrue(mgr.AddRemotePlayer(3, "P3"));
            Assert.IsTrue(mgr.AddRemotePlayer(4, "P4"));
            Assert.IsTrue(mgr.AddRemotePlayer(5, "P5"));

            Assert.AreEqual(6, mgr.PlayerCount);
        }

        [Test]
        public void AddRemotePlayer_Fires_PlayerJoined_Event()
        {
            MultiplayerPlayerJoinedEvent? received = null;
            EventBus.Subscribe<MultiplayerPlayerJoinedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "Bob");

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Value.PlayerId);
        }

        [Test]
        public void RemovePlayer_Decrements_PlayerCount()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "Alice");
            Assert.AreEqual(2, mgr.PlayerCount);

            mgr.RemovePlayer(1);
            Assert.AreEqual(1, mgr.PlayerCount);
        }

        [Test]
        public void RemovePlayer_NonHost_Does_Not_Trigger_Migration()
        {
            int migratedTo = -1;
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "Alice");
            mgr.OnHostMigrated += id => migratedTo = id;

            mgr.RemovePlayer(1); // remove non-host player

            Assert.AreEqual(-1, migratedTo);
            Assert.IsTrue(mgr.IsHost); // local player (0) still host
        }

        // ---- host migration ----

        [Test]
        public void RemovePlayer_Host_Triggers_Migration_To_Next_Player()
        {
            int migratedTo = -1;
            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("AAAA11", localPlayerId: 1); // we are not host (host = 0)
            mgr.AddRemotePlayer(2, "Alice");
            mgr.OnHostMigrated += id => migratedTo = id;

            mgr.RemovePlayer(0); // host (player 0) leaves

            Assert.AreEqual(2, migratedTo); // first remaining remote player promoted
        }

        [Test]
        public void RemovePlayer_Host_With_No_Others_Sets_Offline()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("AAAA11", localPlayerId: 1);
            // no other remote players added

            mgr.RemovePlayer(0); // host leaves, no one left

            Assert.AreEqual(SessionState.Offline, mgr.State);
        }

        // ---- leave session ----

        [Test]
        public void LeaveSession_Sets_Offline_State()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.LeaveSession();
            Assert.AreEqual(SessionState.Offline, mgr.State);
        }

        // ---- clans ----

        [Test]
        public void CreateClan_Valid_Name_And_Tag_Creates_Local_Clan()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            bool created = mgr.CreateClan("Tide Hunters", "tide");

            Assert.IsTrue(created);
            Assert.IsNotNull(mgr.LocalClan);
            Assert.AreEqual("Tide Hunters", mgr.LocalClan.Name);
            Assert.AreEqual("TIDE", mgr.LocalClan.Tag);
            Assert.AreEqual(mgr.LocalPlayerId, mgr.LocalClan.LeaderPlayerId);
        }

        [Test]
        public void CreateClan_Invalid_Tag_Returns_False()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsFalse(mgr.CreateClan("Tide Hunters", "x"));
            Assert.IsNull(mgr.LocalClan);
        }

        [Test]
        public void CreateClan_Publishes_ClanCreated_Event()
        {
            MultiplayerClanCreatedEvent? received = null;
            EventBus.Subscribe<MultiplayerClanCreatedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.CreateClan("Spore Pack", "SPOR");

            Assert.IsNotNull(received);
            Assert.AreEqual("SPOR", received.Value.Tag);
            Assert.AreEqual(mgr.LocalPlayerId, received.Value.LeaderPlayerId);
        }

        [Test]
        public void CreateClan_When_Already_In_Clan_Returns_False()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsTrue(mgr.CreateClan("Spore Pack", "SPOR"));
            Assert.IsFalse(mgr.CreateClan("Second Pack", "SECO"));
        }

        [Test]
        public void CreateParty_Valid_Name_And_Tag_Creates_Local_Party()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            bool created = mgr.CreateParty("Tide Hunters", "tide");

            Assert.IsTrue(created);
            Assert.IsNotNull(mgr.LocalParty);
            Assert.AreEqual("TIDE", mgr.LocalParty.Tag);
            Assert.AreEqual(mgr.LocalPlayerId, mgr.LocalParty.LeaderPlayerId);
            Assert.AreEqual(mgr.LocalPartyId, mgr.LocalClanId);
        }

        [Test]
        public void LeaveSession_Clears_Local_Clan()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.CreateClan("Spore Pack", "SPOR");

            mgr.LeaveSession();

            Assert.IsNull(mgr.LocalClan);
            Assert.AreEqual(0, mgr.Clans.Count);
        }

        // ---- lobby readiness / game start ----

        [Test]
        public void Host_Cannot_Start_Until_Remote_Players_Are_Ready()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "P1");

            Assert.IsFalse(mgr.CanStartGame);

            mgr.ApplyPlayerReady(1, true);

            Assert.IsTrue(mgr.CanStartGame);
        }

        [Test]
        public void Client_SetLocalReady_Updates_Readiness()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("ABCD12", localPlayerId: 1);

            Assert.IsFalse(mgr.LocalReady);
            Assert.IsTrue(mgr.SetLocalReady(true));
            Assert.IsTrue(mgr.LocalReady);
        }

        [Test]
        public void ApplyGameStarted_Publishes_Event()
        {
            MultiplayerGameStartedEvent? received = null;
            EventBus.Subscribe<MultiplayerGameStartedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            string code = mgr.CreateSession();

            Assert.IsTrue(mgr.ApplyGameStarted());
            Assert.IsNotNull(received);
            Assert.AreEqual(code, received.Value.SessionCode);
        }

        [Test]
        public void ApplyNetworkQuality_Publishes_Reconnect_Diagnostics()
        {
            MultiplayerNetworkQualityChangedEvent? received = null;
            EventBus.Subscribe<MultiplayerNetworkQualityChangedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("ABCD12", localPlayerId: 1);
            var snapshot = new NetworkQualitySnapshot
            {
                PingMs = 80f,
                RttTrendMs = 120f,
                PacketLossRatio = 0.2f,
                LastAuthoritativeSnapshotAgeSeconds = 3.5f,
                ReconnectAttempts = 1,
                Quality = NetworkConnectionQuality.Reconnecting
            };

            Assert.IsTrue(mgr.ApplyNetworkQuality(snapshot));

            Assert.IsNotNull(received);
            Assert.AreEqual(NetworkConnectionQuality.Reconnecting, received.Value.Quality);
            Assert.AreEqual(0.2f, mgr.NetworkQuality.PacketLossRatio, 0.0001f);
        }

        [Test]
        public void BeginAndCompleteReconnect_PreserveSessionIdentity()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("ABCD12", localPlayerId: 1);
            mgr.AddRemotePlayer(2, "Friend");
            Assert.AreEqual(SessionState.Connected, mgr.State);

            Assert.IsTrue(mgr.BeginReconnectAttempt());

            Assert.AreEqual(SessionState.Disconnected, mgr.State);
            Assert.AreEqual("ABCD12", mgr.SessionCode);
            Assert.AreEqual(2, mgr.PlayerCount);
            Assert.AreEqual(NetworkConnectionQuality.Reconnecting, mgr.NetworkQuality.Quality);

            Assert.IsTrue(mgr.CompleteReconnect());

            Assert.AreEqual(SessionState.Connected, mgr.State);
            Assert.AreEqual("ABCD12", mgr.SessionCode);
            Assert.AreEqual(2, mgr.PlayerCount);
            Assert.AreEqual(NetworkConnectionQuality.Healthy, mgr.NetworkQuality.Quality);
        }

        [Test]
        public void ApplyChatMessage_Stores_ServerFiltered_Message()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsTrue(mgr.ApplyChatMessage(mgr.LocalPlayerId, " hello   reef "));

            Assert.AreEqual(1, mgr.ChatMessages.Count);
            Assert.AreEqual("hello reef", mgr.ChatMessages[0].Message);
            Assert.AreEqual("You", mgr.ChatMessages[0].SenderName);
            Assert.IsTrue(mgr.ChatMessages[0].IsLocal);
        }

        [Test]
        public void ApplyChatMessage_Publishes_Event()
        {
            MultiplayerChatMessageEvent? received = null;
            EventBus.Subscribe<MultiplayerChatMessageEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "Alice");
            mgr.ApplyChatMessage(1, "ready when you are");

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Value.FromPlayerId);
            Assert.AreEqual("Alice", received.Value.SenderName);
            Assert.AreEqual("ready when you are", received.Value.Message);
            Assert.IsFalse(received.Value.IsLocal);
        }

        [Test]
        public void TryNormalizeChatMessage_Rejects_Invalid_Text()
        {
            Assert.IsFalse(MultiplayerSessionManager.TryNormalizeChatMessage("   ", out _));
            Assert.IsFalse(MultiplayerSessionManager.TryNormalizeChatMessage(new string('x', MultiplayerSessionManager.ChatMessageMaxLength + 1), out _));
            Assert.IsFalse(MultiplayerSessionManager.TryNormalizeChatMessage("bad\u0001message", out _));
        }

        [Test]
        public void ApplySessionMap_Stores_Server_Map()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            bool eventFired = false;
            var map = new SessionMapDefinition(
                SessionMapDefinition.CreatureMode,
                SessionMapDefinition.CurrentVersion,
                2468,
                48f,
                72,
                8f,
                0.055f,
                new[] { new SessionMapLandmark("Cover", 2f, 3f, 4f, 1.5f, 3f) });
            mgr.OnSessionMapChanged += received =>
            {
                eventFired = true;
                Assert.AreEqual(map.Seed, received.Seed);
            };

            Assert.IsTrue(mgr.ApplySessionMap(map));

            Assert.IsTrue(eventFired);
            Assert.IsTrue(mgr.TryGetSessionMap(out var stored));
            Assert.AreEqual(2468, stored.Seed);
            Assert.AreEqual(1, stored.Landmarks.Length);
        }

        [Test]
        public void SetLocalLobby_Valid_Lobby_Updates_Local_State()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsTrue(mgr.SetLocalLobby("cell-0_1"));

            Assert.AreEqual("cell-0_1", mgr.LocalLobbyId);
            Assert.AreEqual("cell-0_1", mgr.GetPlayerLobby(mgr.LocalPlayerId));
        }

        [Test]
        public void ApplyPlayerLobby_Invalid_Lobby_Returns_False()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();

            Assert.IsFalse(mgr.ApplyPlayerLobby(mgr.LocalPlayerId, "../cell"));
            Assert.AreEqual(MultiplayerSessionManager.DefaultLobbyId, mgr.LocalLobbyId);
        }

        [Test]
        public void ApplyPlayerLobby_Publishes_Event()
        {
            MultiplayerPlayerLobbyChangedEvent? received = null;
            EventBus.Subscribe<MultiplayerPlayerLobbyChangedEvent>(e => received = e);

            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.ApplyPlayerLobby(mgr.LocalPlayerId, "cell");

            Assert.IsNotNull(received);
            Assert.AreEqual(mgr.LocalPlayerId, received.Value.PlayerId);
            Assert.AreEqual("cell", received.Value.LobbyId);
        }

        // ---- host authority ----

        [Test]
        public void HostAuthority_IsHost_True_Allows_All()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            var auth = new HostAuthorityController(mgr);

            Assert.IsTrue(auth.CanSpawnFood());
            Assert.IsTrue(auth.CanSpawnOrganism());
            Assert.IsTrue(auth.CanSpawnPickup());
            Assert.IsTrue(auth.CanFireEnvironmentEvent());
            Assert.IsTrue(auth.CanSpawnLeviathan());
        }

        [Test]
        public void HostAuthority_NotHost_Denies_All()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.JoinSession("BBBB22", localPlayerId: 1); // player 1, host is 0
            var auth = new HostAuthorityController(mgr);

            Assert.IsFalse(auth.CanSpawnFood());
            Assert.IsFalse(auth.CanSpawnOrganism());
            Assert.IsFalse(auth.CanSpawnPickup());
            Assert.IsFalse(auth.CanFireEnvironmentEvent());
            Assert.IsFalse(auth.CanSpawnLeviathan());
        }

        [Test]
        public void HostAuthority_Offline_Session_Allows_All()
        {
            var mgr  = new MultiplayerSessionManager(); // never started
            var auth = new HostAuthorityController(mgr);

            Assert.IsTrue(auth.CanSpawnFood());
            Assert.IsTrue(auth.CanSpawnOrganism());
            Assert.IsTrue(auth.CanSpawnPickup());
            Assert.IsTrue(auth.CanFireEnvironmentEvent());
            Assert.IsTrue(auth.CanSpawnLeviathan());
        }

        // ---- GenomeChecksum ----

        [Test]
        public void GenomeChecksum_Null_Returns_Zero()
        {
            Assert.AreEqual(0u, GenomeChecksum.Compute(null));
        }

        [Test]
        public void GenomeChecksum_Deterministic_Same_Genome()
        {
            var g = new Genome { Speed = 3f, Size = 2f, Attack = 1f };
            uint a = GenomeChecksum.Compute(g);
            uint b = GenomeChecksum.Compute(g);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void GenomeChecksum_Different_Genomes_Produce_Different_Checksums()
        {
            var g1 = new Genome { Speed = 1f, Size = 1f };
            var g2 = new Genome { Speed = 5f, Size = 1f };
            Assert.AreNotEqual(GenomeChecksum.Compute(g1), GenomeChecksum.Compute(g2));
        }

        // ---- NullNetworkTransport ----

        [Test]
        public void NullTransport_Does_Not_Throw()
        {
            var t = new NullNetworkTransport();
            Assert.IsFalse(t.IsConnected);
            Assert.DoesNotThrow(() => t.Send(0, new byte[4]));
            Assert.DoesNotThrow(() => t.Broadcast(new byte[4]));
            Assert.IsNull(t.TryReceive(out _));
            Assert.DoesNotThrow(() => t.Disconnect());
        }
    }
}
