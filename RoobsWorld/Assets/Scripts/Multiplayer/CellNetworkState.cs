using System;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Deterministic FNV-1a checksum of a Genome for fast network equality checks.
    /// Floats are quantised to 2 decimal places to avoid cross-machine precision drift.
    /// </summary>
    public static class GenomeChecksum
    {
        public static uint Compute(Genome g)
        {
            if (g == null) return 0u;
            unchecked
            {
                uint h = 2166136261u;
                h = Fnv(h, Quant(g.Speed));
                h = Fnv(h, Quant(g.Size));
                h = Fnv(h, Quant(g.Attack));
                h = Fnv(h, Quant(g.Defence));
                h = Fnv(h, Quant(g.Digestion));
                h = Fnv(h, Quant(g.MetabolismCost));
                h = Fnv(h, Quant(g.Spikes));
                h = Fnv(h, Quant(g.Flagella));
                h = Fnv(h, (uint)(int)g.Traits);
                return h;
            }
        }

        private static uint Fnv(uint h, uint v) => (h ^ v) * 16777619u;
        private static uint Quant(float v) => (uint)Mathf.RoundToInt(v * 100f);
    }

    /// <summary>Network snapshot of one player cell for a single simulation tick.</summary>
    [Serializable]
    public struct PlayerNetState
    {
        public int   PlayerId;
        public float PosX, PosY;
        public float VelX, VelY;
        public uint  GenomeChecksum;
        public float Energy;
        public float Mass;
        public bool  IsAlive;
    }

    /// <summary>Network snapshot of one NPC organism for a single simulation tick.</summary>
    [Serializable]
    public struct OrganismNetState
    {
        public EntityId   OrgId;
        public float PosX, PosY;
        public uint  GenomeChecksum;
        public int   SpeciesId;
        public bool  IsAlive;
    }

    /// <summary>Network snapshot of one food item.</summary>
    [Serializable]
    public struct FoodNetState
    {
        public int      FoodId;
        public float    PosX, PosY;
        public FoodType FoodType;
        public bool     Exists;
    }

    /// <summary>Network snapshot of one Cell-stage part pickup.</summary>
    [Serializable]
    public struct PartPickupNetState
    {
        public int PickupId;
        public float PosX, PosY;
        public CellPartType PartType;
        public bool Exists;
    }

    /// <summary>Authoritative player creature snapshot for Creature stage sessions.</summary>
    [Serializable]
    public struct CreaturePlayerNetState
    {
        public int PlayerId;
        public string CreatureId;
        public string CreatureDNAJson;
        public float PosX, PosY;
        public float RotY;
        public float Health;
        public float MaxHealth;
        public float Stamina;
        public bool IsAlive;
    }

    /// <summary>Authoritative rival creature snapshot for Creature stage sessions.</summary>
    [Serializable]
    public struct CreatureRivalNetState
    {
        public string CreatureId;
        public int Archetype;
        public bool IsAlpha;
        public float PosX, PosY;
        public float RotY;
        public float Health;
        public float MaxHealth;
        public bool Exists;
        public bool IsAlive;
    }

    /// <summary>Authoritative objective progress snapshot for Creature stage sessions.</summary>
    [Serializable]
    public struct CreatureObjectiveNetState
    {
        public bool HasObjective;
        public int ObjectiveType;
        public int Progress;
        public int Target;
        public int ActiveRivals;
        public int DesiredActiveRivals;
        public int ObjectivesCompleted;
        public float HoldSeconds;
        public bool IsComplete;
    }

    /// <summary>Authoritative Creature-stage world progression snapshot shared by the host.</summary>
    [Serializable]
    public struct CreatureWorldProgressionNetState
    {
        public bool HasProgression;
        public int DNA;
        public int Meals;
        public int Allies;
        public int Defeats;
        public int Discoveries;
        public int MigrationsCompleted;
        public bool IsAtNest;
        public bool HasPackMate;
        public bool CanEditAtNest;
        public bool CanMigrate;
        public float NearestNestDistance;
        public float NearestMigrationDistance;
    }

    /// <summary>
    /// Full authoritative session state broadcast by the host each network tick.
    /// Clients apply this snapshot to reconcile their local simulation.
    /// </summary>
    [Serializable]
    public struct SessionNetState
    {
        public uint               Tick;
        public PlayerNetState[]   Players;
        public OrganismNetState[] Organisms;
        public FoodNetState[]     Foods;
        public PartPickupNetState[] PartPickups;
        public CreaturePlayerNetState[] CreaturePlayers;
        public CreatureRivalNetState[] CreatureRivals;
        public CreatureObjectiveNetState CreatureObjective;
        public CreatureWorldProgressionNetState CreatureWorldProgression;
    }

    /// <summary>Host-authoritative command to move every client into the same game stage.</summary>
    [Serializable]
    public struct StageTransitionNetState
    {
        public uint Tick;
        public int RequestingPlayerId;
        public GameStageType TargetStage;
        public uint TransitionId;
    }

    public enum NetworkConnectionQuality
    {
        Healthy,
        Lagging,
        Reconnecting,
        Disconnected
    }

    /// <summary>Client-facing network quality and recovery diagnostics for multiplayer UI and tests.</summary>
    [Serializable]
    public struct NetworkQualitySnapshot
    {
        public float PingMs;
        public float RttTrendMs;
        public float PacketLossRatio;
        public float LastAuthoritativeSnapshotAgeSeconds;
        public int ReconnectAttempts;
        public NetworkConnectionQuality Quality;
    }

    /// <summary>Small ping packet used to measure RTT and detect stale connections.</summary>
    [Serializable]
    public struct NetworkHeartbeatPacket
    {
        public int PlayerId;
        public uint Sequence;
        public double SentAtSeconds;
        public bool IsAck;
    }

    /// <summary>Client request for a fresh authoritative snapshot after reconnect or timeout.</summary>
    [Serializable]
    public struct NetworkRecoveryRequestPacket
    {
        public int PlayerId;
        public uint LastReceivedTick;
        public GameStageType CurrentStage;
        public int ReconnectAttempt;
    }

    /// <summary>Pure multiplayer resilience helpers shared by runtime and EditMode tests.</summary>
    public static class NetworkResilienceRules
    {
        public const float DefaultHeartbeatIntervalSeconds = 1f;
        public const float DefaultLagWarningRttMs = 180f;
        public const float DefaultReconnectSnapshotAgeSeconds = 3f;
        public const float DefaultDisconnectSnapshotAgeSeconds = 8f;

        public static float SmoothRtt(float previousTrendMs, float latestPingMs, float smoothing = 0.2f)
        {
            float safeLatest = Mathf.Max(0f, latestPingMs);
            if (previousTrendMs <= 0f)
                return safeLatest;

            float t = Mathf.Clamp01(smoothing);
            return Mathf.Lerp(previousTrendMs, safeLatest, t);
        }

        public static float ComputePacketLossRatio(int packetsSent, int packetsReceived)
        {
            int sent = Mathf.Max(0, packetsSent);
            if (sent == 0)
                return 0f;

            int received = Mathf.Clamp(packetsReceived, 0, sent);
            return Mathf.Clamp01((sent - received) / (float)sent);
        }

        public static NetworkConnectionQuality ClassifyQuality(
            bool transportConnected,
            float rttTrendMs,
            float packetLossRatio,
            float lastAuthoritativeSnapshotAgeSeconds,
            int reconnectAttempts,
            float lagWarningRttMs = DefaultLagWarningRttMs,
            float reconnectSnapshotAgeSeconds = DefaultReconnectSnapshotAgeSeconds,
            float disconnectSnapshotAgeSeconds = DefaultDisconnectSnapshotAgeSeconds)
        {
            float snapshotAge = Mathf.Max(0f, lastAuthoritativeSnapshotAgeSeconds);
            if (!transportConnected || snapshotAge >= Mathf.Max(reconnectSnapshotAgeSeconds, disconnectSnapshotAgeSeconds))
                return NetworkConnectionQuality.Disconnected;

            if (reconnectAttempts > 0 || snapshotAge >= Mathf.Max(0.1f, reconnectSnapshotAgeSeconds))
                return NetworkConnectionQuality.Reconnecting;

            if (rttTrendMs >= Mathf.Max(1f, lagWarningRttMs) || packetLossRatio >= 0.08f)
                return NetworkConnectionQuality.Lagging;

            return NetworkConnectionQuality.Healthy;
        }

        public static NetworkQualitySnapshot BuildSnapshot(
            bool transportConnected,
            float pingMs,
            float rttTrendMs,
            int packetsSent,
            int packetsReceived,
            float lastAuthoritativeSnapshotAgeSeconds,
            int reconnectAttempts)
        {
            float loss = ComputePacketLossRatio(packetsSent, packetsReceived);
            return new NetworkQualitySnapshot
            {
                PingMs = Mathf.Max(0f, pingMs),
                RttTrendMs = Mathf.Max(0f, rttTrendMs),
                PacketLossRatio = loss,
                LastAuthoritativeSnapshotAgeSeconds = Mathf.Max(0f, lastAuthoritativeSnapshotAgeSeconds),
                ReconnectAttempts = Mathf.Max(0, reconnectAttempts),
                Quality = ClassifyQuality(
                    transportConnected,
                    rttTrendMs,
                    loss,
                    lastAuthoritativeSnapshotAgeSeconds,
                    reconnectAttempts)
            };
        }

        public static bool ShouldRequestRecovery(
            bool isHost,
            NetworkConnectionQuality quality,
            float secondsSinceLastRequest,
            float retryIntervalSeconds = 1.25f)
        {
            if (isHost)
                return false;
            if (quality != NetworkConnectionQuality.Reconnecting && quality != NetworkConnectionQuality.Disconnected)
                return false;

            return secondsSinceLastRequest >= Mathf.Max(0.1f, retryIntervalSeconds);
        }

        public static float ComputeCorrectionAlpha(float snapshotAgeSeconds, float minAlpha = 0.15f, float maxAlpha = 0.85f)
        {
            float normalizedAge = Mathf.Clamp01(Mathf.Max(0f, snapshotAgeSeconds) / DefaultReconnectSnapshotAgeSeconds);
            return Mathf.Lerp(Mathf.Clamp01(minAlpha), Mathf.Clamp01(maxAlpha), normalizedAge);
        }
    }

    /// <summary>Input snapshot sent from each client to the host every tick.</summary>
    [Serializable]
    public struct RemoteInputPacket
    {
        public int   PlayerId;
        public float MoveX, MoveY;
        public bool  EatInput;
        public float Mass;
        public uint  Tick;
        public bool HasCreatureInput;
        public string CreatureId;
        public string CreatureDNAJson;
        public float CreaturePosX, CreaturePosY;
        public float CreatureRotY;
        public float CreatureHealth;
        public float CreatureMaxHealth;
        public float CreatureStamina;
        public bool CreatureIsAlive;
    }

    /// <summary>Small microphone audio chunk broadcast between nearby players.</summary>
    [Serializable]
    public struct VoiceChatPacket
    {
        public int     PlayerId;
        public uint    Sequence;
        public int     SampleRate;
        public short[] Pcm16;
    }
}
