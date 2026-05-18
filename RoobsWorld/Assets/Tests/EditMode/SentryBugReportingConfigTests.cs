using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests.EditMode
{
    public class SentryBugReportingConfigTests
    {
        [Test]
        public void ResolveDsn_UsesCommandLineBeforeEnvironment()
        {
            string dsn = SentryBugReportingConfig.ResolveDsn(
                name => name == SentryBugReportingConfig.PrimaryDsnEnvironmentVariable ? "https://env@example.ingest.sentry.io/1" : null,
                new[] { "RoobsWorld.exe", "--sentry-dsn=https://arg@example.ingest.sentry.io/2" });

            Assert.AreEqual("https://arg@example.ingest.sentry.io/2", dsn);
        }

        [Test]
        public void IsEnabled_DisablesEditorReportingByDefault()
        {
            bool enabled = SentryBugReportingConfig.IsEnabled(
                _ => null,
                new[] { "RoobsWorld.exe" },
                isEditor: true);

            Assert.IsFalse(enabled);
        }

        [Test]
        public void IsEnabled_AllowsEditorReportingWhenOptedIn()
        {
            bool enabled = SentryBugReportingConfig.IsEnabled(
                _ => null,
                new[] { "RoobsWorld.exe", "--sentry-capture-in-editor", "true" },
                isEditor: true);

            Assert.IsTrue(enabled);
        }

        [Test]
        public void ResolveEnvironment_NormalizesValuesForSentry()
        {
            string environment = SentryBugReportingConfig.ResolveEnvironment(
                name => name == SentryBugReportingConfig.PrimaryEnvironmentVariable ? "playtest / windows" : null,
                new[] { "RoobsWorld.exe" },
                isEditor: false,
                isDebugBuild: false);

            Assert.AreEqual("playtest---windows", environment);
        }

        [Test]
        public void ResolveRelease_UsesExplicitReleaseBeforeDefault()
        {
            string release = SentryBugReportingConfig.ResolveRelease(
                name => name == SentryBugReportingConfig.FallbackReleaseEnvironmentVariable ? "roobsworld@playtest-42" : null,
                new[] { "RoobsWorld.exe" },
                "RoobsWorld",
                "1.0");

            Assert.AreEqual("roobsworld@playtest-42", release);
        }

        [Test]
        public void ResolveDebug_ParsesEnvironmentToggle()
        {
            bool debug = SentryBugReportingConfig.ResolveDebug(
                name => name == SentryBugReportingConfig.PrimaryDebugEnvironmentVariable ? "off" : null,
                new[] { "RoobsWorld.exe" },
                defaultValue: true);

            Assert.IsFalse(debug);
        }

        [Test]
        public void BuildDefaultRelease_FallsBackWhenProductOrVersionMissing()
        {
            Assert.AreEqual("RoobsWorld@0.0.0", SentryBugReportingConfig.BuildDefaultRelease("", null));
        }
    }
}
