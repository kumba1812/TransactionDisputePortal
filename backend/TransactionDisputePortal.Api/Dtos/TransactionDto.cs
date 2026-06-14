namespace TransactionDisputePortal.Api.Dtos;

public class TransactionDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DisputeDto>? Disputes { get; set; }

    public TransactionDto() { }

    public TransactionDto(Models.Transaction transaction)
    {
        Id = transaction.Id;
        CustomerId = transaction.CustomerId;
        TransactionId = transaction.TransactionId;
        Amount = transaction.Amount;
        Description = transaction.Description;
        TransactionDate = transaction.TransactionDate;
        Merchant = transaction.Merchant;
        Category = transaction.Category;
        Status = (int)transaction.Status;
        CreatedAt = transaction.CreatedAt;

        // Map disputes without circular references
        if (transaction.Disputes != null && transaction.Disputes.Any())
        {
            Disputes = transaction.Disputes.Select(d => new DisputeDto
            {
                Id = d.Id,
                TransactionId = d.TransactionIdFk,
                CustomerId = d.CustomerId,
                Reason = d.Reason,
                Description = d.Description,
                Status = (int)d.Status,
                CreatedAt = d.CreatedAt,
                ResolvedAt = d.ResolvedAt,
                ResolutionNotes = d.ResolutionNotes,
                RefundAmount = d.RefundAmount.HasValue ? d.RefundAmount.Value : 0m,
                Transaction = null // Prevent circular reference
            }).ToList();
        }
    }
}
