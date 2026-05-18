using System;
using System.IO;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for the multiplayer layer:
    ///   <see cref="MultiplayerSessionManager"/> lifecycle and host migration,
    ///   <see cref="HostAuthorityController"/> offline/host/client authority,
    ///   <see cref="NetworkSerializer"/> binary round-trips,
    ///   <see cref="GenomeChecksum"/> determinism.
    /// </summary>
    public class MultiplayerTests
    {
        // ════════════════════════════════════════════════════════
        // MultiplayerSessionManager — session lifecycle
        // ════════════════════════════════════════════════════════

        [Test]
        public void CreateSession_SetsStateToHosting()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.AreEqual(SessionState.Hosting, mgr.State);
        }

        [Test]
        public void CreateSession_GeneratesSixCharCode()
        {
            var mgr  = new MultiplayerSessionManager();
            string code = mgr.CreateSession();
            Assert.AreEqual(MultiplayerSessionManager.SessionCodeLength, code.Length,
                "Session code must be exactly 6 characters");
        }

        [Test]
        public void CreateSession_CodeContainsNoAmbiguousChars()
        {
            // Characters 0, 1, I, O omitted to avoid visual confusion in code entry
            var mgr = new MultiplayerSessionManager();
            for (int i = 0; i < 50; i++)
            {
                string code = mgr.CreateSession();
                Assert.IsFalse(code.Contains("0"), "Code must not contain '0'");
                Assert.IsFalse(code.Contains("1"), "Code must not contain '1'");
                Assert.IsFalse(code.Contains("I"), "Code must not contain 'I'");
                Assert.IsFalse(code.Contains("O"), "Code must not contain 'O'");
            }
        }

        [Test]
        public void CreateSession_LocalPlayerIsHost()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.IsTrue(mgr.IsHost);
            Assert.AreEqual(0, mgr.LocalPlayerId);
            Assert.AreEqual(0, mgr.HostPlayerId);
        }

        [Test]
        public void CreateSession_PlayerCountIsOne()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            Assert.AreEqual(1, mgr.PlayerCount);
        }

        [Test]
        public void JoinSession_ValidCode_ReturnsTrue()
        {
            var host   = new MultiplayerSessionManager();
            string code = host.CreateSession();

            var client = new MultiplayerSessionManager();
            bool joined = client.JoinSession(code, 1);

            Assert.IsTrue(joined);
            Assert.AreEqual(SessionState.Connected, client.State);
        }

        [Test]
        public void JoinSession_EmptyCode_ReturnsFalse()
        {
            var client = new MultiplayerSessionManager();
            Assert.IsFalse(client.JoinSession("", 1));
        }

        [Test]
        public void JoinSession_NullCode_ReturnsFalse()
        {
            var client = new MultiplayerSessionManager();
            Assert.IsFalse(client.JoinSession(null, 1));
        }

        [Test]
        public void JoinSession_WrongLengthCode_ReturnsFalse()
        {
            var client = new MultiplayerSessionManager();
            Assert.IsFalse(client.JoinSession("ABC", 1));   // too short
            Assert.IsFalse(client.JoinSession("ABCDEFGH", 1)); // too long
        }

        [Test]
        public void JoinSession_NormalisesCodeToUppercase()
        {
            var host   = new MultiplayerSessionManager();
            string code = host.CreateSession(); // always uppercase
            var client = new MultiplayerSessionManager();
            bool ok = client.JoinSession(code.ToLower(), 1);
            Assert.IsTrue(ok, "JoinSession should accept lowercase input and normalise it");
        }

        [Test]
        public void AddRemotePlayer_IncreasesPlayerCount()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession(); // count = 1
            mgr.AddRemotePlayer(1, "Bob");
            Assert.AreEqual(2, mgr.PlayerCount);
        }

        [Test]
        public void AddRemotePlayer_AllowsMoreThanFourPlayers()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "P1");
            mgr.AddRemotePlayer(2, "P2");
            mgr.AddRemotePlayer(3, "P3");
            bool added = mgr.AddRemotePlayer(4, "P4");

            Assert.IsTrue(added);
            Assert.AreEqual(5, mgr.PlayerCount);
        }

        [Test]
        public void AddRemotePlayer_WhenOffline_ReturnsFalse()
        {
            var mgr = new MultiplayerSessionManager();
            bool added = mgr.AddRemotePlayer(1, "Bob");
            Assert.IsFalse(added);
        }

        [Test]
        public void AddRemotePlayer_FiresOnPlayerJoined()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            RemotePlayer received = null;
            mgr.OnPlayerJoined += p => received = p;
            mgr.AddRemotePlayer(1, "Alice");
            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.PlayerId);
            Assert.AreEqual("Alice", received.DisplayName);
        }

        // ── Host migration ──

        [Test]
        public void RemovePlayer_HostLeaves_PromotesNextPlayer()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();           // local = host (0)
            mgr.AddRemotePlayer(1, "P1");
            mgr.AddRemotePlayer(2, "P2");

            int newHost = -1;
            mgr.OnHostMigrated += id => newHost = id;

            mgr.RemovePlayer(0); // host leaves
            Assert.AreEqual(1, newHost, "First remaining player should become host");
        }

        [Test]
        public void RemovePlayer_LastPlayer_GoesOffline()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.RemovePlayer(0); // only player leaves
            Assert.AreEqual(SessionState.Offline, mgr.State);
        }

        [Test]
        public void RemovePlayer_NonHost_NoMigration()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "P1");

            bool migrated = false;
            mgr.OnHostMigrated += _ => migrated = true;
            mgr.RemovePlayer(1); // non-host leaves

            Assert.IsFalse(migrated);
            Assert.AreEqual(SessionState.Hosting, mgr.State);
        }

        [Test]
        public void LeaveSession_ResetsAllState()
        {
            var mgr = new MultiplayerSessionManager();
            mgr.CreateSession();
            mgr.AddRemotePlayer(1, "P1");
            mgr.LeaveSession();

            Assert.AreEqual(SessionState.Offline, mgr.State);
            Assert.AreEqual(0, mgr.PlayerCount);
            Assert.IsNull(mgr.SessionCode);
            Assert.IsFalse(mgr.IsHost);
        }

        // ════════════════════════════════════════════════════════
        // HostAuthorityController
        // ════════════════════════════════════════════════════════

        [Test]
        public void Authority_Offline_AllChecksTrue()
        {
            var mgr  = new MultiplayerSessionManager(); // Offline
            var auth = new HostAuthorityController(mgr);
            Assert.IsTrue(auth.CanSpawnFood());
            Assert.IsTrue(auth.CanSpawnOrganism());
            Assert.IsTrue(auth.CanSpawnPickup());
            Assert.IsTrue(auth.CanFireEnvironmentEvent());
            Assert.IsTrue(auth.CanSpawnLeviathan());
        }

        [Test]
        public void Authority_Host_AllChecksTrue()
        {
            var mgr  = new MultiplayerSessionManager();
            mgr.CreateSession(); // IsHost = true
            var auth = new HostAuthorityController(mgr);
            Assert.IsTrue(auth.CanSpawnFood());
            Assert.IsTrue(auth.CanSpawnOrganism());
            Assert.IsTrue(auth.CanSpawnPickup());
            Assert.IsTrue(auth.CanFireEnvironmentEvent());
            Assert.IsTrue(auth.CanSpawnLeviathan());
        }

        [Test]
        public void Authority_Client_AllChecksFalse()
        {
            var host   = new MultiplayerSessionManager();
            string code = host.CreateSession();

            var client = new MultiplayerSessionManager();
            client.JoinSession(code, 1); // IsHost = false

            var auth = new HostAuthorityController(client);
            Assert.IsFalse(auth.CanSpawnFood());
            Assert.IsFalse(auth.CanSpawnOrganism());
            Assert.IsFalse(auth.CanSpawnPickup());
            Assert.IsFalse(auth.CanFireEnvironmentEvent());
            Assert.IsFalse(auth.CanSpawnLeviathan());
        }

        [Test]
        public void Authority_NullSession_AllChecksTrue()
        {
            var auth = new HostAuthorityController(null);
            Assert.IsTrue(auth.CanSpawnFood(),           "null session = offline = authoritative");
            Assert.IsTrue(auth.CanSpawnOrganism());
            Assert.IsTrue(auth.CanSpawnPickup());
            Assert.IsTrue(auth.CanFireEnvironmentEvent());
            Assert.IsTrue(auth.CanSpawnLeviathan());
        }

        // ════════════════════════════════════════════════════════
        // NetworkSerializer — binary round-trips
        // ════════════════════════════════════════════════════════

        [Test]
        public void Serialize_RemoteInputPacket_RoundTrip()
        {
            var pkt = new RemoteInputPacket
            {
                PlayerId = 2,
                MoveX    = -0.75f,
                MoveY    =  0.5f,
                EatInput = true,
                Mass     = 3.25f,
                Tick     = 1234u,
                HasCreatureInput = true,
                CreatureId = "creature-2",
                CreatureDNAJson = "{\"CreatureId\":\"creature-2\"}",
                CreaturePosX = 9f,
                CreaturePosY = -4f,
                CreatureRotY = 135f,
                CreatureHealth = 22f,
                CreatureMaxHealth = 30f,
                CreatureStamina = 11f,
                CreatureIsAlive = true
            };
            byte[] data = NetworkSerializer.Serialize(pkt);
            Assert.IsNotNull(data);
            Assert.Greater(data.Length, 0);

            var restored = NetworkSerializer.DeserializeInput(data);
            Assert.AreEqual(pkt.PlayerId, restored.PlayerId);
            Assert.AreEqual(pkt.MoveX,    restored.MoveX,    0.0001f);
            Assert.AreEqual(pkt.MoveY,    restored.MoveY,    0.0001f);
            Assert.AreEqual(pkt.EatInput, restored.EatInput);
            Assert.AreEqual(pkt.Mass,     restored.Mass,     0.0001f);
            Assert.AreEqual(pkt.Tick,     restored.Tick);
            Assert.AreEqual(pkt.HasCreatureInput, restored.HasCreatureInput);
            Assert.AreEqual(pkt.CreatureId, restored.CreatureId);
            Assert.AreEqual(pkt.CreatureDNAJson, restored.CreatureDNAJson);
            Assert.AreEqual(pkt.CreaturePosX, restored.CreaturePosX, 0.0001f);
            Assert.AreEqual(pkt.CreaturePosY, restored.CreaturePosY, 0.0001f);
            Assert.AreEqual(pkt.CreatureRotY, restored.CreatureRotY, 0.0001f);
            Assert.AreEqual(pkt.CreatureHealth, restored.CreatureHealth, 0.0001f);
            Assert.AreEqual(pkt.CreatureMaxHealth, restored.CreatureMaxHealth, 0.0001f);
            Assert.AreEqual(pkt.CreatureStamina, restored.CreatureStamina, 0.0001f);
            Assert.AreEqual(pkt.CreatureIsAlive, restored.CreatureIsAlive);
        }

        [Test]
        public void DeserializeInput_FromTaggedPacketSlice_RoundTrip()
        {
            var pkt = new RemoteInputPacket
            {
                PlayerId = 3,
                MoveX    = 0.25f,
                MoveY    = -1f,
                EatInput = false,
                Mass     = 5.5f,
                Tick     = 77u
            };

            byte[] payload = NetworkSerializer.Serialize(pkt);
            byte[] packet = NetworkTickManager.Prepend(NetworkTickManager.TagInput, payload);
            var restored = NetworkSerializer.DeserializeInput(packet, 1, packet.Length - 1);

            Assert.AreEqual(pkt.PlayerId, restored.PlayerId);
            Assert.AreEqual(pkt.MoveX, restored.MoveX, 0.0001f);
            Assert.AreEqual(pkt.MoveY, restored.MoveY, 0.0001f);
            Assert.AreEqual(pkt.Mass, restored.Mass, 0.0001f);
            Assert.AreEqual(pkt.Tick, restored.Tick);
        }

        [Test]
        public void Serialize_VoiceChatPacket_RoundTrip()
        {
            var pkt = new VoiceChatPacket
            {
                PlayerId = 2,
                Sequence = 42u,
                SampleRate = ProximityVoiceChat.SampleRate,
                Pcm16 = new short[] { short.MinValue, -120, 0, 120, short.MaxValue }
            };

            byte[] data = NetworkSerializer.Serialize(pkt);
            var restored = NetworkSerializer.DeserializeVoice(data);

            Assert.AreEqual(pkt.PlayerId, restored.PlayerId);
            Assert.AreEqual(pkt.Sequence, restored.Sequence);
            Assert.AreEqual(pkt.SampleRate, restored.SampleRate);
            CollectionAssert.AreEqual(pkt.Pcm16, restored.Pcm16);
        }

        [Test]
        public void Serialize_StageTransitionNetState_RoundTrip()
        {
            var pkt = new StageTransitionNetState
            {
                Tick = 44u,
                RequestingPlayerId = 0,
                TargetStage = GameStageType.Creature,
                TransitionId = 9001u
            };

            byte[] data = NetworkSerializer.Serialize(pkt);
            var restored = NetworkSerializer.DeserializeStageTransition(data);

            Assert.AreEqual(pkt.Tick, restored.Tick);
            Assert.AreEqual(pkt.RequestingPlayerId, restored.RequestingPlayerId);
            Assert.AreEqual(pkt.TargetStage, restored.TargetStage);
            Assert.AreEqual(pkt.TransitionId, restored.TransitionId);
        }

        [Test]
        public void DeserializeStageTransition_FromTaggedPacketSlice_RoundTrip()
        {
            var pkt = new StageTransitionNetState
            {
                Tick = 45u,
                RequestingPlayerId = 0,
                TargetStage = GameStageType.Creature
            };

            byte[] payload = NetworkSerializer.Serialize(pkt);
            byte[] packet = NetworkTickManager.Prepend(NetworkTickManager.TagStageTransition, payload);
            var restored = NetworkSerializer.DeserializeStageTransition(packet, 1, packet.Length - 1);

            Assert.AreEqual(pkt.Tick, restored.Tick);
            Assert.AreEqual(pkt.RequestingPlayerId, restored.RequestingPlayerId);
            Assert.AreEqual(pkt.TargetStage, restored.TargetStage);
        }

        [Test]
        public void Serialize_SessionNetState_RoundTrip()
        {
            var state = new SessionNetState
            {
                Tick = 999u,
                Players = new[]
                {
                    new PlayerNetState { PlayerId = 0, PosX = 1.5f, PosY = -2f,
                        VelX = 0.1f, VelY = 0.2f, GenomeChecksum = 0xDEAD, Energy = 80f,
                        Mass = 3.5f, IsAlive = true },
                    new PlayerNetState { PlayerId = 1, PosX = -5f,  PosY = 10f,
                        VelX = 0f,   VelY = 0f,   GenomeChecksum = 0xBEEF, Energy = 40f,
                        Mass = 2f,   IsAlive = true }
                },
                Organisms = new[]
                {
                    new OrganismNetState { OrgId = default, PosX = 3f, PosY = 0f,
                        GenomeChecksum = 111u, SpeciesId = 2, IsAlive = true }
                },
                Foods = new[]
                {
                    new FoodNetState { FoodId = 7, PosX = -1f, PosY = 4f,
                        FoodType = FoodType.Plant, Exists = true }
                },
                PartPickups = new[]
                {
                    new PartPickupNetState { PickupId = 5, PosX = 8f, PosY = -6f,
                        PartType = CellPartType.Spike, Exists = true }
                },
                CreaturePlayers = new[]
                {
                    new CreaturePlayerNetState { PlayerId = 0, CreatureId = "player-roob",
                        CreatureDNAJson = "{\"CreatureId\":\"player-roob\",\"CreatureName\":\"Roob\"}",
                        PosX = 2f, PosY = 3f, RotY = 45f, Health = 21f, MaxHealth = 30f,
                        Stamina = 14f, IsAlive = true }
                },
                CreatureRivals = new[]
                {
                    new CreatureRivalNetState { CreatureId = "rival-alpha",
                        Archetype = 2, IsAlpha = true, PosX = -4f, PosY = 9f, RotY = 180f,
                        Health = 18f, MaxHealth = 25f, Exists = true, IsAlive = true }
                },
                CreatureObjective = new CreatureObjectiveNetState
                {
                    HasObjective = true,
                    ObjectiveType = 1,
                    Progress = 2,
                    Target = 3,
                    ActiveRivals = 4,
                    DesiredActiveRivals = 5,
                    ObjectivesCompleted = 1,
                    HoldSeconds = 7.5f,
                    IsComplete = false
                },
                CreatureWorldProgression = new CreatureWorldProgressionNetState
                {
                    HasProgression = true,
                    DNA = 72,
                    Meals = 3,
                    Allies = 1,
                    Defeats = 2,
                    Discoveries = 4,
                    MigrationsCompleted = 1,
                    IsAtNest = true,
                    HasPackMate = false,
                    CanEditAtNest = true,
                    CanMigrate = true,
                    NearestNestDistance = 0f,
                    NearestMigrationDistance = 12.5f
                }
            };

            byte[] data = NetworkSerializer.Serialize(state);
            Assert.IsNotNull(data);

            var r = NetworkSerializer.DeserializeSession(data);
            Assert.AreEqual(state.Tick, r.Tick);

            Assert.AreEqual(2, r.Players.Length);
            Assert.AreEqual(0,     r.Players[0].PlayerId);
            Assert.AreEqual(1.5f,  r.Players[0].PosX,    0.0001f);
            Assert.AreEqual(-2f,   r.Players[0].PosY,    0.0001f);
            Assert.IsTrue(r.Players[0].IsAlive);
            Assert.AreEqual(1,     r.Players[1].PlayerId);
            Assert.IsTrue(r.Players[1].IsAlive); // set in fixture

            Assert.AreEqual(1, r.Organisms.Length);
            Assert.AreEqual(state.Organisms[0].OrgId, r.Organisms[0].OrgId);
            Assert.AreEqual(2, r.Organisms[0].SpeciesId);

            Assert.AreEqual(1, r.Foods.Length);
            Assert.AreEqual(7,           r.Foods[0].FoodId);
            Assert.AreEqual(FoodType.Plant, r.Foods[0].FoodType);
            Assert.IsTrue(r.Foods[0].Exists);

            Assert.AreEqual(1, r.PartPickups.Length);
            Assert.AreEqual(5, r.PartPickups[0].PickupId);
            Assert.AreEqual(CellPartType.Spike, r.PartPickups[0].PartType);
            Assert.IsTrue(r.PartPickups[0].Exists);

            Assert.AreEqual(1, r.CreaturePlayers.Length);
            Assert.AreEqual("player-roob", r.CreaturePlayers[0].CreatureId);
            Assert.That(r.CreaturePlayers[0].CreatureDNAJson, Does.Contain("player-roob"));
            Assert.AreEqual(14f, r.CreaturePlayers[0].Stamina, 0.0001f);

            Assert.AreEqual(1, r.CreatureRivals.Length);
            Assert.AreEqual("rival-alpha", r.CreatureRivals[0].CreatureId);
            Assert.IsTrue(r.CreatureRivals[0].IsAlpha);
            Assert.AreEqual(2, r.CreatureRivals[0].Archetype);

            Assert.IsTrue(r.CreatureObjective.HasObjective);
            Assert.AreEqual(2, r.CreatureObjective.Progress);
            Assert.AreEqual(7.5f, r.CreatureObjective.HoldSeconds, 0.0001f);
            Assert.IsTrue(r.CreatureWorldProgression.HasProgression);
            Assert.AreEqual(72, r.CreatureWorldProgression.DNA);
            Assert.AreEqual(3, r.CreatureWorldProgression.Meals);
            Assert.IsTrue(r.CreatureWorldProgression.CanMigrate);
            Assert.AreEqual(12.5f, r.CreatureWorldProgression.NearestMigrationDistance, 0.0001f);
        }

        [Test]
        public void Serialize_Heartbeat_And_RecoveryRequest_RoundTrip()
        {
            var heartbeat = new NetworkHeartbeatPacket
            {
                PlayerId = 3,
                Sequence = 12u,
                SentAtSeconds = 42.5d,
                IsAck = true
            };
            byte[] heartbeatPacket = NetworkTickManager.Prepend(
                NetworkTickManager.TagHeartbeat,
                NetworkSerializer.Serialize(heartbeat));
            var restoredHeartbeat = NetworkSerializer.DeserializeHeartbeat(
                heartbeatPacket,
                1,
                heartbeatPacket.Length - 1);

            Assert.AreEqual(heartbeat.PlayerId, restoredHeartbeat.PlayerId);
            Assert.AreEqual(heartbeat.Sequence, restoredHeartbeat.Sequence);
            Assert.AreEqual(heartbeat.SentAtSeconds, restoredHeartbeat.SentAtSeconds);
            Assert.IsTrue(restoredHeartbeat.IsAck);

            var recovery = new NetworkRecoveryRequestPacket
            {
                PlayerId = 4,
                LastReceivedTick = 88u,
                CurrentStage = GameStageType.Creature,
                ReconnectAttempt = 2
            };
            byte[] recoveryPacket = NetworkTickManager.Prepend(
                NetworkTickManager.TagRecoveryRequest,
                NetworkSerializer.Serialize(recovery));
            var restoredRecovery = NetworkSerializer.DeserializeRecoveryRequest(
                recoveryPacket,
                1,
                recoveryPacket.Length - 1);

            Assert.AreEqual(recovery.PlayerId, restoredRecovery.PlayerId);
            Assert.AreEqual(recovery.LastReceivedTick, restoredRecovery.LastReceivedTick);
            Assert.AreEqual(recovery.CurrentStage, restoredRecovery.CurrentStage);
            Assert.AreEqual(recovery.ReconnectAttempt, restoredRecovery.ReconnectAttempt);
        }

        [Test]
        public void DeserializeSession_FromTaggedPacketSlice_RoundTrip()
        {
            var state = new SessionNetState
            {
                Tick = 123u,
                Players = new[]
                {
                    new PlayerNetState { PlayerId = 1, PosX = 2f, PosY = 3f, IsAlive = true }
                },
                Organisms = Array.Empty<OrganismNetState>(),
                Foods = Array.Empty<FoodNetState>(),
                PartPickups = Array.Empty<PartPickupNetState>()
            };

            byte[] payload = NetworkSerializer.Serialize(state);
            byte[] packet = NetworkTickManager.Prepend(NetworkTickManager.TagSession, payload);
            var restored = NetworkSerializer.DeserializeSession(packet, 1, packet.Length - 1);

            Assert.AreEqual(state.Tick, restored.Tick);
            Assert.AreEqual(1, restored.Players.Length);
            Assert.AreEqual(1, restored.Players[0].PlayerId);
            Assert.AreEqual(2f, restored.Players[0].PosX, 0.0001f);
        }

        [Test]
        public void Serialize_SessionNetState_EmptyArrays_RoundTrip()
        {
            var state = new SessionNetState
            {
                Tick      = 1u,
                Players   = Array.Empty<PlayerNetState>(),
                Organisms = Array.Empty<OrganismNetState>(),
                Foods     = Array.Empty<FoodNetState>(),
                PartPickups = Array.Empty<PartPickupNetState>()
            };
            byte[] data = NetworkSerializer.Serialize(state);
            var r = NetworkSerializer.DeserializeSession(data);
            Assert.AreEqual(1u, r.Tick);
            Assert.AreEqual(0, r.Players.Length);
            Assert.AreEqual(0, r.Organisms.Length);
            Assert.AreEqual(0, r.Foods.Length);
            Assert.AreEqual(0, r.PartPickups.Length);
        }

        [Test]
        public void Serialize_SessionNetState_NullArrays_RoundTrip()
        {
            var state = new SessionNetState { Tick = 2u }; // arrays null
            byte[] data = NetworkSerializer.Serialize(state);
            var r = NetworkSerializer.DeserializeSession(data);
            Assert.AreEqual(0, r.Players.Length);
            Assert.AreEqual(0, r.PartPickups.Length);
        }

        [Test]
        public void DeserializeInput_EmptyData_Throws()
        {
            Assert.Throws<InvalidDataException>(
                () => NetworkSerializer.DeserializeInput(Array.Empty<byte>()));
        }

        [Test]
        public void DeserializeSession_EmptyData_Throws()
        {
            Assert.Throws<InvalidDataException>(
                () => NetworkSerializer.DeserializeSession(Array.Empty<byte>()));
        }

        [Test]
        public void DeserializeVoice_EmptyData_Throws()
        {
            Assert.Throws<InvalidDataException>(
                () => NetworkSerializer.DeserializeVoice(Array.Empty<byte>()));
        }

        [Test]
        public void Serialize_AllFoodTypes_PreserveType()
        {
            foreach (FoodType ft in Enum.GetValues(typeof(FoodType)))
            {
                var state = new SessionNetState
                {
                    Tick      = 0u,
                    Players   = Array.Empty<PlayerNetState>(),
                    Organisms = Array.Empty<OrganismNetState>(),
                    Foods     = new[] { new FoodNetState { FoodId = 1, FoodType = ft, Exists = true } }
                };
                byte[] data = NetworkSerializer.Serialize(state);
                var r = NetworkSerializer.DeserializeSession(data);
                Assert.AreEqual(ft, r.Foods[0].FoodType,
                    $"FoodType {ft} not preserved across serialization");
            }
        }

        // ════════════════════════════════════════════════════════
        // GenomeChecksum
        // ════════════════════════════════════════════════════════

        [Test]
        public void GenomeChecksum_NullGenome_ReturnsZero()
        {
            Assert.AreEqual(0u, GenomeChecksum.Compute(null));
        }

        [Test]
        public void GenomeChecksum_SameGenome_SameChecksum()
        {
            var g = new Genome { Speed = 5f, Attack = 3f, Digestion = 4f };
            uint a = GenomeChecksum.Compute(g);
            uint b = GenomeChecksum.Compute(g);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void GenomeChecksum_DifferentGenomes_DifferentChecksums()
        {
            var g1 = new Genome { Speed = 5f, Attack = 3f };
            var g2 = new Genome { Speed = 5f, Attack = 7f };
            Assert.AreNotEqual(GenomeChecksum.Compute(g1), GenomeChecksum.Compute(g2));
        }

        [Test]
        public void GenomeChecksum_TraitsAffectChecksum()
        {
            var g1 = new Genome { Speed = 3f, Traits = GeneticTraits.None };
            var g2 = new Genome { Speed = 3f, Traits = GeneticTraits.Electric };
            Assert.AreNotEqual(GenomeChecksum.Compute(g1), GenomeChecksum.Compute(g2));
        }

        // ════════════════════════════════════════════════════════
        // NullNetworkTransport (safety / no-op)
        // ════════════════════════════════════════════════════════

        [Test]
        public void NullTransport_IsNotConnected()
        {
            var t = new NullNetworkTransport();
            Assert.IsFalse(t.IsConnected);
        }

        [Test]
        public void NullTransport_TryReceive_ReturnsNull()
        {
            var t = new NullNetworkTransport();
            byte[] data = t.TryReceive(out int from);
            Assert.IsNull(data);
            Assert.AreEqual(-1, from);
        }
    }
}
