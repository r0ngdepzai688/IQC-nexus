using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportWorkItems")]
public class PersistentImportWorkItem
{
    [Key]
    [MaxLength(64)]
    public string WorkItemId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(64)]
    public string JobId { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string WorkType { get; set; } = "Commit";

    [Required]
    [MaxLength(32)]
    public string State { get; set; } = "Pending"; // Pending, Leased, Completed, Failed, Poison

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 3;

    public DateTimeOffset AvailableAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(128)]
    public string? LeaseOwner { get; set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }

    [Required]
    [MaxLength(64)]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public long ExpectedVersion { get; set; } = 1;

    [Required]
    [MaxLength(128)]
    public string ActorUserId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? LastErrorCode { get; set; }

    [MaxLength(512)]
    public string? LastErrorMessage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
