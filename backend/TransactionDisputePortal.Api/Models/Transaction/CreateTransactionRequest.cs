namespace TransactionDisputePortal.Api.Models.Transaction;

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
