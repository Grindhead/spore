using System;
using System.Reflection;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Multiplayer;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for NetworkTickManager helper methods and RemotePlayerCell colour palette.
    /// MonoBehaviour-dependent tests (Update loop, FindObjects) require PlayMode.
    /// </summary>
    public class NetworkTickManagerTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        // ---- Prepend / StripTag ----

        [Test]
        public void Prepend_Adds_Tag_Byte_At_Front()
        {
            var payload = new byte[] { 0xAA, 0xBB };
            var packet  = NetworkTickManager.Prepend(NetworkTickManager.TagSession, payload);
            Assert.AreEqual(NetworkTickManager.TagSession, packet[0]);
            Assert.AreEqual(0xAA, packet[1]);
            Assert.AreEqual(0xBB, packet[2]);
        }

        [Test]
        public void Prepend_Length_Is_Payload_Plus_One()
        {
            var payload = new byte[10];
            var packet  = NetworkTickManager.Prepend(NetworkTickManager.TagInput, payload);
            Assert.AreEqual(11, packet.Length);
        }

        [Test]
        public void StripTag_Returns_Payload_Without_First_Byte()
        {
            var packet  = new byte[] { NetworkTickManager.TagSession, 0x01, 0x02, 0x03 };
            var payload = NetworkTickManager.StripTag(packet);
            Assert.AreEqual(3, payload.Length);
            Assert.AreEqual(0x01, payload[0]);
            Assert.AreEqual(0x03, payload[2]);
        }

        [Test]
        public void StripTag_SingleByte_Packet_Returns_Empty()
        {
            var result = NetworkTickManager.StripTag(new byte[] { 0x01 });
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void StripTag_Null_Returns_Empty()
        {
            var result = NetworkTickManager.StripTag(null);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void GetTag_Returns_First_Byte()
        {
            Assert.AreEqual(NetworkTickManager.TagSession, NetworkTickManager.GetTag(new byte[] { 0x01, 0xFF }));
            Assert.AreEqual(NetworkTickManager.TagInput,   NetworkTickManager.GetTag(new byte[] { 0x02, 0x00 }));
        }

        [Test]
        public void GetTag_Null_Returns_Zero()
        {
            Assert.AreEqual(0, NetworkTickManager.GetTag(null));
        }

        // ---- Prepend / StripTag round-trip ----

        [Test]
        public void Prepend_Then_StripTag_Recovers_Payload()
        {
            var original = new byte[] { 10, 20, 30, 40, 50 };
            var packet   = NetworkTickManager.Prepend(NetworkTickManager.TagSession, original);
            var stripped = NetworkTickManager.StripTag(packet);
            CollectionAssert.AreEqual(original, stripped);
        }

        // ---- BroadcastInterval constant ----

        [Test]
        public void BroadcastInterval_Is_Positive_And_At_Most_100ms()
        {
            Assert.Greater(NetworkTickManager.BroadcastInterval, 0f);
            Assert.LessOrEqual(NetworkTickManager.BroadcastInterval, 0.1f);
        }

        // ---- RemotePlayerCell colour palette ----

        [Test]
        public void GetColorForPlayer_Zero_Returns_Green()
        {
            var c = RemotePlayerCell.GetColorForPlayer(0);
            Assert.Greater(c.g, c.r);
            Assert.Greater(c.g, c.b);
        }

        [Test]
        public void GetColorForPlayer_Distinct_For_Players_1_2_3()
        {
            var c1 = RemotePlayerCell.GetColorForPlayer(1);
            var c2 = RemotePlayerCell.GetColorForPlayer(2);
            var c3 = RemotePlayerCell.GetColorForPlayer(3);
            Assert.AreNotEqual(c1, c2);
            Assert.AreNotEqual(c2, c3);
            Assert.AreNotEqual(c1, c3);
        }

        [Test]
        public void GetColorForPlayer_OutOfRange_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() => RemotePlayerCell.GetColorForPlayer(-1));
            Assert.DoesNotThrow(() => RemotePlayerCell.GetColorForPlayer(99));
        }

        [Test]
        public void NormalizeDisplayName_Trims_And_Preserves_Name()
        {
            Assert.AreEqual("Alice", RemotePlayerCell.NormalizeDisplayName(2, "  Alice  "));
        }

        [Test]
        public void NormalizeDisplayName_Empty_Uses_Player_Fallback()
        {
            Assert.AreEqual("Player 7", RemotePlayerCell.NormalizeDisplayName(7, " "));
        }

        [Test]
        public void NormalizeDisplayName_Truncates_Long_Names()
        {
            string name = new string('A', RemotePlayerCell.MaxDisplayNameLength + 5);
            string normalized = RemotePlayerCell.NormalizeDisplayName(1, name);
            Assert.AreEqual(RemotePlayerCell.MaxDisplayNameLength, normalized.Length);
        }

        [Test]
        public void BuildMassRelationLabel_DescribesRemoteSizeAgainstLocalPlayer()
        {
            Assert.AreEqual("Larger", RemotePlayerCell.BuildMassRelationLabel(12f, 10f));
            Assert.AreEqual("Smaller", RemotePlayerCell.BuildMassRelationLabel(8f, 10f));
            Assert.AreEqual("Similar", RemotePlayerCell.BuildMassRelationLabel(10.5f, 10f));
            Assert.AreEqual(string.Empty, RemotePlayerCell.BuildMassRelationLabel(5f, 0f));
        }

        [Test]
        public void BuildMassStatusLine_FormatsCompactGameplayReadout()
        {
            Assert.AreEqual("Mass 12.0 - Larger", RemotePlayerCell.BuildMassStatusLine(12f, 10f, true));
            Assert.AreEqual("Mass 8.0 - Smaller", RemotePlayerCell.BuildMassStatusLine(8f, 10f, true));
            Assert.AreEqual("Mass 4.5", RemotePlayerCell.BuildMassStatusLine(4.5f, -1f, true));
            Assert.AreEqual("Down", RemotePlayerCell.BuildMassStatusLine(4.5f, 10f, false));
        }

        [Test]
        public void BuildMassConditionLabel_FlagsLowAndCriticalRemotePlayers()
        {
            Assert.AreEqual("Critical", RemotePlayerCell.BuildMassConditionLabel(0.8f, true));
            Assert.AreEqual("Low mass", RemotePlayerCell.BuildMassConditionLabel(1.5f, true));
            Assert.AreEqual(string.Empty, RemotePlayerCell.BuildMassConditionLabel(3f, true));
            Assert.AreEqual("Down", RemotePlayerCell.BuildMassConditionLabel(3f, false));
        }

        [Test]
        public void BuildMassStatusLine_CombinesConditionAndMassRelationship()
        {
            Assert.AreEqual("Mass 0.8 - Critical - Smaller", RemotePlayerCell.BuildMassStatusLine(0.8f, 10f, true));
            Assert.AreEqual("Mass 1.5 - Low mass", RemotePlayerCell.BuildMassStatusLine(1.5f, -1f, true));
        }

        [Test]
        public void BuildStatusLabel_CombinesNameWithMassAndRelationship()
        {
            Assert.AreEqual("Alice\nMass 12.0 - Larger", RemotePlayerCell.BuildStatusLabel("Alice", 12f, 10f, true));
            Assert.AreEqual("Player 7\nMass 8.0 - Smaller", RemotePlayerCell.BuildStatusLabel(7, " ", 8f, 10f, true));
        }

        [Test]
        public void RemotePlayerCreature_BuildStatusLabel_CombinesNameHealthAndStamina()
        {
            Assert.AreEqual(
                "Alice\nHP 12/20 - STA 7",
                RemotePlayerCreature.BuildStatusLabel("Alice", 12f, 20f, 7f, true));
            Assert.AreEqual(
                "Bob\nDown",
                RemotePlayerCreature.BuildStatusLabel("Bob", 0f, 20f, 0f, false));
        }

        // ---- packet tag constants are distinct ----

        [Test]
        public void Tag_Constants_Are_Distinct()
        {
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagInput);
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagVoice);
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagStageTransition);
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagHeartbeat);
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagRecoveryRequest);
            Assert.AreNotEqual(NetworkTickManager.TagInput, NetworkTickManager.TagVoice);
            Assert.AreNotEqual(NetworkTickManager.TagInput, NetworkTickManager.TagStageTransition);
            Assert.AreNotEqual(NetworkTickManager.TagInput, NetworkTickManager.TagHeartbeat);
            Assert.AreNotEqual(NetworkTickManager.TagInput, NetworkTickManager.TagRecoveryRequest);
            Assert.AreNotEqual(NetworkTickManager.TagVoice, NetworkTickManager.TagStageTransition);
            Assert.AreNotEqual(NetworkTickManager.TagVoice, NetworkTickManager.TagHeartbeat);
            Assert.AreNotEqual(NetworkTickManager.TagVoice, NetworkTickManager.TagRecoveryRequest);
            Assert.AreNotEqual(NetworkTickManager.TagStageTransition, NetworkTickManager.TagHeartbeat);
            Assert.AreNotEqual(NetworkTickManager.TagStageTransition, NetworkTickManager.TagRecoveryRequest);
            Assert.AreNotEqual(NetworkTickManager.TagHeartbeat, NetworkTickManager.TagRecoveryRequest);
        }

        [Test]
        public void ShouldAcceptRemoteInputTick_AcceptsFirstEqualAndNewerTicks()
        {
            Assert.IsTrue(NetworkTickManager.ShouldAcceptRemoteInputTick(false, 0u, 0u));
            Assert.IsTrue(NetworkTickManager.ShouldAcceptRemoteInputTick(true, 8u, 8u));
            Assert.IsTrue(NetworkTickManager.ShouldAcceptRemoteInputTick(true, 8u, 9u));
        }

        [Test]
        public void ShouldAcceptRemoteInputTick_RejectsOlderTicks()
        {
            Assert.IsFalse(NetworkTickManager.ShouldAcceptRemoteInputTick(true, 8u, 7u));
        }

        [Test]
        public void ShouldApplyStageTransition_AcceptsOnlyHostCommandOnClient()
        {
            Assert.IsTrue(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: false,
                fromPlayerId: 0,
                hostPlayerId: 0,
                currentStage: GameStageType.Cell,
                targetStage: GameStageType.Creature,
                isTransitioning: false));

            Assert.IsFalse(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: true,
                fromPlayerId: 0,
                hostPlayerId: 0,
                currentStage: GameStageType.Cell,
                targetStage: GameStageType.Creature,
                isTransitioning: false));

            Assert.IsFalse(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: false,
                fromPlayerId: 2,
                hostPlayerId: 0,
                currentStage: GameStageType.Cell,
                targetStage: GameStageType.Creature,
                isTransitioning: false));
        }

        [Test]
        public void ShouldApplyStageTransition_RejectsCurrentUnsupportedOrBusyTargets()
        {
            Assert.IsFalse(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: false,
                fromPlayerId: 0,
                hostPlayerId: 0,
                currentStage: GameStageType.Creature,
                targetStage: GameStageType.Creature,
                isTransitioning: false));

            Assert.IsFalse(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: false,
                fromPlayerId: 0,
                hostPlayerId: 0,
                currentStage: GameStageType.Cell,
                targetStage: GameStageType.None,
                isTransitioning: false));

            Assert.IsFalse(NetworkTickManager.ShouldApplyStageTransition(
                isLocalHost: false,
                fromPlayerId: 0,
                hostPlayerId: 0,
                currentStage: GameStageType.Cell,
                targetStage: GameStageType.Creature,
                isTransitioning: true));
        }

        [Test]
        public void ComputeRemoteInputMoveSpeed_UsesCellMassPenalty()
        {
            float baseSpeed = 7.5f;
            float small = NetworkTickManager.ComputeRemoteInputMoveSpeed(baseSpeed, 0.5f);
            float large = NetworkTickManager.ComputeRemoteInputMoveSpeed(baseSpeed, 10f);

            Assert.That(small, Is.EqualTo(baseSpeed).Within(0.001f));
            Assert.Less(large, small);
            Assert.That(large, Is.EqualTo(baseSpeed * PlayerCell.ComputeSizeSpeedPenalty(10f)).Within(0.001f));
        }

        [Test]
        public void NetworkResilienceRules_Classifies_Lag_Reconnect_And_Disconnect()
        {
            Assert.AreEqual(
                NetworkConnectionQuality.Healthy,
                NetworkResilienceRules.ClassifyQuality(true, 40f, 0f, 0.2f, 0));
            Assert.AreEqual(
                NetworkConnectionQuality.Lagging,
                NetworkResilienceRules.ClassifyQuality(true, 220f, 0f, 0.2f, 0));
            Assert.AreEqual(
                NetworkConnectionQuality.Reconnecting,
                NetworkResilienceRules.ClassifyQuality(true, 40f, 0f, 3.5f, 0));
            Assert.AreEqual(
                NetworkConnectionQuality.Disconnected,
                NetworkResilienceRules.ClassifyQuality(false, 40f, 0f, 0.2f, 0));
        }

        [Test]
        public void NetworkResilienceRules_BuildSnapshot_TracksPacketLossAndRecovery()
        {
            var snapshot = NetworkResilienceRules.BuildSnapshot(
                transportConnected: true,
                pingMs: 90f,
                rttTrendMs: 120f,
                packetsSent: 10,
                packetsReceived: 7,
                lastAuthoritativeSnapshotAgeSeconds: 3.2f,
                reconnectAttempts: 1);

            Assert.AreEqual(90f, snapshot.PingMs);
            Assert.AreEqual(0.3f, snapshot.PacketLossRatio, 0.0001f);
            Assert.AreEqual(NetworkConnectionQuality.Reconnecting, snapshot.Quality);
            Assert.IsTrue(NetworkResilienceRules.ShouldRequestRecovery(false, snapshot.Quality, 2f));
            Assert.IsFalse(NetworkResilienceRules.ShouldRequestRecovery(true, snapshot.Quality, 2f));
        }

        [Test]
        public void ProximityVoiceChat_Volume_Fades_By_Distance()
        {
            Assert.AreEqual(1f, ProximityVoiceChat.CalculateVolume(0f));
            Assert.AreEqual(1f, ProximityVoiceChat.CalculateVolume(ProximityVoiceChat.FullVolumeRadius));
            Assert.AreEqual(0f, ProximityVoiceChat.CalculateVolume(ProximityVoiceChat.VoiceRadius));

            float mid = (ProximityVoiceChat.FullVolumeRadius + ProximityVoiceChat.VoiceRadius) * 0.5f;
            Assert.Greater(ProximityVoiceChat.CalculateVolume(mid), 0f);
            Assert.Less(ProximityVoiceChat.CalculateVolume(mid), 1f);
        }

        [Test]
        public void ProximityVoiceChat_RadiusGate_UsesVoiceRadiusBoundary()
        {
            Assert.IsTrue(ProximityVoiceChat.IsWithinVoiceRadius(ProximityVoiceChat.VoiceRadius));
            Assert.IsFalse(ProximityVoiceChat.IsWithinVoiceRadius(ProximityVoiceChat.VoiceRadius + 0.01f));
        }

        // ---- GenomeChecksum integration (confirm stable through serialization) ----

        [Test]
        public void GenomeChecksum_Stable_After_Prepend_StripTag()
        {
            // Simulate: host builds PlayerNetState, embeds checksum; client reads it back.
            var genome = new Genome { Speed = 4f, Size = 3f, Attack = 2f };
            uint checksum = GenomeChecksum.Compute(genome);

            var state = new PlayerNetState
            {
                PlayerId       = 1,
                PosX           = 5f,
                PosY           = -3f,
                GenomeChecksum = checksum,
                Mass           = 2.5f,
                IsAlive        = true
            };

            // Serialize the full session containing this player
            var session = new SessionNetState
            {
                Tick    = 42u,
                Players = new[] { state }
            };
            var payload  = NetworkSerializer.Serialize(session);
            var packet   = NetworkTickManager.Prepend(NetworkTickManager.TagSession, payload);
            var stripped = NetworkTickManager.StripTag(packet);
            var decoded  = NetworkSerializer.DeserializeSession(stripped);

            Assert.AreEqual(checksum, decoded.Players[0].GenomeChecksum);
        }

        [Test]
        public void BuildSessionState_Skips_Destroyed_Food_Cache_Entries()
        {
            var mgrGo = new GameObject("TestTick");
            var foodGo = new GameObject("StaleFood");
            try
            {
                var mgr = mgrGo.AddComponent<NetworkTickManager>();
                var food = foodGo.AddComponent<FoodItem>();
                food.Initialize(FoodType.Plant, 1f);
                food.SetFoodManagerId(12345);

                var buildSessionState = typeof(NetworkTickManager).GetMethod(
                    "BuildSessionState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(buildSessionState);

                var initialState = (SessionNetState)buildSessionState.Invoke(mgr, new object[] { 1u });
                Assert.IsTrue(Array.Exists(initialState.Foods, f => f.FoodId == 12345));

                UnityEngine.Object.DestroyImmediate(foodGo);

                Assert.DoesNotThrow(() =>
                {
                    var staleState = (SessionNetState)buildSessionState.Invoke(mgr, new object[] { 2u });
                    Assert.IsFalse(Array.Exists(staleState.Foods, f => f.FoodId == 12345));
                });
            }
            finally
            {
                if (foodGo != null)
                    UnityEngine.Object.DestroyImmediate(foodGo);
                UnityEngine.Object.DestroyImmediate(mgrGo);
            }
        }

        [Test]
        public void ApplyFoodState_ReconcilesClientFoodToHostSnapshot()
        {
            var mgrGo = new GameObject("TestTick");
            var foodManagerGo = new GameObject("FoodManager_Test");
            try
            {
                var tickManager = mgrGo.AddComponent<NetworkTickManager>();
                var foodManager = foodManagerGo.AddComponent<FoodManager>();
                int staleLocalId = foodManager.AddFoodAt(Vector3.zero, FoodType.Plant, 5f);
                Assert.IsTrue(foodManager.HasFood(staleLocalId));

                var applyFoodState = typeof(NetworkTickManager).GetMethod(
                    "ApplyFoodState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(applyFoodState);

                var hostFoods = new[]
                {
                    new FoodNetState
                    {
                        FoodId = 77,
                        PosX = 4f,
                        PosY = -2f,
                        FoodType = FoodType.Meat,
                        Exists = true
                    }
                };

                applyFoodState.Invoke(tickManager, new object[] { hostFoods });

                Assert.IsFalse(foodManager.HasFood(staleLocalId));
                Assert.IsTrue(foodManager.HasFood(77));
                var nearest = foodManager.GetNearestFood(new Vector3(4f, 0f, -2f), 0.5f);
                Assert.IsTrue(nearest.HasValue);
                Assert.AreEqual(77, nearest.Value.Id);
                Assert.AreEqual(FoodType.Meat, nearest.Value.Type);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mgrGo);
                UnityEngine.Object.DestroyImmediate(foodManagerGo);
            }
        }

        [Test]
        public void BuildSessionState_Includes_CellPartPickups()
        {
            var mgrGo = new GameObject("TestTick");
            var pickupGo = new GameObject("PartPickup");
            try
            {
                var mgr = mgrGo.AddComponent<NetworkTickManager>();
                var pickup = pickupGo.AddComponent<CellPartPickup>();
                pickup.SetPickupId(55);
                pickup.SetPartType(CellPartType.Jet);

                var buildSessionState = typeof(NetworkTickManager).GetMethod(
                    "BuildSessionState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(buildSessionState);

                var state = (SessionNetState)buildSessionState.Invoke(mgr, new object[] { 1u });

                Assert.IsTrue(Array.Exists(state.PartPickups, p => p.PickupId == 55 && p.PartType == CellPartType.Jet));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(mgrGo);
                DestroyAllPartPickups();
            }
        }

        [Test]
        public void ApplyPartPickupState_ReconcilesClientPickupsToHostSnapshot()
        {
            var mgrGo = new GameObject("TestTick");
            var stageGo = new GameObject("CellStage_Test");
            try
            {
                var tickManager = mgrGo.AddComponent<NetworkTickManager>();
                var stage = stageGo.AddComponent<CellStage>();
                stage.AddOrUpdateNetworkPartPickup(12, CellPartType.Cilia, Vector3.zero);
                Assert.IsTrue(stage.HasPartPickup(12));

                var applyPickupState = typeof(NetworkTickManager).GetMethod(
                    "ApplyPartPickupState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(applyPickupState);

                var hostPickups = new[]
                {
                    new PartPickupNetState
                    {
                        PickupId = 77,
                        PosX = 5f,
                        PosY = -3f,
                        PartType = CellPartType.Spike,
                        Exists = true
                    }
                };

                applyPickupState.Invoke(tickManager, new object[] { hostPickups });

                Assert.IsFalse(stage.HasPartPickup(12));
                Assert.IsTrue(stage.HasPartPickup(77));
                var pickup = FindPartPickupById(77);
                Assert.NotNull(pickup);
                Assert.AreEqual(CellPartType.Spike, pickup.PartType);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mgrGo);
                UnityEngine.Object.DestroyImmediate(stageGo);
                DestroyAllPartPickups();
            }
        }

        [Test]
        public void ApplyCreaturePlayerState_SpawnsRemoteCreatureFromHostSnapshot()
        {
            var mgrGo = new GameObject("CreatureTick");
            try
            {
                var tickManager = mgrGo.AddComponent<NetworkTickManager>();
                var applyCreaturePlayers = typeof(NetworkTickManager).GetMethod(
                    "ApplyCreaturePlayerState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(applyCreaturePlayers);
                var dna = CreatureDNA.CreateDefault();
                dna.CreatureId = "remote-creature-8";
                dna.CreatureName = "Painted Runner";
                dna.Body.BodyScale = new Vector3(1.4f, 1.1f, 0.9f);

                var hostPlayers = new[]
                {
                    new CreaturePlayerNetState
                    {
                        PlayerId = 8,
                        CreatureId = "remote-creature-8",
                        CreatureDNAJson = dna.ToJson(),
                        PosX = 4f,
                        PosY = -6f,
                        RotY = 90f,
                        Health = 14f,
                        MaxHealth = 22f,
                        Stamina = 5f,
                        IsAlive = true
                    }
                };

                applyCreaturePlayers.Invoke(tickManager, new object[] { hostPlayers });

                var remote = FindRemoteCreatureByPlayerId(8);
                Assert.NotNull(remote);
                Assert.AreEqual("remote-creature-8", remote.CreatureId);
                Assert.AreEqual(14f, remote.Health, 0.0001f);
                Assert.AreEqual(22f, remote.MaxHealth, 0.0001f);
                Assert.AreEqual(5f, remote.Stamina, 0.0001f);
                Assert.AreEqual(4f, remote.transform.position.x, 0.0001f);
                Assert.AreEqual(-6f, remote.transform.position.z, 0.0001f);
                var instance = remote.GetComponent<CreatureInstance>();
                Assert.NotNull(instance);
                Assert.AreEqual("Painted Runner", instance.DNA.CreatureName);
                Assert.AreEqual(1.4f, instance.DNA.Body.BodyScale.x, 0.0001f);
                Assert.IsFalse(string.IsNullOrWhiteSpace(remote.SourceDnaJson));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mgrGo);
                DestroyAllRemoteCreatures();
            }
        }

        [Test]
        public void HostRemoteCreatureInput_IsIncludedInAuthoritativeCreatureSnapshots()
        {
            var mgrGo = new GameObject("HostCreatureTick");
            NetworkTickManager tickManager = null;
            try
            {
                tickManager = mgrGo.AddComponent<NetworkTickManager>();
                var session = new MultiplayerSessionManager();
                session.CreateSession();
                session.AddRemotePlayer(7, "Client Creature");
                tickManager.Activate(session, new NullNetworkTransport(), null);

                var dna = CreatureDNA.CreateDefault();
                dna.CreatureId = "client-creature-7";
                dna.CreatureName = "Client Runner";
                var input = new RemoteInputPacket
                {
                    PlayerId = 7,
                    MoveX = 0f,
                    MoveY = 1f,
                    Tick = 4u,
                    HasCreatureInput = true,
                    CreatureId = dna.CreatureId,
                    CreatureDNAJson = dna.ToJson(),
                    CreaturePosX = 3f,
                    CreaturePosY = -2f,
                    CreatureRotY = 25f,
                    CreatureHealth = 18f,
                    CreatureMaxHealth = 24f,
                    CreatureStamina = 9f,
                    CreatureIsAlive = true
                };

                var applyRemoteInput = typeof(NetworkTickManager).GetMethod(
                    "ApplyRemoteInput",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var buildSessionState = typeof(NetworkTickManager).GetMethod(
                    "BuildSessionState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(applyRemoteInput);
                Assert.NotNull(buildSessionState);

                applyRemoteInput.Invoke(tickManager, new object[] { 7, input });
                var state = (SessionNetState)buildSessionState.Invoke(tickManager, new object[] { 9u });

                Assert.IsTrue(Array.Exists(
                    state.CreaturePlayers,
                    p => p.PlayerId == 7 &&
                        p.CreatureId == "client-creature-7" &&
                        p.CreatureDNAJson.Contains("Client Runner")));
            }
            finally
            {
                tickManager?.Deactivate();
                UnityEngine.Object.DestroyImmediate(mgrGo);
                DestroyAllRemoteCreatures();
            }
        }

        private static RemotePlayerCreature FindRemoteCreatureByPlayerId(int playerId)
        {
            var creatures = UnityEngine.Object.FindObjectsByType<RemotePlayerCreature>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < creatures.Length; i++)
            {
                if (creatures[i] != null && creatures[i].PlayerId == playerId)
                    return creatures[i];
            }

            return null;
        }

        private static void DestroyAllRemoteCreatures()
        {
            var creatures = UnityEngine.Object.FindObjectsByType<RemotePlayerCreature>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < creatures.Length; i++)
            {
                if (creatures[i] != null)
                    UnityEngine.Object.DestroyImmediate(creatures[i].gameObject);
            }
        }

        private static CellPartPickup FindPartPickupById(int pickupId)
        {
            var pickups = UnityEngine.Object.FindObjectsByType<CellPartPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null && pickups[i].PickupId == pickupId)
                    return pickups[i];
            }

            return null;
        }

        private static void DestroyAllPartPickups()
        {
            var pickups = UnityEngine.Object.FindObjectsByType<CellPartPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null)
                    UnityEngine.Object.DestroyImmediate(pickups[i].gameObject);
            }
        }

        // ---- NullNetworkTransport: Activate with null session is safe ----

        [Test]
        public void Activate_With_Null_Session_Does_Not_Throw()
        {
            var go  = new GameObject("TestTick");
            var mgr = go.AddComponent<NetworkTickManager>();
            Assert.DoesNotThrow(() => mgr.Activate(null, null, null));
            mgr.Deactivate();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
