using System;
using UnityEngine;
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
    }

    /// <summary>Input snapshot sent from each client to the host every tick.</summary>
    [Serializable]
    public struct RemoteInputPacket
    {
        public int   PlayerId;
        public float MoveX, MoveY;
        public bool  EatInput;
        public uint  Tick;
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
