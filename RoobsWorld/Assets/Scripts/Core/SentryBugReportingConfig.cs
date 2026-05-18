using System;
using System.Collections.Generic;
using System.Text;

namespace RoobsWorld.Core
{
    public static class SentryBugReportingConfig
    {
        public const string PrimaryDsnEnvironmentVariable = "ROOBSWORLD_SENTRY_DSN";
        public const string FallbackDsnEnvironmentVariable = "SENTRY_DSN";
        public const string PrimaryEnvironmentVariable = "ROOBSWORLD_SENTRY_ENVIRONMENT";
        public const string FallbackEnvironmentVariable = "SENTRY_ENVIRONMENT";
        public const string PrimaryReleaseEnvironmentVariable = "ROOBSWORLD_SENTRY_RELEASE";
        public const string FallbackReleaseEnvironmentVariable = "SENTRY_RELEASE";
        public const string PrimaryEnabledEnvironmentVariable = "ROOBSWORLD_SENTRY_ENABLED";
        public const string PrimaryEditorEnvironmentVariable = "ROOBSWORLD_SENTRY_CAPTURE_IN_EDITOR";
        public const string PrimaryDebugEnvironmentVariable = "ROOBSWORLD_SENTRY_DEBUG";

        private static readonly string[] DsnArgumentNames = { "--sentry-dsn", "-sentry-dsn" };
        private static readonly string[] EnvironmentArgumentNames = { "--sentry-environment", "-sentry-environment" };
        private static readonly string[] ReleaseArgumentNames = { "--sentry-release", "-sentry-release" };
        private static readonly string[] EnabledArgumentNames = { "--sentry-enabled", "-sentry-enabled" };
        private static readonly string[] EditorArgumentNames = { "--sentry-capture-in-editor", "-sentry-capture-in-editor" };
        private static readonly string[] DebugArgumentNames = { "--sentry-debug", "-sentry-debug" };

        public static bool IsEnabled(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs,
            bool isEditor)
        {
            bool enabled = ResolveOptionalBoolean(
                getEnvironmentVariable,
                commandLineArgs,
                true,
                new[] { PrimaryEnabledEnvironmentVariable, "SENTRY_ENABLED" },
                EnabledArgumentNames);

            if (!enabled)
                return false;

            if (!isEditor)
                return true;

            return ResolveOptionalBoolean(
                getEnvironmentVariable,
                commandLineArgs,
                false,
                new[] { PrimaryEditorEnvironmentVariable, "SENTRY_CAPTURE_IN_EDITOR" },
                EditorArgumentNames);
        }

        public static string ResolveDsn(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs)
        {
            return FirstNonEmpty(
                ReadCommandLineOption(commandLineArgs, DsnArgumentNames),
                ReadEnvironmentVariable(getEnvironmentVariable, PrimaryDsnEnvironmentVariable),
                ReadEnvironmentVariable(getEnvironmentVariable, FallbackDsnEnvironmentVariable));
        }

        public static string ResolveEnvironment(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs,
            bool isEditor,
            bool isDebugBuild)
        {
            string fallback = isEditor ? "editor" : isDebugBuild ? "development" : "production";
            string configured = FirstNonEmpty(
                ReadCommandLineOption(commandLineArgs, EnvironmentArgumentNames),
                ReadEnvironmentVariable(getEnvironmentVariable, PrimaryEnvironmentVariable),
                ReadEnvironmentVariable(getEnvironmentVariable, FallbackEnvironmentVariable));

            return NormalizeEnvironment(configured, fallback);
        }

        public static string ResolveRelease(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs,
            string productName,
            string applicationVersion)
        {
            return FirstNonEmpty(
                ReadCommandLineOption(commandLineArgs, ReleaseArgumentNames),
                ReadEnvironmentVariable(getEnvironmentVariable, PrimaryReleaseEnvironmentVariable),
                ReadEnvironmentVariable(getEnvironmentVariable, FallbackReleaseEnvironmentVariable),
                BuildDefaultRelease(productName, applicationVersion));
        }

        public static bool ResolveDebug(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs,
            bool defaultValue)
        {
            return ResolveOptionalBoolean(
                getEnvironmentVariable,
                commandLineArgs,
                defaultValue,
                new[] { PrimaryDebugEnvironmentVariable, "SENTRY_DEBUG" },
                DebugArgumentNames);
        }

        public static string BuildDefaultRelease(string productName, string applicationVersion)
        {
            string safeProduct = SanitizeReleasePart(productName, "RoobsWorld");
            string safeVersion = SanitizeReleasePart(applicationVersion, "0.0.0");
            return $"{safeProduct}@{safeVersion}";
        }

        public static string NormalizeEnvironment(string environment, string fallback)
        {
            string normalizedFallback = string.IsNullOrWhiteSpace(fallback) ? "production" : fallback.Trim();
            if (string.IsNullOrWhiteSpace(environment))
                return normalizedFallback;

            var builder = new StringBuilder(environment.Trim().Length);
            foreach (char c in environment.Trim())
            {
                builder.Append(c == '/' || char.IsWhiteSpace(c) ? '-' : c);
            }

            string normalized = builder.ToString().Trim('-');
            if (string.IsNullOrWhiteSpace(normalized) ||
                string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFallback;
            }

            return normalized.Length <= 64 ? normalized : normalized.Substring(0, 64);
        }

        private static bool ResolveOptionalBoolean(
            Func<string, string> getEnvironmentVariable,
            IReadOnlyList<string> commandLineArgs,
            bool defaultValue,
            IReadOnlyList<string> environmentVariableNames,
            IReadOnlyList<string> argumentNames)
        {
            string argumentValue = ReadCommandLineOption(commandLineArgs, argumentNames);
            if (TryParseBoolean(argumentValue, out bool parsedArgument))
                return parsedArgument;

            foreach (string name in environmentVariableNames)
            {
                if (TryParseBoolean(ReadEnvironmentVariable(getEnvironmentVariable, name), out bool parsedEnvironment))
                    return parsedEnvironment;
            }

            return defaultValue;
        }

        private static bool TryParseBoolean(string value, out bool parsed)
        {
            parsed = false;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    parsed = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    parsed = false;
                    return true;
                default:
                    return false;
            }
        }

        private static string ReadCommandLineOption(IReadOnlyList<string> args, IReadOnlyList<string> names)
        {
            if (args == null)
                return null;

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                foreach (string name in names)
                {
                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
                        return args[i + 1];

                    string prefix = name + "=";
                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return arg.Substring(prefix.Length);
                }
            }

            return null;
        }

        private static string ReadEnvironmentVariable(Func<string, string> getEnvironmentVariable, string name)
        {
            if (getEnvironmentVariable == null || string.IsNullOrWhiteSpace(name))
                return null;

            try
            {
                return getEnvironmentVariable(name);
            }
            catch
            {
                return null;
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private static string SanitizeReleasePart(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim().Replace("/", "_").Replace("\\", "_").Replace("@", "_");
        }
    }
}
