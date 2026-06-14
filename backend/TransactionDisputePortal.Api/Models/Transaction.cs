namespace TransactionDisputePortal.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
}

public enum TransactionStatus
{
    Completed,
    Pending,
    Failed,
    Refunded
}
