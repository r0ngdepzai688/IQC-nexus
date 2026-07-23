using System;
using System.Collections.Generic;

namespace IqcQms.Application.DataPlatform;

public enum FailureInjectionPoint
{
    None,
    BeforeTransactionSave,
    AfterCommittingState,
    AfterTargetRecordInsert,
    BeforeAuditAppend,
    BeforeCompletedTransition
}

public interface IFailureInjector
{
    FailureInjectionPoint InjectionPoint { get; set; }
    void Trigger(FailureInjectionPoint point);
}

public sealed class TestFailureInjector : IFailureInjector
{
    public FailureInjectionPoint InjectionPoint { get; set; } = FailureInjectionPoint.None;

    public void Trigger(FailureInjectionPoint point)
    {
        if (InjectionPoint != FailureInjectionPoint.None && InjectionPoint == point)
        {
            throw new InvalidOperationException($"SIMULATED_COMMIT_FAILURE at point '{point}'. Transaction must roll back.");
        }
    }
}

public sealed record ImportCommitRequest(
    string JobId,
    string IdempotencyKey,
    long ExpectedVersion);

public interface IImportCommitEngine
{
    Task<ImportCommitResult> ExecuteCommitAsync(
        ImportCommitRequest request,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
