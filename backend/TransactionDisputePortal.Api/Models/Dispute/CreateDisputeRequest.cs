namespace TransactionDisputePortal.Api.Models.Dispute;

public class CreateDisputeRequest
{
    public int TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
