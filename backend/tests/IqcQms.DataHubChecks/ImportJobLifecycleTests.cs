using IqcQms.Application.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class ImportJobLifecycleTests
{
    [Fact]
    public void UnsupportedProtocolVersionUsesStableSanitizedError()
    {
        var exception = Assert.Throws<ImportPlatformException>(() =>
            NormalizedWorkbookValidation.EnsureProtocolSupported("2.0"));

        Assert.Equal(ImportErrorCodes.ProtocolUnsupported, exception.Code);
        Assert.Equal("The normalized workbook protocol version is not supported.", exception.Message);
    }

    [Fact]
    public void PreviewIsRequiredAndCommitReceiptIsIdempotent()
    {
        var job = new ImportJob("synthetic-job", "synthetic-owner");
        job.TransitionTo(ImportJobState.Inspecting);
        job.TransitionTo(ImportJobState.ReadyForMapping);
        job.TransitionTo(ImportJobState.Validating);

        var exception = Assert.Throws<ImportPlatformException>(() =>
            job.BeginCommit("missing-preview", "synthetic-key"));
        Assert.Equal(ImportErrorCodes.PreviewRequired, exception.Code);

        job.MarkPreviewReady("preview-v1");
        job.BeginCommit("preview-v1", "synthetic-key");
        var first = job.CompleteCommit("synthetic-key", 2, 1, 0);
        var replay = job.CompleteCommit("synthetic-key", 2, 1, 0);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Inserted, replay.Inserted);
        Assert.Equal(first.Updated, replay.Updated);
        Assert.Equal(ImportJobState.Completed, job.State);
    }

    [Fact]
    public void JobSupportsCancellationAndPropagatesCancellationToken()
    {
        var job = new ImportJob("synthetic-job", "synthetic-owner");
        job.Cancel();
        Assert.Equal(ImportJobState.Cancelled, job.State);

        var second = new ImportJob("synthetic-job-2", "synthetic-owner");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            second.TransitionTo(ImportJobState.Inspecting, cancellation.Token));
    }

    [Fact]
    public void InvalidAggregateTransitionUsesStableErrorCode()
    {
        var job = new ImportJob("synthetic-job", "synthetic-owner");

        var exception = Assert.Throws<ImportPlatformException>(() =>
            job.MarkPreviewReady("preview-v1"));

        Assert.Equal("IMPORT_INVALID_TRANSITION", exception.Code);
        Assert.Equal("The import job transition is not allowed.", exception.Message);
    }
}
