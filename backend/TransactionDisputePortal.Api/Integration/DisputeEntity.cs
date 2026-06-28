using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionDisputePortal.Api.Integration
{
    /// <summary>
    /// Dispute model - moved to Models/Dispute/Dispute.cs for organization
    /// </summary>
    public class DisputeEntity
    {
        [Key]
        public int Id { get; set; }

        [Column("transaction_id")]
        public int TransactionId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DisputeStatus Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("resolution_notes")]
        public string? ResolutionNotes { get; set; }

        [Column("refund_amount")]
        public decimal? RefundAmount { get; set; }

        [Column("transaction_id_fk")]
        public int TransactionIdFk { get; set; }

        // Soft-lock fields — prevent two bankers editing simultaneously
        [Column("locked_by_user_id")]
        public int? LockedByUserId { get; set; }

        [Column("locked_by_name")]
        [MaxLength(200)]
        public string? LockedByName { get; set; }

        [Column("locked_at")]
        public DateTime? LockedAt { get; set; }

        [ForeignKey("TransactionIdFk")]
        public TransactionEntity Transaction { get; set; } = null!;
    }

    public enum DisputeStatus
    {
        Pending = 0,
        UnderReview = 1,
        Resolved = 2,
        Refunded = 3,
        Rejected = 4
    }
}
