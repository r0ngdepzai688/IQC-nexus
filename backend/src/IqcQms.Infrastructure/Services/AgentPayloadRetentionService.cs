using IqcQms.Application.Services;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Config;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IqcQms.Infrastructure.Services;

public class AgentPayloadRetentionService : IAgentPayloadRetentionService
{
    private readonly AppDbContext _db;
    private readonly AgentPayloadRetentionOptions _options;
    private readonly IRelationalConstraintViolationClassifier _classifier;
    private readonly ILogger<AgentPayloadRetentionService> _logger;

    public AgentPayloadRetentionService(
        AppDbContext db,
        IOptions<AgentPayloadRetentionOptions> options,
        IRelationalConstraintViolationClassifier classifier,
        ILogger<AgentPayloadRetentionService> logger)
    {
        _db = db;
        _options = options.Value;
        _options.Validate();
        _classifier = classifier;
        _logger = logger;
    }

    public AgentPayloadRetentionService(AppDbContext db, ILogger<AgentPayloadRetentionService> logger)
        : this(db, Options.Create(new AgentPayloadRetentionOptions()), new RelationalConstraintViolationClassifier(), logger)
    {
    }

    public async Task<int> CleanupExpiredSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_options.FullResultRetentionDays);
        var expiredSubmissions = await _db.AgentPayloadSubmissions
            .Where(s => s.CreatedAtUtc < cutoff)
            .OrderBy(s => s.CreatedAtUtc)
            .Take(_options.CleanupBatchSize)
            .ToListAsync(cancellationToken);

        int cleanedCount = 0;

        foreach (var sub in expiredSubmissions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var tombstoneExpiry = sub.CreatedAtUtc.AddDays(_options.ReplayTombstoneRetentionDays);

                var existingTombstone = await _db.AgentPayloadReplayTombstones
                    .FirstOrDefaultAsync(t => t.AgentDeviceId == sub.AgentDeviceId && t.PayloadSubmissionId == sub.PayloadSubmissionId, cancellationToken);

                if (existingTombstone == null)
                {
                    var tombstone = new AgentPayloadReplayTombstone
                    {
                        Id = Guid.NewGuid(),
                        AgentDeviceId = sub.AgentDeviceId,
                        DeviceId = sub.DeviceId,
                        PayloadSubmissionId = sub.PayloadSubmissionId,
                        Nonce = sub.Nonce,
                        CanonicalPayloadHash = sub.CanonicalPayloadHash,
                        AcceptedAtUtc = sub.CreatedAtUtc,
                        TombstoneExpiresAtUtc = tombstoneExpiry
                    };

                    _db.AgentPayloadReplayTombstones.Add(tombstone);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                // Delete full submission record after tombstone creation is persisted
                _db.AgentPayloadSubmissions.Remove(sub);
                await _db.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
                cleanedCount++;
                _logger.LogInformation("Archived submission {SubmissionId} to replay tombstone and cleaned full record.", sub.PayloadSubmissionId);
            }
            catch (DbUpdateException ex) when (_classifier.IsUniqueConstraintViolation(ex))
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogWarning("Concurrent cleanup race for submission {SubmissionId}; verifying tombstone existence before removal.", sub.PayloadSubmissionId);

                using var retryTx = await _db.Database.BeginTransactionAsync(cancellationToken);
                var tombstone = await _db.AgentPayloadReplayTombstones
                    .FirstOrDefaultAsync(t => t.AgentDeviceId == sub.AgentDeviceId && t.PayloadSubmissionId == sub.PayloadSubmissionId, cancellationToken);

                if (tombstone != null)
                {
                    var targetSub = await _db.AgentPayloadSubmissions
                        .FirstOrDefaultAsync(s => s.Id == sub.Id, cancellationToken);
                    if (targetSub != null)
                    {
                        _db.AgentPayloadSubmissions.Remove(targetSub);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    await retryTx.CommitAsync(cancellationToken);
                    cleanedCount++;
                }
                else
                {
                    await retryTx.RollbackAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cleanup submission {SubmissionId}; full record left authoritative intact.", sub.PayloadSubmissionId);
            }
        }

        return cleanedCount;
    }

    public async Task<int> CleanupExpiredTombstonesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredTombstones = await _db.AgentPayloadReplayTombstones
            .Where(t => t.TombstoneExpiresAtUtc < now)
            .OrderBy(t => t.TombstoneExpiresAtUtc)
            .Take(_options.CleanupBatchSize)
            .ToListAsync(cancellationToken);

        if (expiredTombstones.Count > 0)
        {
            _db.AgentPayloadReplayTombstones.RemoveRange(expiredTombstones);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} expired replay tombstones past replay retention window.", expiredTombstones.Count);
        }

        return expiredTombstones.Count;
    }

    public async Task<int> CleanupExpiredRefreshOperationResultsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredEnvelopes = await _db.AgentRefreshOperationResults
            .Where(r => r.ExpiresAtUtc < now)
            .OrderBy(r => r.ExpiresAtUtc)
            .Take(_options.CleanupBatchSize)
            .ToListAsync(cancellationToken);

        if (expiredEnvelopes.Count > 0)
        {
            _db.AgentRefreshOperationResults.RemoveRange(expiredEnvelopes);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Purged {Count} expired refresh operation recovery envelopes.", expiredEnvelopes.Count);
        }

        return expiredEnvelopes.Count;
    }
}
