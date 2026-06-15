using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionDisputePortal.Api.Models
{
    // Banking-focused transaction model
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("transaction_uid")]
        public string TransactionId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; }

        public string Merchant { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public TransactionStatus Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // navigation
        public List<Dispute> Disputes { get; set; } = new();
    }

    public enum TransactionStatus
    {
        Completed = 0,
        Pending = 1,
        Failed = 2,
        Reversed = 3
    }
}
