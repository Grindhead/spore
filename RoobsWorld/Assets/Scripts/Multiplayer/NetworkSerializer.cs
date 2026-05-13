using System;
using System.IO;
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

            return state;
        }

        // ---- RemoteInputPacket ----

        /// <summary>Serializes a client input packet to bytes (sent every tick from client → host).</summary>
        public static byte[] Serialize(in RemoteInputPacket pkt)
        {
            using var ms = new MemoryStream(24);
            using var w  = new BinaryWriter(ms);

            w.Write(pkt.PlayerId);
            w.Write(pkt.MoveX);
            w.Write(pkt.MoveY);
            w.Write(pkt.EatInput);
            w.Write(pkt.Mass);
            w.Write(pkt.Tick);

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

            return new RemoteInputPacket
            {
                PlayerId = r.ReadInt32(),
                MoveX    = r.ReadSingle(),
                MoveY    = r.ReadSingle(),
                EatInput = r.ReadBoolean(),
                Mass     = r.ReadSingle(),
                Tick     = r.ReadUInt32()
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

        private static void ValidateSlice(byte[] data, int offset, int count, string packetName)
        {
            if (offset < 0 || count <= 0 || offset > data.Length || count > data.Length - offset)
                throw new InvalidDataException($"Invalid {packetName} slice.");
        }
    }
}
