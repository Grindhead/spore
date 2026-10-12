using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Talks to the relay's progress API. Each install registers once with the shared
    /// game token and receives a per-player credential; every save request authenticates
    /// with that credential, and the server derives the storage key from it.
    /// All network calls are asynchronous; nothing here blocks the main thread.
    /// </summary>
    public static class ServerProgressAutosaveClient
    {
        private const float MinUploadIntervalSeconds = 10f;
        private const string CredentialFileName = "progress-credential.txt";

        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly SemaphoreSlim CredentialLock = new SemaphoreSlim(1, 1);
        private static string _cachedCredential;
        private static float _lastUploadRealtime = -999f;

        public enum CloudStatus
        {
            /// <summary>No server configured, offline, timed out, cancelled or failed.</summary>
            Unavailable,
            /// <summary>The server has never stored this slot.</summary>
            Missing,
            /// <summary>The slot was deleted; see <see cref="CloudSlotResult.DeletedAtUtc"/>.</summary>
            Deleted,
            Ok
        }

        public sealed class CloudSlotResult
        {
            public CloudStatus Status;
            public SaveData Save;
            public DateTime? DeletedAtUtc;

            public static CloudSlotResult Unavailable => new CloudSlotResult { Status = CloudStatus.Unavailable };
        }

        /// <summary>
        /// Throttled, fire-and-forget upload of <paramref name="data"/> to <paramref name="slotName"/>.
        /// Local saves are always written first, so a skipped/failed upload only means the
        /// cloud copy is older; <see cref="SaveManager.LoadAsync"/> picks the newest copy.
        /// </summary>
        public static void TryUpload(SaveData data, string slotName)
        {
            if (data == null)
                return;

            if (Time.realtimeSinceStartup - _lastUploadRealtime < MinUploadIntervalSeconds)
                return;

            if (!TryResolveEndpoint(out var endpoint))
                return;

            _lastUploadRealtime = Time.realtimeSinceStartup;
            string slot = NormalizeSlot(slotName);
            string payload = JsonUtility.ToJson(new AutosaveEnvelope { slot = slot, save = data });

            _ = RunLoggedAsync("upload", () => UploadAsync(endpoint, slot, payload, CancellationToken.None));
        }

        /// <summary>Fire-and-forget cloud delete; failures are logged and retried by the next load.</summary>
        public static void TryDelete(string slotName)
        {
            if (!TryResolveEndpoint(out var endpoint))
                return;

            string slot = NormalizeSlot(slotName);
            _ = RunLoggedAsync("delete", () => DeleteAsync(endpoint, slot, CancellationToken.None));
        }

        /// <summary>Fetches a slot from the cloud without blocking. Never throws.</summary>
        public static async Task<CloudSlotResult> TryLoadAsync(string slotName, CancellationToken ct)
        {
            if (!TryResolveEndpoint(out var endpoint))
                return CloudSlotResult.Unavailable;

            string slot = NormalizeSlot(slotName);
            try
            {
                string credential = await GetCredentialAsync(endpoint, ct);
                if (string.IsNullOrEmpty(credential))
                    return CloudSlotResult.Unavailable;

                using var req = new HttpRequestMessage(HttpMethod.Get, SlotUrl(endpoint, slot));
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credential);
                using HttpResponseMessage response = await Client.SendAsync(req, ct);
                string json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    var missing = JsonUtility.FromJson<ProgressError>(json);
                    if (missing != null && missing.reason == "deleted")
                    {
                        return new CloudSlotResult
                        {
                            Status = CloudStatus.Deleted,
                            DeletedAtUtc = SaveCandidateSelector.ParseTimestamp(missing.deletedAt)
                        };
                    }

                    return new CloudSlotResult { Status = CloudStatus.Missing };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    ForgetCredential(endpoint);

                if (!response.IsSuccessStatusCode)
                    return CloudSlotResult.Unavailable;

                var wrapper = JsonUtility.FromJson<AutosaveLookupResponse>(json);
                if (wrapper == null || !wrapper.ok || wrapper.data == null || wrapper.data.save == null)
                    return CloudSlotResult.Unavailable;

                return new CloudSlotResult { Status = CloudStatus.Ok, Save = wrapper.data.save };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return CloudSlotResult.Unavailable;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Server load error: {ex.Message}");
                return CloudSlotResult.Unavailable;
            }
        }

        private static async Task UploadAsync(Endpoint endpoint, string slot, string payload, CancellationToken ct)
        {
            string credential = await GetCredentialAsync(endpoint, ct);
            if (string.IsNullOrEmpty(credential))
                return;

            using var req = new HttpRequestMessage(HttpMethod.Put, SlotUrl(endpoint, slot));
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credential);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await Client.SendAsync(req, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                ForgetCredential(endpoint);
            if (!response.IsSuccessStatusCode)
                Debug.LogWarning($"[Autosave] Server autosave failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        private static async Task DeleteAsync(Endpoint endpoint, string slot, CancellationToken ct)
        {
            string credential = await GetCredentialAsync(endpoint, ct);
            if (string.IsNullOrEmpty(credential))
                return;

            using var req = new HttpRequestMessage(HttpMethod.Delete, SlotUrl(endpoint, slot));
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credential);
            using HttpResponseMessage response = await Client.SendAsync(req, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                ForgetCredential(endpoint);
            if (!response.IsSuccessStatusCode)
                Debug.LogWarning($"[Autosave] Server delete failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        private static async Task RunLoggedAsync(string operation, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Server {operation} error: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns this install's per-player credential, registering one with the
        /// shared game token on first use. The game token alone never grants save access.
        /// </summary>
        private static async Task<string> GetCredentialAsync(Endpoint endpoint, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(_cachedCredential))
                return _cachedCredential;

            await CredentialLock.WaitAsync(ct);
            try
            {
                if (!string.IsNullOrEmpty(_cachedCredential))
                    return _cachedCredential;

                string stored = ReadCredentialFile(endpoint.CredentialPath);
                if (!string.IsNullOrEmpty(stored))
                    return _cachedCredential = stored;

                if (string.IsNullOrWhiteSpace(endpoint.GameToken))
                    return null;

                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint.BaseUrl + "/api/progress/register");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", endpoint.GameToken);
                using HttpResponseMessage response = await Client.SendAsync(req, ct);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"[Autosave] Progress registration failed: {(int)response.StatusCode}");
                    return null;
                }

                var registered = JsonUtility.FromJson<RegisterResponse>(await response.Content.ReadAsStringAsync());
                if (registered == null || !registered.ok || string.IsNullOrEmpty(registered.credential))
                    return null;

                WriteCredentialFile(endpoint.CredentialPath, registered.credential);
                return _cachedCredential = registered.credential;
            }
            finally
            {
                CredentialLock.Release();
            }
        }

        private static void ForgetCredential(Endpoint endpoint)
        {
            _cachedCredential = null;
            try
            {
                if (File.Exists(endpoint.CredentialPath))
                    File.Delete(endpoint.CredentialPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Could not clear rejected credential: {ex.Message}");
            }
        }

        private static string ReadCredentialFile(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteCredentialFile(string path, string credential)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, credential);
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temporary, path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Autosave] Could not persist progress credential: {ex.Message}");
            }
        }

        private static string NormalizeSlot(string slotName)
        {
            return string.IsNullOrWhiteSpace(slotName) ? "autosave" : slotName.Trim().ToLowerInvariant();
        }

        private static string SlotUrl(Endpoint endpoint, string slot)
        {
            return endpoint.BaseUrl + "/api/progress/slots/" + Uri.EscapeDataString(slot);
        }

        /// <summary>Resolved on the main thread (Unity APIs) before any await.</summary>
        private readonly struct Endpoint
        {
            public readonly string BaseUrl;
            public readonly string GameToken;
            public readonly string CredentialPath;

            public Endpoint(string baseUrl, string gameToken, string credentialPath)
            {
                BaseUrl = baseUrl;
                GameToken = gameToken;
                CredentialPath = credentialPath;
            }
        }

        private static bool TryResolveEndpoint(out Endpoint endpoint)
        {
            endpoint = default;
            var config = RoobsWorld.Multiplayer.RelayConfigService.LoadFromResources();
            string baseUrl = ResolveAutosaveBaseUrl(config);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return false;

            string credentialPath = Path.Combine(Application.persistentDataPath, CredentialFileName);
            endpoint = new Endpoint(baseUrl.TrimEnd('/'), ResolveGameToken(config), credentialPath);
            return true;
        }

        private static string ResolveAutosaveBaseUrl(RoobsWorld.Multiplayer.RelayConfigService.RelayConfig config)
        {
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

        private static string ResolveGameToken(RoobsWorld.Multiplayer.RelayConfigService.RelayConfig config)
        {
            if (config?.officialServers == null)
                return null;

            foreach (var server in config.officialServers)
            {
                if (!string.IsNullOrWhiteSpace(server?.gameToken))
                    return server.gameToken;
            }

            return null;
        }

        [Serializable]
        private sealed class AutosaveEnvelope
        {
            public string slot;
            public string savedAt;
            public SaveData save;
        }

        [Serializable]
        private sealed class AutosaveLookupResponse
        {
            public bool ok;
            public AutosaveEnvelope data;
        }

        [Serializable]
        private sealed class ProgressError
        {
            public bool ok;
            public string reason;
            public string deletedAt;
        }

        [Serializable]
        private sealed class RegisterResponse
        {
            public bool ok;
            public string playerId;
            public string credential;
        }
    }
}
