using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportJobs")]
public class PersistentImportJob
{
    [Key]
    [MaxLength(64)]
    public string JobId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string OwnerUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string State { get; set; } = "Created";

    [Required]
    [MaxLength(64)]
    public string SourceKind { get; set; } = "Csv";

    [Required]
    [MaxLength(256)]
    public string SourceDisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string NormalizedContentFingerprint { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? MappingProfileId { get; set; }

    [MaxLength(32)]
    public string? MappingProfileVersion { get; set; }

    [MaxLength(64)]
    public string? ValidationProfileId { get; set; }

    [MaxLength(32)]
    public string? ValidationProfileVersion { get; set; }

    [MaxLength(128)]
    public string? PreviewFingerprint { get; set; }

    [MaxLength(256)]
    public string? PreviewAttestationSignature { get; set; }

    public DateTimeOffset? PreviewGeneratedAt { get; set; }

    public DateTimeOffset? PreviewExpiresAt { get; set; }

    public bool IsPreviewInvalidated { get; set; }

    [MaxLength(128)]
    public string? PreviewInvalidationReason { get; set; }

    public int MappedRecordCount { get; set; }

    public int WarningCount { get; set; }

    public int ErrorCount { get; set; }

    public int BlockingErrorCount { get; set; }

    public bool IsCommitted { get; set; }

    public DateTimeOffset? CommittedAt { get; set; }

    [MaxLength(128)]
    public string? CommittedBy { get; set; }

    [MaxLength(128)]
    public string? CommitIdempotencyKey { get; set; }

    [ConcurrencyCheck]
    public long ConcurrencyVersion { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
