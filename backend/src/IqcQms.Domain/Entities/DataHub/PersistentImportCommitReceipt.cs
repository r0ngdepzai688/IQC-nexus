using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportCommitReceipts")]
public class PersistentImportCommitReceipt
{
    [Key]
    [MaxLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string JobId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ContentFingerprint { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string CommittedBy { get; set; } = string.Empty;

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public DateTimeOffset CommittedAt { get; set; } = DateTimeOffset.UtcNow;
}
