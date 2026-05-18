using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    public static class RelayConfigService
    {
        [Serializable]
        public sealed class RelayConfig
        {
            public string name = "";
            public string relayUrl = "";
            public string gameToken = "";
            public RelayServerConfig[] officialServers = Array.Empty<RelayServerConfig>();
        }

        [Serializable]
        public sealed class RelayServerConfig
        {
            public string name = "";
            public string region = "";
            public string relayUrl = "";
            public string[] relayUrls = Array.Empty<string>();
            public string gameToken = "";

            public string[] GetRelayUrls()
            {
                var urls = new List<string>();
                AddRelayUrl(urls, relayUrl);
                if (relayUrls != null)
                {
                    foreach (var url in relayUrls)
                        AddRelayUrl(urls, url);
                }

                return urls.ToArray();
            }
        }

        public static RelayConfig LoadFromResources(string resourcePath = "multiplayer-relay")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return null;

            try
            {
                return JsonUtility.FromJson<RelayConfig>(asset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RelayConfigService] Invalid config: " + ex.Message);
                return null;
            }
        }

        public static (string relayUrl, string gameToken, string relayName) ParseCommandLineOverrides(string[] args)
        {
            string relayUrl = null;
            string gameToken = null;
            string relayName = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-relayUrl")
                    relayUrl = args[i + 1];
                else if (args[i] == "-gameToken")
                    gameToken = args[i + 1];
                else if (args[i] == "-relayName")
                    relayName = args[i + 1];
            }

            return (relayUrl, gameToken, relayName);
        }

        private static void AddRelayUrl(List<string> urls, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string trimmed = value.Trim();
            if (!urls.Contains(trimmed))
                urls.Add(trimmed);
        }
    }
}
