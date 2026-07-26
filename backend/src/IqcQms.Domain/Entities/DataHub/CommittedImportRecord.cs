using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub;

[Table("CommittedImportRecords")]
public class CommittedImportRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string ImportJobId { get; set; } = string.Empty;

    public int SourceRecordIndex { get; set; }

    [Required]
    [MaxLength(128)]
    public string ItemCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime InspectionDate { get; set; }

    [Required]
    [MaxLength(32)]
    public string Result { get; set; } = "PASS";

    [Required]
    [MaxLength(32)]
    public string MappingProfileVersion { get; set; } = "1.0";

    public DateTimeOffset CommittedAt { get; set; } = DateTimeOffset.UtcNow;
}
