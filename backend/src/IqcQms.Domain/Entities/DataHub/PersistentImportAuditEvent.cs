using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportAuditEvents")]
public class PersistentImportAuditEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(64)]
    public string JobId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ActorUserId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? FromState { get; set; }

    [MaxLength(64)]
    public string? ToState { get; set; }

    [Required]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    public string? SanitizedMetadataJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
