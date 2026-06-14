namespace TransactionDisputePortal.Api.Models;

public class Dispute
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? RefundAmount { get; set; }

    // Navigation
    public int TransactionIdFk { get; set; }
    public Transaction? Transaction { get; set; }
}

public enum DisputeStatus
{
    Pending,
    UnderReview,
    Resolved,
    Rejected,
    Refunded
}
