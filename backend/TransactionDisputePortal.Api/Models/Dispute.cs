using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionDisputePortal.Api.Models
{
    // Banking-focused dispute model
    public class Dispute
    {
        [Key]
        public int Id { get; set; }

        [Column("transaction_id")]
        public int TransactionId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        public string Reason { get; set; }

        public string Description { get; set; }

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

        [ForeignKey("TransactionIdFk")]
        public Transaction Transaction { get; set; }
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
