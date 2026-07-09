namespace Evidata.Worker.Outbox.Messaging;

public static class LogicalDestinations
{
    public const string DocumentProcessing = "document-processing";
    public const string SearchIndexing = "search-indexing";
    public const string ReportGeneration = "report-generation";
    public const string NotificationDelivery = "notification-delivery";
    public const string McpBatch = "mcp-batch";
    public const string SecurityJobs = "security-jobs";
    public const string MaintenanceJobs = "maintenance-jobs";
}
