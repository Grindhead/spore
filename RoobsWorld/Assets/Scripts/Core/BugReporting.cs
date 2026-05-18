namespace RoobsWorld.Core
{
    public struct BugReportRequest
    {
        public string Description;
        public string Email;
        public string Name;
        public bool AttachScreenshot;

        public BugReportRequest(string description, string email = null, string name = null, bool attachScreenshot = true)
        {
            Description = description;
            Email = email;
            Name = name;
            AttachScreenshot = attachScreenshot;
        }
    }

    public interface IBugReportService
    {
        bool IsAvailable { get; }

        bool Submit(BugReportRequest request, out string error);
    }

    public static class BugReporting
    {
        private static readonly IBugReportService DisabledService = new DisabledBugReportService();
        private static IBugReportService _service = DisabledService;

        public static bool IsAvailable => _service != null && _service.IsAvailable;

        public static void Register(IBugReportService service)
        {
            _service = service ?? DisabledService;
        }

        public static bool Submit(BugReportRequest request, out string error)
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                error = "Describe the bug first.";
                return false;
            }

            if (!IsAvailable)
            {
                error = "Bug reporting is not configured.";
                return false;
            }

            request.Description = request.Description.Trim();
            request.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            request.Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();

            return _service.Submit(request, out error);
        }

        public static void ResetForTests()
        {
            _service = DisabledService;
        }

        private sealed class DisabledBugReportService : IBugReportService
        {
            public bool IsAvailable => false;

            public bool Submit(BugReportRequest request, out string error)
            {
                error = "Bug reporting is not configured.";
                return false;
            }
        }
    }
}
