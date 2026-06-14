using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisputesController : ControllerBase
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly ITransactionRepository _transactionRepository;
    private const int CustomerId = 1; // Hardcoded for demo

    public DisputesController(IDisputeRepository disputeRepository, ITransactionRepository transactionRepository)
    {
        _disputeRepository = disputeRepository;
        _transactionRepository = transactionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetDisputes()
    {
        var disputes = await _disputeRepository.GetByCustomerIdAsync(CustomerId);
        return Ok(disputes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDispute(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        return Ok(dispute);
    }

    [HttpGet("transaction/{transactionId}")]
    public async Task<IActionResult> GetDisputesByTransaction(int transactionId)
    {
        var disputes = await _disputeRepository.GetByTransactionIdAsync(transactionId);
        return Ok(disputes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verify transaction exists and belongs to customer
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
        if (transaction == null || transaction.CustomerId != CustomerId)
            return BadRequest(new { message = "Invalid transaction" });

        var dispute = new Dispute
        {
            TransactionIdFk = request.TransactionId,
            CustomerId = CustomerId,
            Reason = request.Reason,
            Description = request.Description,
            Status = DisputeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RefundAmount = transaction.Amount
        };

        var result = await _disputeRepository.AddAsync(dispute);
        return CreatedAtAction(nameof(GetDispute), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDispute(int id, [FromBody] UpdateDisputeRequest request)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        if (dispute.CustomerId != CustomerId && !User.IsInRole("Admin"))
            return Forbid();

        dispute.Status = request.Status;
        dispute.ResolutionNotes = request.ResolutionNotes ?? dispute.ResolutionNotes;

        if (request.Status == DisputeStatus.Resolved || request.Status == DisputeStatus.Refunded)
        {
            dispute.ResolvedAt = DateTime.UtcNow;
        }

        await _disputeRepository.UpdateAsync(dispute);
        return Ok(dispute);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDispute(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        if (dispute.CustomerId != CustomerId)
            return Forbid();

        await _disputeRepository.DeleteAsync(id);
        return NoContent();
    }
}

public class CreateDisputeRequest
{
    public int TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateDisputeRequest
{
    public DisputeStatus Status { get; set; }
    public string? ResolutionNotes { get; set; }
}
