using System;
using System.IO;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Fast binary serialization for all multiplayer network state types.
    ///
    /// Format contract (must match between host and all clients):
    ///   • All multi-byte values are little-endian (BinaryWriter default on all target platforms).
    ///   • Floats are written as-is (4 bytes IEEE 754); positions and velocities.
    ///   • FoodType is serialized as a single byte.
    ///   • Arrays are prefixed with an int32 element count.
    ///   • bool is 1 byte (0 = false, 1 = true).
    ///
    /// Throws <see cref="InvalidDataException"/> on read if the stream is truncated.
    /// </summary>
    public static class NetworkSerializer
    {
        // ---- SessionNetState ----

        /// <summary>Serializes a full host-broadcast snapshot to bytes.</summary>
        public static byte[] Serialize(in SessionNetState state)
        {
            using var ms = new MemoryStream(256);
            using var w  = new BinaryWriter(ms);

            w.Write(state.Tick); // uint32

            // Players
            int pc = state.Players?.Length ?? 0;
            w.Write(pc);
            for (int i = 0; i < pc; i++) WritePlayer(w, state.Players[i]);

            // Organisms
            int oc = state.Organisms?.Length ?? 0;
            w.Write(oc);
            for (int i = 0; i < oc; i++) WriteOrganism(w, state.Organisms[i]);

            // Foods
            int fc = state.Foods?.Length ?? 0;
            w.Write(fc);
            for (int i = 0; i < fc; i++) WriteFood(w, state.Foods[i]);

            int pcPickup = state.PartPickups?.Length ?? 0;
            w.Write(pcPickup);
            for (int i = 0; i < pcPickup; i++) WritePartPickup(w, state.PartPickups[i]);

            int creaturePlayerCount = state.CreaturePlayers?.Length ?? 0;
            w.Write(creaturePlayerCount);
            for (int i = 0; i < creaturePlayerCount; i++) WriteCreaturePlayer(w, state.CreaturePlayers[i]);

            int creatureRivalCount = state.CreatureRivals?.Length ?? 0;
            w.Write(creatureRivalCount);
            for (int i = 0; i < creatureRivalCount; i++) WriteCreatureRival(w, state.CreatureRivals[i]);

            WriteCreatureObjective(w, state.CreatureObjective);
            WriteCreatureWorldProgression(w, state.CreatureWorldProgression);

            return ms.ToArray();
        }

        /// <summary>Deserializes a host-broadcast snapshot from bytes.</summary>
        public static SessionNetState DeserializeSession(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty SessionNetState packet.");

            return DeserializeSession(data, 0, data.Length);
        }

        /// <summary>Deserializes a host-broadcast snapshot from a slice of a tagged packet.</summary>
        public static SessionNetState DeserializeSession(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty SessionNetState packet.");
            ValidateSlice(data, offset, count, "SessionNetState packet.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r  = new BinaryReader(ms);

            var state = new SessionNetState { Tick = r.ReadUInt32() };

            int pc = r.ReadInt32();
            state.Players = new PlayerNetState[pc];
            for (int i = 0; i < pc; i++) state.Players[i] = ReadPlayer(r);

            int oc = r.ReadInt32();
            state.Organisms = new OrganismNetState[oc];
            for (int i = 0; i < oc; i++) state.Organisms[i] = ReadOrganism(r);

            int fc = r.ReadInt32();
            state.Foods = new FoodNetState[fc];
            for (int i = 0; i < fc; i++) state.Foods[i] = ReadFood(r);

            if (ms.Position < ms.Length)
            {
                int pcPickup = r.ReadInt32();
                state.PartPickups = new PartPickupNetState[pcPickup];
                for (int i = 0; i < pcPickup; i++) state.PartPickups[i] = ReadPartPickup(r);
            }
            else
            {
                state.PartPickups = Array.Empty<PartPickupNetState>();
            }

            if (ms.Position < ms.Length)
            {
                int creaturePlayerCount = r.ReadInt32();
                state.CreaturePlayers = new CreaturePlayerNetState[creaturePlayerCount];
                for (int i = 0; i < creaturePlayerCount; i++) state.CreaturePlayers[i] = ReadCreaturePlayer(r);
            }
            else
            {
                state.CreaturePlayers = Array.Empty<CreaturePlayerNetState>();
            }

            if (ms.Position < ms.Length)
            {
                int creatureRivalCount = r.ReadInt32();
                state.CreatureRivals = new CreatureRivalNetState[creatureRivalCount];
                for (int i = 0; i < creatureRivalCount; i++) state.CreatureRivals[i] = ReadCreatureRival(r);
            }
            else
            {
                state.CreatureRivals = Array.Empty<CreatureRivalNetState>();
            }

            state.CreatureObjective = ms.Position < ms.Length
                ? ReadCreatureObjective(r)
                : default;

            state.CreatureWorldProgression = ms.Position < ms.Length
                ? ReadCreatureWorldProgression(r)
                : default;

            return state;
        }

        // ---- RemoteInputPacket ----

        /// <summary>Serializes a client input packet to bytes (sent every tick from client → host).</summary>
        public static byte[] Serialize(in RemoteInputPacket pkt)
        {
            using var ms = new MemoryStream(256);
            using var w  = new BinaryWriter(ms);

            w.Write(pkt.PlayerId);
            w.Write(pkt.MoveX);
            w.Write(pkt.MoveY);
            w.Write(pkt.EatInput);
            w.Write(pkt.Mass);
            w.Write(pkt.Tick);
            w.Write(pkt.HasCreatureInput);
            w.Write(pkt.CreatureId ?? string.Empty);
            w.Write(pkt.CreatureDNAJson ?? string.Empty);
            w.Write(pkt.CreaturePosX);
            w.Write(pkt.CreaturePosY);
            w.Write(pkt.CreatureRotY);
            w.Write(pkt.CreatureHealth);
            w.Write(pkt.CreatureMaxHealth);
            w.Write(pkt.CreatureStamina);
            w.Write(pkt.CreatureIsAlive);

            return ms.ToArray();
        }

        /// <summary>Deserializes a client input packet from bytes.</summary>
        public static RemoteInputPacket DeserializeInput(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty RemoteInputPacket.");

            return DeserializeInput(data, 0, data.Length);
        }

        /// <summary>Deserializes a client input packet from a slice of a tagged packet.</summary>
        public static RemoteInputPacket DeserializeInput(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty RemoteInputPacket.");
            ValidateSlice(data, offset, count, "RemoteInputPacket.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r  = new BinaryReader(ms);

            var pkt = new RemoteInputPacket
            {
                PlayerId = r.ReadInt32(),
                MoveX    = r.ReadSingle(),
                MoveY    = r.ReadSingle(),
                EatInput = r.ReadBoolean(),
                Mass     = r.ReadSingle(),
                Tick     = r.ReadUInt32()
            };

            if (ms.Position < ms.Length)
            {
                pkt.HasCreatureInput = r.ReadBoolean();
                pkt.CreatureId = r.ReadString();
                pkt.CreatureDNAJson = r.ReadString();
                pkt.CreaturePosX = r.ReadSingle();
                pkt.CreaturePosY = r.ReadSingle();
                pkt.CreatureRotY = r.ReadSingle();
                pkt.CreatureHealth = r.ReadSingle();
                pkt.CreatureMaxHealth = r.ReadSingle();
                pkt.CreatureStamina = r.ReadSingle();
                pkt.CreatureIsAlive = r.ReadBoolean();
            }

            return pkt;
        }

        // ---- StageTransitionNetState ----

        public static byte[] Serialize(in StageTransitionNetState pkt)
        {
            using var ms = new MemoryStream(16);
            using var w = new BinaryWriter(ms);

            w.Write(pkt.Tick);
            w.Write(pkt.RequestingPlayerId);
            w.Write((int)pkt.TargetStage);
            w.Write(pkt.TransitionId);

            return ms.ToArray();
        }

        public static StageTransitionNetState DeserializeStageTransition(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty StageTransitionNetState packet.");

            return DeserializeStageTransition(data, 0, data.Length);
        }

        public static StageTransitionNetState DeserializeStageTransition(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty StageTransitionNetState packet.");
            ValidateSlice(data, offset, count, "StageTransitionNetState packet.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r = new BinaryReader(ms);

            var transition = new StageTransitionNetState
            {
                Tick = r.ReadUInt32(),
                RequestingPlayerId = r.ReadInt32(),
                TargetStage = (GameStageType)r.ReadInt32()
            };

            if (ms.Position < ms.Length)
                transition.TransitionId = r.ReadUInt32();

            return transition;
        }

        // ---- Network resilience packets ----

        public static byte[] Serialize(in NetworkHeartbeatPacket pkt)
        {
            using var ms = new MemoryStream(24);
            using var w = new BinaryWriter(ms);

            w.Write(pkt.PlayerId);
            w.Write(pkt.Sequence);
            w.Write(pkt.SentAtSeconds);
            w.Write(pkt.IsAck);

            return ms.ToArray();
        }

        public static NetworkHeartbeatPacket DeserializeHeartbeat(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty NetworkHeartbeatPacket.");
            ValidateSlice(data, offset, count, "NetworkHeartbeatPacket.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r = new BinaryReader(ms);

            return new NetworkHeartbeatPacket
            {
                PlayerId = r.ReadInt32(),
                Sequence = r.ReadUInt32(),
                SentAtSeconds = r.ReadDouble(),
                IsAck = r.ReadBoolean()
            };
        }

        public static byte[] Serialize(in NetworkRecoveryRequestPacket pkt)
        {
            using var ms = new MemoryStream(20);
            using var w = new BinaryWriter(ms);

            w.Write(pkt.PlayerId);
            w.Write(pkt.LastReceivedTick);
            w.Write((int)pkt.CurrentStage);
            w.Write(pkt.ReconnectAttempt);

            return ms.ToArray();
        }

        public static NetworkRecoveryRequestPacket DeserializeRecoveryRequest(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty NetworkRecoveryRequestPacket.");
            ValidateSlice(data, offset, count, "NetworkRecoveryRequestPacket.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r = new BinaryReader(ms);

            return new NetworkRecoveryRequestPacket
            {
                PlayerId = r.ReadInt32(),
                LastReceivedTick = r.ReadUInt32(),
                CurrentStage = (GameStageType)r.ReadInt32(),
                ReconnectAttempt = r.ReadInt32()
            };
        }

        // ---- VoiceChatPacket ----

        /// <summary>Serializes a small voice chat PCM packet to bytes.</summary>
        public static byte[] Serialize(in VoiceChatPacket pkt)
        {
            int count = pkt.Pcm16?.Length ?? 0;
            using var ms = new MemoryStream(16 + count * sizeof(short));
            using var w  = new BinaryWriter(ms);

            w.Write(pkt.PlayerId);
            w.Write(pkt.Sequence);
            w.Write(pkt.SampleRate);
            w.Write(count);
            for (int i = 0; i < count; i++)
                w.Write(pkt.Pcm16[i]);

            return ms.ToArray();
        }

        /// <summary>Deserializes a voice chat PCM packet from bytes.</summary>
        public static VoiceChatPacket DeserializeVoice(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty VoiceChatPacket.");

            return DeserializeVoice(data, 0, data.Length);
        }

        /// <summary>Deserializes a voice chat PCM packet from a slice of a tagged packet.</summary>
        public static VoiceChatPacket DeserializeVoice(byte[] data, int offset, int count)
        {
            if (data == null || data.Length == 0)
                throw new InvalidDataException("Empty VoiceChatPacket.");
            ValidateSlice(data, offset, count, "VoiceChatPacket.");

            using var ms = new MemoryStream(data, offset, count, writable: false);
            using var r  = new BinaryReader(ms);

            var pkt = new VoiceChatPacket
            {
                PlayerId   = r.ReadInt32(),
                Sequence   = r.ReadUInt32(),
                SampleRate = r.ReadInt32()
            };

            int sampleCount = r.ReadInt32();
            if (sampleCount < 0 || sampleCount > ProximityVoiceChat.MaxSamplesPerPacket)
                throw new InvalidDataException("Invalid VoiceChatPacket sample count.");

            pkt.Pcm16 = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                pkt.Pcm16[i] = r.ReadInt16();

            return pkt;
        }

        // ---- Helpers ----

        private static void WritePlayer(BinaryWriter w, in PlayerNetState p)
        {
            w.Write(p.PlayerId);
            w.Write(p.PosX);
            w.Write(p.PosY);
            w.Write(p.VelX);
            w.Write(p.VelY);
            w.Write(p.GenomeChecksum);
            w.Write(p.Energy);
            w.Write(p.Mass);
            w.Write(p.IsAlive);
        }

        private static PlayerNetState ReadPlayer(BinaryReader r) => new PlayerNetState
        {
            PlayerId       = r.ReadInt32(),
            PosX           = r.ReadSingle(),
            PosY           = r.ReadSingle(),
            VelX           = r.ReadSingle(),
            VelY           = r.ReadSingle(),
            GenomeChecksum = r.ReadUInt32(),
            Energy         = r.ReadSingle(),
            Mass           = r.ReadSingle(),
            IsAlive        = r.ReadBoolean()
        };

        private static void WriteOrganism(BinaryWriter w, in OrganismNetState o)
        {
#pragma warning disable CS0618 // Unity EntityId int casting is transitional in current Unity 6 API.
            w.Write((int)o.OrgId);
#pragma warning restore CS0618
            w.Write(o.PosX);
            w.Write(o.PosY);
            w.Write(o.GenomeChecksum);
            w.Write(o.SpeciesId);
            w.Write(o.IsAlive);
        }

        private static OrganismNetState ReadOrganism(BinaryReader r) => new OrganismNetState
        {
#pragma warning disable CS0618 // Unity EntityId int casting is transitional in current Unity 6 API.
            OrgId          = (UnityEngine.EntityId)r.ReadInt32(),
#pragma warning restore CS0618
            PosX           = r.ReadSingle(),
            PosY           = r.ReadSingle(),
            GenomeChecksum = r.ReadUInt32(),
            SpeciesId      = r.ReadInt32(),
            IsAlive        = r.ReadBoolean()
        };

        private static void WriteFood(BinaryWriter w, in FoodNetState f)
        {
            w.Write(f.FoodId);
            w.Write(f.PosX);
            w.Write(f.PosY);
            w.Write((byte)f.FoodType);
            w.Write(f.Exists);
        }

        private static FoodNetState ReadFood(BinaryReader r) => new FoodNetState
        {
            FoodId   = r.ReadInt32(),
            PosX     = r.ReadSingle(),
            PosY     = r.ReadSingle(),
            FoodType = (FoodType)r.ReadByte(),
            Exists   = r.ReadBoolean()
        };

        private static void WritePartPickup(BinaryWriter w, in PartPickupNetState p)
        {
            w.Write(p.PickupId);
            w.Write(p.PosX);
            w.Write(p.PosY);
            w.Write((byte)p.PartType);
            w.Write(p.Exists);
        }

        private static PartPickupNetState ReadPartPickup(BinaryReader r) => new PartPickupNetState
        {
            PickupId = r.ReadInt32(),
            PosX = r.ReadSingle(),
            PosY = r.ReadSingle(),
            PartType = (CellPartType)r.ReadByte(),
            Exists = r.ReadBoolean()
        };

        private static void WriteCreaturePlayer(BinaryWriter w, in CreaturePlayerNetState p)
        {
            w.Write(p.PlayerId);
            w.Write(p.CreatureId ?? string.Empty);
            w.Write(p.CreatureDNAJson ?? string.Empty);
            w.Write(p.PosX);
            w.Write(p.PosY);
            w.Write(p.RotY);
            w.Write(p.Health);
            w.Write(p.MaxHealth);
            w.Write(p.Stamina);
            w.Write(p.IsAlive);
        }

        private static CreaturePlayerNetState ReadCreaturePlayer(BinaryReader r) => new CreaturePlayerNetState
        {
            PlayerId = r.ReadInt32(),
            CreatureId = r.ReadString(),
            CreatureDNAJson = r.ReadString(),
            PosX = r.ReadSingle(),
            PosY = r.ReadSingle(),
            RotY = r.ReadSingle(),
            Health = r.ReadSingle(),
            MaxHealth = r.ReadSingle(),
            Stamina = r.ReadSingle(),
            IsAlive = r.ReadBoolean()
        };

        private static void WriteCreatureRival(BinaryWriter w, in CreatureRivalNetState r)
        {
            w.Write(r.CreatureId ?? string.Empty);
            w.Write(r.Archetype);
            w.Write(r.IsAlpha);
            w.Write(r.PosX);
            w.Write(r.PosY);
            w.Write(r.RotY);
            w.Write(r.Health);
            w.Write(r.MaxHealth);
            w.Write(r.Exists);
            w.Write(r.IsAlive);
        }

        private static CreatureRivalNetState ReadCreatureRival(BinaryReader r) => new CreatureRivalNetState
        {
            CreatureId = r.ReadString(),
            Archetype = r.ReadInt32(),
            IsAlpha = r.ReadBoolean(),
            PosX = r.ReadSingle(),
            PosY = r.ReadSingle(),
            RotY = r.ReadSingle(),
            Health = r.ReadSingle(),
            MaxHealth = r.ReadSingle(),
            Exists = r.ReadBoolean(),
            IsAlive = r.ReadBoolean()
        };

        private static void WriteCreatureObjective(BinaryWriter w, in CreatureObjectiveNetState objective)
        {
            w.Write(objective.HasObjective);
            w.Write(objective.ObjectiveType);
            w.Write(objective.Progress);
            w.Write(objective.Target);
            w.Write(objective.ActiveRivals);
            w.Write(objective.DesiredActiveRivals);
            w.Write(objective.ObjectivesCompleted);
            w.Write(objective.HoldSeconds);
            w.Write(objective.IsComplete);
        }

        private static CreatureObjectiveNetState ReadCreatureObjective(BinaryReader r) => new CreatureObjectiveNetState
        {
            HasObjective = r.ReadBoolean(),
            ObjectiveType = r.ReadInt32(),
            Progress = r.ReadInt32(),
            Target = r.ReadInt32(),
            ActiveRivals = r.ReadInt32(),
            DesiredActiveRivals = r.ReadInt32(),
            ObjectivesCompleted = r.ReadInt32(),
            HoldSeconds = r.ReadSingle(),
            IsComplete = r.ReadBoolean()
        };

        private static void WriteCreatureWorldProgression(BinaryWriter w, in CreatureWorldProgressionNetState progression)
        {
            w.Write(progression.HasProgression);
            w.Write(progression.DNA);
            w.Write(progression.Meals);
            w.Write(progression.Allies);
            w.Write(progression.Defeats);
            w.Write(progression.Discoveries);
            w.Write(progression.MigrationsCompleted);
            w.Write(progression.IsAtNest);
            w.Write(progression.HasPackMate);
            w.Write(progression.CanEditAtNest);
            w.Write(progression.CanMigrate);
            w.Write(progression.NearestNestDistance);
            w.Write(progression.NearestMigrationDistance);
        }

        private static CreatureWorldProgressionNetState ReadCreatureWorldProgression(BinaryReader r) => new CreatureWorldProgressionNetState
        {
            HasProgression = r.ReadBoolean(),
            DNA = r.ReadInt32(),
            Meals = r.ReadInt32(),
            Allies = r.ReadInt32(),
            Defeats = r.ReadInt32(),
            Discoveries = r.ReadInt32(),
            MigrationsCompleted = r.ReadInt32(),
            IsAtNest = r.ReadBoolean(),
            HasPackMate = r.ReadBoolean(),
            CanEditAtNest = r.ReadBoolean(),
            CanMigrate = r.ReadBoolean(),
            NearestNestDistance = r.ReadSingle(),
            NearestMigrationDistance = r.ReadSingle()
        };

        private static void ValidateSlice(byte[] data, int offset, int count, string packetName)
        {
            if (offset < 0 || count <= 0 || offset > data.Length || count > data.Length - offset)
                throw new InvalidDataException($"Invalid {packetName} slice.");
        }
    }
}
