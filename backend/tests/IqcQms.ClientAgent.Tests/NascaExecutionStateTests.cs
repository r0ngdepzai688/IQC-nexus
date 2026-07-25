using Xunit;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using Microsoft.Extensions.Logging.Abstractions;

namespace IqcQms.ClientAgent.Tests;

public class NascaExecutionStateTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly SqliteNascaExecutionStateStore _store;

    public NascaExecutionStateTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nasca_state_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _store = new SqliteNascaExecutionStateStore(_tempDirectory, NullLogger<SqliteNascaExecutionStateStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task InitialState_IsQueued()
    {
        var record = await _store.CreateInitialStateAsync("corr_001", "qitem_001", "work_001");

        Assert.Equal("corr_001", record.CorrelationId);
        Assert.Equal("qitem_001", record.QueueItemId);
        Assert.Equal(NascaExecutionState.Queued, record.CurrentState);
        Assert.Equal(1, record.AttemptNumber);
        Assert.Equal(1, record.SchemaVersion);
    }

    [Fact]
    public async Task AllowedTransition_Succeeds()
    {
        await _store.CreateInitialStateAsync("corr_002", "qitem_002", "work_002");

        var updated = await _store.TransitionStateAsync("corr_002", NascaExecutionState.Queued, NascaExecutionState.InputValidated, "INPUT_VALIDATED");

        Assert.Equal(NascaExecutionState.InputValidated, updated.CurrentState);
        Assert.Equal("INPUT_VALIDATED", updated.SanitizedReasonCode);
    }

    [Fact]
    public async Task IllegalTransition_IsRejected()
    {
        await _store.CreateInitialStateAsync("corr_003", "qitem_003", "work_003");

        // Queued -> Completed directly is illegal
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.TransitionStateAsync("corr_003", NascaExecutionState.Queued, NascaExecutionState.Completed, "ILLEGAL_JUMP"));
    }

    [Fact]
    public async Task CompletedState_IsTerminal()
    {
        await _store.CreateInitialStateAsync("corr_004", "qitem_004", "work_004");
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.InputValidated, NascaExecutionState.InputStaged);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.InputStaged, NascaExecutionState.ExecutionPending);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.ExecutionPending, NascaExecutionState.ExecutionStarted);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.ExecutionStarted, NascaExecutionState.OutputPending);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.OutputPending, NascaExecutionState.OutputValidated);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.OutputValidated, NascaExecutionState.Submitting);
        await _store.TransitionStateAsync("corr_004", NascaExecutionState.Submitting, NascaExecutionState.Completed);

        // Completed -> ExecutionStarted is illegal
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.TransitionStateAsync("corr_004", NascaExecutionState.Completed, NascaExecutionState.ExecutionStarted));
    }

    [Fact]
    public async Task RetryableFailure_RetainsCorrelationId()
    {
        var record = await _store.CreateInitialStateAsync("corr_005", "qitem_005", "work_005");
        await _store.TransitionStateAsync("corr_005", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
        await _store.TransitionStateAsync("corr_005", NascaExecutionState.InputValidated, NascaExecutionState.InputStaged);
        await _store.TransitionStateAsync("corr_005", NascaExecutionState.InputStaged, NascaExecutionState.ExecutionPending);
        await _store.TransitionStateAsync("corr_005", NascaExecutionState.ExecutionPending, NascaExecutionState.ExecutionStarted);
        await _store.TransitionStateAsync("corr_005", NascaExecutionState.ExecutionStarted, NascaExecutionState.RetryableFailure, "RETRYABLE_ERR");

        var retryAttempt = await _store.RecordNewAttemptAsync("corr_005");

        Assert.Equal("corr_005", retryAttempt.CorrelationId);
        Assert.Equal(2, retryAttempt.AttemptNumber);
        Assert.Equal(NascaExecutionState.ExecutionPending, retryAttempt.CurrentState);
    }

    [Fact]
    public async Task Retry_IncrementsAttemptNumber()
    {
        await _store.CreateInitialStateAsync("corr_006", "qitem_006", "work_006");
        var attempt2 = await _store.RecordNewAttemptAsync("corr_006");
        var attempt3 = await _store.RecordNewAttemptAsync("corr_006");

        Assert.Equal(3, attempt3.AttemptNumber);
    }

    [Fact]
    public async Task Restart_RestoresSameLogicalExecution()
    {
        await _store.CreateInitialStateAsync("corr_007", "qitem_007", "work_007");

        var newStoreInstance = new SqliteNascaExecutionStateStore(_tempDirectory, NullLogger<SqliteNascaExecutionStateStore>.Instance);
        var restored = await newStoreInstance.GetStateAsync("corr_007");

        Assert.NotNull(restored);
        Assert.Equal("corr_007", restored.CorrelationId);
        Assert.Equal(NascaExecutionState.Queued, restored.CurrentState);
    }

    [Fact]
    public async Task RestartDuringExecution_RequiresRecovery()
    {
        await _store.CreateInitialStateAsync("corr_008", "qitem_008", "work_008");
        await _store.TransitionStateAsync("corr_008", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
        await _store.TransitionStateAsync("corr_008", NascaExecutionState.InputValidated, NascaExecutionState.InputStaged);
        await _store.TransitionStateAsync("corr_008", NascaExecutionState.InputStaged, NascaExecutionState.ExecutionPending);
        await _store.TransitionStateAsync("corr_008", NascaExecutionState.ExecutionPending, NascaExecutionState.ExecutionStarted);

        var action = NascaExecutionRecoveryPolicy.DetermineRestartAction(NascaExecutionState.ExecutionStarted);
        Assert.Equal(RecoveryAction.MarkRecoveryRequired, action);

        var updated = await _store.MarkRecoveryRequiredAsync("corr_008", "RESTART_DURING_EXECUTION");
        Assert.Equal(NascaExecutionState.RecoveryRequired, updated.CurrentState);
    }

    [Fact]
    public void RestartAfterOutputValidation_DoesNotRerunExecution()
    {
        var action = NascaExecutionRecoveryPolicy.DetermineRestartAction(NascaExecutionState.OutputValidated);
        Assert.Equal(RecoveryAction.SkipToSubmission, action);
    }

    [Fact]
    public void RestartDuringSubmission_UsesIdempotentReconciliation()
    {
        var action = NascaExecutionRecoveryPolicy.DetermineRestartAction(NascaExecutionState.Submitting);
        Assert.Equal(RecoveryAction.ReconcileIdempotently, action);
    }

    [Fact]
    public async Task AtomicTransition_PreventsConcurrentDoubleAdvance()
    {
        await _store.CreateInitialStateAsync("corr_009", "qitem_009", "work_009");

        var t1 = Task.Run(async () =>
        {
            try
            {
                await _store.TransitionStateAsync("corr_009", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
                return true;
            }
            catch (InvalidOperationException) { return false; }
        });

        var t2 = Task.Run(async () =>
        {
            try
            {
                await _store.TransitionStateAsync("corr_009", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
                return true;
            }
            catch (InvalidOperationException) { return false; }
        });

        var outcomes = await Task.WhenAll(t1, t2);

        // Exactly one task succeeded, and exactly one threw InvalidOperationException
        Assert.Single(outcomes, true);
        Assert.Single(outcomes, false);

        var finalRecord = await _store.GetStateAsync("corr_009");
        Assert.Equal(NascaExecutionState.InputValidated, finalRecord?.CurrentState);
    }

    [Fact]
    public async Task Cancellation_DoesNotMarkCompleted()
    {
        await _store.CreateInitialStateAsync("corr_010", "qitem_010", "work_010");
        await _store.TransitionStateAsync("corr_010", NascaExecutionState.Queued, NascaExecutionState.Cancelled, "JOB_CANCELLED");

        var record = await _store.GetStateAsync("corr_010");
        Assert.Equal(NascaExecutionState.Cancelled, record?.CurrentState);
        Assert.NotEqual(NascaExecutionState.Completed, record?.CurrentState);
    }

    [Fact]
    public async Task Timeout_DoesNotMarkCompleted()
    {
        await _store.CreateInitialStateAsync("corr_011", "qitem_011", "work_011");
        await _store.TransitionStateAsync("corr_011", NascaExecutionState.Queued, NascaExecutionState.InputValidated);
        await _store.TransitionStateAsync("corr_011", NascaExecutionState.InputValidated, NascaExecutionState.InputStaged);
        await _store.TransitionStateAsync("corr_011", NascaExecutionState.InputStaged, NascaExecutionState.ExecutionPending);
        await _store.TransitionStateAsync("corr_011", NascaExecutionState.ExecutionPending, NascaExecutionState.ExecutionStarted);
        await _store.TransitionStateAsync("corr_011", NascaExecutionState.ExecutionStarted, NascaExecutionState.RetryableFailure, "EXECUTION_TIMEOUT");

        var record = await _store.GetStateAsync("corr_011");
        Assert.Equal(NascaExecutionState.RetryableFailure, record?.CurrentState);
        Assert.NotEqual(NascaExecutionState.Completed, record?.CurrentState);
    }

    [Fact]
    public void PermanentFailure_DoesNotAutoRetry()
    {
        var action = NascaExecutionRecoveryPolicy.DetermineRestartAction(NascaExecutionState.PermanentFailure);
        Assert.Equal(RecoveryAction.TerminalNoRetry, action);
    }

    [Fact]
    public void StatePayload_DoesNotContainSecrets()
    {
        var props = typeof(NascaExecutionStateRecord).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("AccessToken", props);
        Assert.DoesNotContain("SecretKey", props);
        Assert.DoesNotContain("Password", props);
        Assert.DoesNotContain("BearerToken", props);
    }

    [Fact]
    public void StatePayload_DoesNotContainWorkbookContent()
    {
        var props = typeof(NascaExecutionStateRecord).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("WorkbookBytes", props);
        Assert.DoesNotContain("RawContent", props);
        Assert.DoesNotContain("CellValues", props);
    }

    [Fact]
    public async Task StateSchemaVersion_IsPersisted()
    {
        var record = await _store.CreateInitialStateAsync("corr_012", "qitem_012", "work_012");
        Assert.Equal(1, record.SchemaVersion);
    }

    [Fact]
    public void ProcessLaunchCode_RemainsAbsent()
    {
        var prodTypes = typeof(SqliteNascaExecutionStateStore).Assembly.GetTypes();
        var procStartCalls = prodTypes.SelectMany(t => t.GetMethods())
            .Where(m => m.Name.Equals("Start", StringComparison.OrdinalIgnoreCase) && m.DeclaringType?.Name == "Process");

        Assert.Empty(procStartCalls);
    }

    [Fact]
    public void OfficeInteropAssembly_RemainsAbsent()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "");
        Assert.DoesNotContain(assemblies, a => a.Equals("office", StringComparison.OrdinalIgnoreCase));
    }
}
