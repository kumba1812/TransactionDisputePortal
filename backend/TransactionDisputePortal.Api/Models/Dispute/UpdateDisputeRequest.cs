using TransactionDisputePortal.Api.Integration;

namespace TransactionDisputePortal.Api.Models.Dispute;

public class UpdateDisputeRequest
{
    public DisputeStatus Status { get; set; }
    public string? ResolutionNotes { get; set; }
}
