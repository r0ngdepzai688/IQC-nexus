using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("PersistentImportMappedPayloads")]
public class PersistentImportMappedPayload
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string JobId { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string MappingProfileVersion { get; set; } = "1.0";

    [Required]
    public string CanonicalPayloadJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
