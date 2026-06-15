using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Dtos
{
    public class ChangeStatusDto
    {
        public DisputeStatus Status { get; set; }
        public string Notes { get; set; }
        public decimal? RefundAmount { get; set; }
    }
}
