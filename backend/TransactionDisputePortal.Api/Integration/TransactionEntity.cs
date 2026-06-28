using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionDisputePortal.Api.Integration
{
    /// <summary>
    /// Transaction model - moved to Models/Transaction/Transaction.cs for organization
    /// </summary>
    public class TransactionEntity
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
        public List<DisputeEntity> Disputes { get; set; } = new();
    }

    public enum TransactionStatus
    {
        Completed = 0,
        Pending = 1,
        Failed = 2,
        Reversed = 3
    }
}
