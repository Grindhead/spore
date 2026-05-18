using System;
using System.Collections.Generic;
using System.Globalization;

namespace RoobsWorld.Multiplayer
{
    [Serializable]
    public struct SessionMapLandmark
    {
        public string Type;
        public float X;
        public float Z;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public SessionMapLandmark(string type, float x, float z, float scaleX, float scaleY, float scaleZ)
        {
            Type = type;
            X = x;
            Z = z;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
        }
    }

    [Serializable]
    public struct SessionMapDefinition
    {
        public const string CreatureMode = "creature";
        public const int CurrentVersion = 1;

        public string Mode;
        public int Version;
        public int Seed;
        public float ArenaRadius;
        public int IslandResolution;
        public float IslandHeight;
        public float IslandNoiseScale;
        public SessionMapLandmark[] Landmarks;

        public bool IsCreatureMap =>
            string.Equals(Mode, CreatureMode, StringComparison.OrdinalIgnoreCase);

        public bool IsValid =>
            IsCreatureMap
            && Version > 0
            && Seed > 0
            && ArenaRadius > 0f
            && IslandResolution > 0
            && IslandHeight > 0f
            && IslandNoiseScale > 0f;

        public SessionMapDefinition(
            string mode,
            int version,
            int seed,
            float arenaRadius,
            int islandResolution,
            float islandHeight,
            float islandNoiseScale,
            SessionMapLandmark[] landmarks)
        {
            Mode = mode;
            Version = version;
            Seed = seed;
            ArenaRadius = arenaRadius;
            IslandResolution = islandResolution;
            IslandHeight = islandHeight;
            IslandNoiseScale = islandNoiseScale;
            Landmarks = landmarks ?? Array.Empty<SessionMapLandmark>();
        }

        public static bool TryDecode(string encoded, out SessionMapDefinition map)
        {
            map = default;
            if (string.IsNullOrWhiteSpace(encoded))
                return false;

            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(encoded);
            }
            catch
            {
                return false;
            }

            string[] sections = decoded.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length == 0)
                return false;

            string[] header = sections[0].Split(',');
            if (header.Length < 7)
                return false;

            string mode = header[0];
            if (!TryParseInt(header[1], out int version)
                || !TryParseInt(header[2], out int seed)
                || !TryParseFloat(header[3], out float arenaRadius)
                || !TryParseInt(header[4], out int islandResolution)
                || !TryParseFloat(header[5], out float islandHeight)
                || !TryParseFloat(header[6], out float islandNoiseScale))
            {
                return false;
            }

            var landmarks = new List<SessionMapLandmark>(Math.Max(0, sections.Length - 1));
            for (int i = 1; i < sections.Length; i++)
            {
                string[] parts = sections[i].Split(',');
                if (parts.Length < 6)
                    return false;

                if (!TryParseFloat(parts[1], out float x)
                    || !TryParseFloat(parts[2], out float z)
                    || !TryParseFloat(parts[3], out float scaleX)
                    || !TryParseFloat(parts[4], out float scaleY)
                    || !TryParseFloat(parts[5], out float scaleZ))
                {
                    return false;
                }

                landmarks.Add(new SessionMapLandmark(parts[0], x, z, scaleX, scaleY, scaleZ));
            }

            map = new SessionMapDefinition(
                mode,
                version,
                seed,
                arenaRadius,
                islandResolution,
                islandHeight,
                islandNoiseScale,
                landmarks.ToArray());

            return map.IsValid;
        }

        private static bool TryParseInt(string value, out int parsed)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsed);
        }

        private static bool TryParseFloat(string value, out float parsed)
        {
            return float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out parsed);
        }
    }
}
