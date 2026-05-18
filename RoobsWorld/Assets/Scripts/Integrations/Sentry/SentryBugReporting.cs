using System;
using RoobsWorld.Core;
using UnityEngine;
using UnitySentrySdk = Sentry.Unity.SentrySdk;

namespace RoobsWorld.Integrations.Sentry
{
    public static class SentryBugReporting
    {
        private static readonly SentryBugReportService Service = new SentryBugReportService();
        private static bool _initialized;
        private static bool _initializing;

        public static bool IsReady => _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            BugReporting.Register(Service);
            TryInitialize();
        }

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;
            if (_initializing)
                return false;

            string[] args = SafeGetCommandLineArgs();
            if (!SentryBugReportingConfig.IsEnabled(GetEnvironmentVariable, args, Application.isEditor))
                return false;

            string dsn = SentryBugReportingConfig.ResolveDsn(GetEnvironmentVariable, args);
            if (string.IsNullOrWhiteSpace(dsn))
                return false;

            string environment = SentryBugReportingConfig.ResolveEnvironment(
                GetEnvironmentVariable,
                args,
                Application.isEditor,
                Debug.isDebugBuild);
            string release = SentryBugReportingConfig.ResolveRelease(
                GetEnvironmentVariable,
                args,
                Application.productName,
                Application.version);
            bool debug = SentryBugReportingConfig.ResolveDebug(GetEnvironmentVariable, args, Debug.isDebugBuild);

            _initializing = true;
            try
            {
                UnitySentrySdk.Init(options =>
                {
                    options.Dsn = dsn;
                    options.Environment = environment;
                    options.Release = release;
                    options.Debug = debug;
                    options.CaptureInEditor = Application.isEditor;
                    options.CaptureLogErrorEvents = true;
                    options.SendDefaultPii = false;
                    options.AttachScreenshot = false;
                    options.AttachViewHierarchy = false;
                    options.SetBeforeSend(@event =>
                    {
                        @event.SetTag("game", Application.productName);
                        @event.SetTag("unity_platform", Application.platform.ToString());

                        if (!string.IsNullOrWhiteSpace(Application.buildGUID))
                            @event.SetTag("build_guid", Application.buildGUID);

                        return @event;
                    });
                });

                _initialized = true;
                Application.quitting += Shutdown;
                Debug.Log($"[Sentry] Bug reporting enabled for {environment} ({release}).");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Sentry] Bug reporting failed to initialize: {ex.Message}");
                return false;
            }
            finally
            {
                _initializing = false;
            }
        }

        public static void CaptureException(Exception exception)
        {
            if (exception == null)
                return;

            if (!_initialized && !TryInitialize())
                return;

            UnitySentrySdk.CaptureException(exception);
        }

        public static void CaptureMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!_initialized && !TryInitialize())
                return;

            UnitySentrySdk.CaptureMessage(message);
        }

        public static bool CaptureFeedback(BugReportRequest request, out string error)
        {
            error = null;

            if (!_initialized && !TryInitialize())
            {
                error = "Bug reporting is not configured.";
                return false;
            }

            try
            {
                UnitySentrySdk.CaptureFeedback(request.Description, request.Email, request.Name, request.AttachScreenshot);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Could not send report: {ex.Message}";
                return false;
            }
        }

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
            UnitySentrySdk.Close();
            _initialized = false;
        }

        private static string[] SafeGetCommandLineArgs()
        {
            try
            {
                return Environment.GetCommandLineArgs();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string GetEnvironmentVariable(string name)
        {
            try
            {
                return Environment.GetEnvironmentVariable(name);
            }
            catch
            {
                return null;
            }
        }

        private sealed class SentryBugReportService : IBugReportService
        {
            public bool IsAvailable => SentryBugReporting.IsReady || SentryBugReporting.TryInitialize();

            public bool Submit(BugReportRequest request, out string error)
            {
                return SentryBugReporting.CaptureFeedback(request, out error);
            }
        }
    }
}
