using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Uploads save snapshots to the relay backend for server-side autosave.
    /// </summary>
    public static class ServerProgressAutosaveClient
    {
        private const float MinUploadIntervalSeconds = 10f;

        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static float _lastUploadRealtime = -999f;

        public static void TryUpload(SaveData data, string slotName)
        {
            if (data == null)
                return;

            if (Time.realtimeSinceStartup - _lastUploadRealtime < MinUploadIntervalSeconds)
                return;

            string baseUrl = ResolveAutosaveBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            string token = ResolveGameToken();
            if (string.IsNullOrWhiteSpace(token))
                return;

            string playerId = ResolvePlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            _lastUploadRealtime = Time.realtimeSinceStartup;
            string payload = JsonUtility.ToJson(new AutosaveEnvelope
            {
                slot = string.IsNullOrWhiteSpace(slotName) ? "autosave" : slotName,
                save = data
            });

#pragma warning disable CS4014
            UploadAsync(baseUrl, token, playerId, payload);
#pragma warning restore CS4014
        }

        public static SaveData TryLoad(string slotName = null)
        {
            string baseUrl = ResolveAutosaveBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            string token = ResolveGameToken();
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string playerId = ResolvePlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
                return null;

            try
            {
                string targetUrl = baseUrl.TrimEnd('/') + "/api/progress/autosave/" + Uri.EscapeDataString(playerId);
                using var req = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                using HttpResponseMessage response = Client.SendAsync(req).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var wrapper = JsonUtility.FromJson<AutosaveLookupResponse>(json);
                if (wrapper == null || !wrapper.ok || wrapper.data == null)
                    return null;

                string expectedSlot = string.IsNullOrWhiteSpace(slotName) ? "autosave" : slotName;
                if (!string.IsNullOrWhiteSpace(wrapper.data.slot)
                    && !string.Equals(wrapper.data.slot, expectedSlot, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return wrapper.data.save;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Server load error: {ex.Message}");
                return null;
            }
        }

        private static async Task UploadAsync(string baseUrl, string token, string playerId, string payload)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/api/progress/autosave");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                req.Headers.Add("x-player-id", playerId);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await Client.SendAsync(req).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    Debug.LogWarning($"[Autosave] Server autosave failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Server autosave error: {ex.Message}");
            }
        }

        private static string ResolveAutosaveBaseUrl()
        {
            var config = RoobsWorld.Multiplayer.RelayConfigService.LoadFromResources();
            if (config?.officialServers == null || config.officialServers.Length == 0)
                return null;

            string relayUrl = null;
            foreach (var server in config.officialServers)
            {
                if (!string.IsNullOrWhiteSpace(server?.relayUrl))
                {
                    relayUrl = server.relayUrl;
                    break;
                }

                var urls = server?.relayUrls;
                if (urls != null && urls.Length > 0 && !string.IsNullOrWhiteSpace(urls[0]))
                {
                    relayUrl = urls[0];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(relayUrl))
                return null;

            if (relayUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                return "https://" + relayUrl.Substring("wss://".Length);
            if (relayUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
                return "http://" + relayUrl.Substring("ws://".Length);
            return relayUrl;
        }

        private static string ResolveGameToken()
        {
            var config = RoobsWorld.Multiplayer.RelayConfigService.LoadFromResources();
            if (config?.officialServers == null)
                return null;

            foreach (var server in config.officialServers)
            {
                if (!string.IsNullOrWhiteSpace(server?.gameToken))
                    return server.gameToken;
            }

            return null;
        }

        private static string ResolvePlayerId()
        {
            string platformUserId = GameManager.HasInstance ? GameManager.Instance.PlatformUser?.UserId : null;
            if (!string.IsNullOrWhiteSpace(platformUserId))
                return platformUserId;

            string device = SystemInfo.deviceUniqueIdentifier;
            return string.IsNullOrWhiteSpace(device) ? "unknown-player" : device;
        }

        [Serializable]
        private sealed class AutosaveEnvelope
        {
            public string slot;
            public SaveData save;
        }

        [Serializable]
        private sealed class AutosaveLookupResponse
        {
            public bool ok;
            public AutosaveEnvelope data;
        }
    }
}
