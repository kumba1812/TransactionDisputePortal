using TransactionDisputePortal.Api.Integration;

namespace TransactionDisputePortal.Api.Models.Transaction;

public class UpdateTransactionRequest
{
    public string? Description { get; set; }
    public TransactionStatus Status { get; set; }
}
