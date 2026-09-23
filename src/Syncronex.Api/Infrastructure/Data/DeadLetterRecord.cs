using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Syncronex.Api.Infrastructure.Data;

[Table("dead_letters")]
public class DeadLetterRecord
{
  [Key]
  [Column("id")]
  public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [Column("source_event_id")]
  [MaxLength(100)]
  public string SourceEventId { get; set; } = string.Empty;

  [Required]
  [Column("payload")]
  public string Payload { get; set; } = string.Empty; // Stored as a raw JSON string for auditing

  [Required]
  [Column("failure_reason")]
  public string FailureReason { get; set; } = string.Empty;

  [Column("created_at")]
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  [Required]
  [Column("status")]
  [MaxLength(50)]
  public string Status { get; set; } = "Pending_Review"; // Valid states: Pending_Review, Resolved, Ignored
}
