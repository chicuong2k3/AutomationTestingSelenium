namespace TestManagementAutomationTest
{
    public class AddTestPlanData
    {
        public string TestPlanName { get; set; } = string.Empty;
        public string ReleaseId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Description { get; set; }
    }
}
