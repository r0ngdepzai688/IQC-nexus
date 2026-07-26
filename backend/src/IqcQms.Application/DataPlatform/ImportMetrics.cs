using System;
using System.Diagnostics.Metrics;

namespace IqcQms.Application.DataPlatform;

public static class ImportMetrics
{
    public const string MeterName = "IqcQms.ImportPlatform";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> ImportsCreated = Meter.CreateCounter<long>("imports_created_total", "Count", "Total import jobs created");
    public static readonly Counter<long> ImportsCompleted = Meter.CreateCounter<long>("imports_completed_total", "Count", "Total import jobs completed");
    public static readonly Counter<long> ImportsFailed = Meter.CreateCounter<long>("imports_failed_total", "Count", "Total import jobs failed");
    public static readonly Counter<long> MappingsCompleted = Meter.CreateCounter<long>("mappings_completed_total", "Count", "Total mappings executed");
    public static readonly Counter<long> ValidationsCompleted = Meter.CreateCounter<long>("validations_completed_total", "Count", "Total validations executed");
    public static readonly Counter<long> PreviewsGenerated = Meter.CreateCounter<long>("previews_generated_total", "Count", "Total previews generated");
    public static readonly Counter<long> PreviewsInvalidated = Meter.CreateCounter<long>("previews_invalidated_total", "Count", "Total previews invalidated");
    public static readonly Counter<long> CommitRequests = Meter.CreateCounter<long>("commit_requests_total", "Count", "Total commit requests received");
    public static readonly Counter<long> CommitSuccess = Meter.CreateCounter<long>("commit_success_total", "Count", "Total commits succeeded");
    public static readonly Counter<long> CommitFailure = Meter.CreateCounter<long>("commit_failure_total", "Count", "Total commits failed");
    public static readonly Counter<long> CommitReplay = Meter.CreateCounter<long>("commit_replay_total", "Count", "Total idempotent commit replays");
    public static readonly Counter<long> CommitRollback = Meter.CreateCounter<long>("commit_rollback_total", "Count", "Total commit transactions rolled back");
    public static readonly Counter<long> ConcurrencyConflicts = Meter.CreateCounter<long>("concurrency_conflicts_total", "Count", "Total concurrency conflicts");
    public static readonly Counter<long> BackgroundRetries = Meter.CreateCounter<long>("background_retries_total", "Count", "Total background task retries");
    public static readonly Counter<long> PoisonWorkItems = Meter.CreateCounter<long>("poison_work_items_total", "Count", "Total poison work items detected");
    public static readonly Counter<long> OutboxDispatch = Meter.CreateCounter<long>("outbox_dispatch_total", "Count", "Total outbox messages dispatched");
    public static readonly Counter<long> OutboxFailure = Meter.CreateCounter<long>("outbox_failure_total", "Count", "Total outbox message failures");

    public static readonly Histogram<double> MappingDurationMs = Meter.CreateHistogram<double>("mapping_duration_ms", "ms", "Mapping duration in ms");
    public static readonly Histogram<double> ValidationDurationMs = Meter.CreateHistogram<double>("validation_duration_ms", "ms", "Validation duration in ms");
    public static readonly Histogram<double> PreviewDurationMs = Meter.CreateHistogram<double>("preview_duration_ms", "ms", "Preview generation duration in ms");
    public static readonly Histogram<double> CommitDurationMs = Meter.CreateHistogram<double>("commit_duration_ms", "ms", "Commit duration in ms");
    public static readonly Histogram<long> ImportedRecordCount = Meter.CreateHistogram<long>("imported_record_count", "Records", "Number of imported records");
}
