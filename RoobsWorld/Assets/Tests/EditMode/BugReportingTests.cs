using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests.EditMode
{
    public class BugReportingTests
    {
        [SetUp]
        public void SetUp()
        {
            BugReporting.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            BugReporting.ResetForTests();
        }

        [Test]
        public void Submit_RejectsBlankDescription()
        {
            bool submitted = BugReporting.Submit(new BugReportRequest("  "), out string error);

            Assert.IsFalse(submitted);
            Assert.AreEqual("Describe the bug first.", error);
        }

        [Test]
        public void Submit_ReturnsUnavailable_WhenNoServiceRegistered()
        {
            bool submitted = BugReporting.Submit(new BugReportRequest("The player clipped through terrain."), out string error);

            Assert.IsFalse(submitted);
            Assert.AreEqual("Bug reporting is not configured.", error);
        }

        [Test]
        public void Submit_UsesRegisteredServiceAndTrimsInput()
        {
            var service = new FakeBugReportService();
            BugReporting.Register(service);

            bool submitted = BugReporting.Submit(
                new BugReportRequest("  Crash after opening map.  ", "  playtester@example.com  "),
                out string error);

            Assert.IsTrue(submitted);
            Assert.IsNull(error);
            Assert.AreEqual("Crash after opening map.", service.LastRequest.Description);
            Assert.AreEqual("playtester@example.com", service.LastRequest.Email);
        }

        private sealed class FakeBugReportService : IBugReportService
        {
            public BugReportRequest LastRequest;

            public bool IsAvailable => true;

            public bool Submit(BugReportRequest request, out string error)
            {
                LastRequest = request;
                error = null;
                return true;
            }
        }
    }
}
