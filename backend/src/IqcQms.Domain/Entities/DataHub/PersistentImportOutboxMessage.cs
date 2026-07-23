using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportOutboxMessages")]
public class PersistentImportOutboxMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(64)]
    public string AggregateId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    public bool IsDispatched { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DispatchedAtUtc { get; set; }
}
