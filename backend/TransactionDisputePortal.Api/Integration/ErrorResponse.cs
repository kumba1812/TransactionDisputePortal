namespace TransactionDisputePortal.Api.Integration;

/// <summary>
/// Standard error response returned to clients
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}
