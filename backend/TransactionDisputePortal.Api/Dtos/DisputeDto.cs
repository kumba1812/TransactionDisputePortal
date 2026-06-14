namespace TransactionDisputePortal.Api.Dtos;

public class DisputeDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal RefundAmount { get; set; }
    public TransactionDto? Transaction { get; set; }

    public DisputeDto() { }

    public DisputeDto(Models.Dispute dispute)
    {
        Id = dispute.Id;
        TransactionId = dispute.TransactionIdFk;
        CustomerId = dispute.CustomerId;
        Reason = dispute.Reason;
        Description = dispute.Description;
        Status = (int)dispute.Status;
        CreatedAt = dispute.CreatedAt;
        ResolvedAt = dispute.ResolvedAt;
        ResolutionNotes = dispute.ResolutionNotes;
        RefundAmount = dispute.RefundAmount.HasValue ? dispute.RefundAmount.Value : 0m;

        // Include transaction without its disputes collection
        if (dispute.Transaction != null)
        {
            Transaction = new TransactionDto
            {
                Id = dispute.Transaction.Id,
                CustomerId = dispute.Transaction.CustomerId,
                TransactionId = dispute.Transaction.TransactionId,
                Amount = dispute.Transaction.Amount,
                Description = dispute.Transaction.Description,
                TransactionDate = dispute.Transaction.TransactionDate,
                Merchant = dispute.Transaction.Merchant,
                Category = dispute.Transaction.Category,
                Status = (int)dispute.Transaction.Status,
                CreatedAt = dispute.Transaction.CreatedAt
            };
        }
    }
}
