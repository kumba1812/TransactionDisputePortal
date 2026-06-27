using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransactionDisputePortal.Api.Dtos;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IConfiguration _config;

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(idStr, out var id)) return id;
        return -1;
    }

    private string GetUserFullName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    private bool IsClient() => User.IsInRole("Client");

    private int LockExpiryMinutes =>
        int.TryParse(_config["Disputes:LockExpiryMinutes"], out var m) ? m : 10;

    public DisputesController(
        IDisputeRepository disputeRepository,
        ITransactionRepository transactionRepository,
        IConfiguration config)
    {
        _disputeRepository = disputeRepository;
        _transactionRepository = transactionRepository;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetDisputes()
    {
        IEnumerable<Dispute> disputes;

        if (IsClient())
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();
            disputes = await _disputeRepository.GetByCustomerIdAsync(userId);
        }
        else
        {
            disputes = await _disputeRepository.GetAllAsync();
        }

        return Ok(disputes.Select(d => new DisputeDto(d)).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDispute(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        if (IsClient() && dispute.CustomerId != GetUserId())
            return Forbid();

        return Ok(new DisputeDto(dispute));
    }

    [HttpGet("transaction/{transactionId}")]
    public async Task<IActionResult> GetDisputesByTransaction(int transactionId)
    {
        var disputes = await _disputeRepository.GetByTransactionIdAsync(transactionId);
        return Ok(disputes.Select(d => new DisputeDto(d)).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Client")]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        if (userId <= 0) return Unauthorized();

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
        if (transaction == null || transaction.CustomerId != userId)
            return BadRequest(new { message = "Invalid transaction" });

        var existingDisputes = await _disputeRepository.GetByTransactionIdAsync(request.TransactionId);
        if (existingDisputes.Any())
            return BadRequest(new { message = "A dispute already exists for this transaction" });

        var dispute = new Dispute
        {
            TransactionIdFk = request.TransactionId,
            CustomerId = userId,
            Reason = request.Reason,
            Description = request.Description,
            Status = DisputeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RefundAmount = transaction.Amount
        };

        var result = await _disputeRepository.AddAsync(dispute);
        return CreatedAtAction(nameof(GetDispute), new { id = result.Id }, new DisputeDto(result));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> UpdateDispute(int id, [FromBody] UpdateDisputeRequest request)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        // Verify the caller holds an active lock
        var userId = GetUserId();
        bool lockActive = dispute.LockedByUserId.HasValue &&
                          dispute.LockedAt.HasValue &&
                          dispute.LockedAt.Value.AddMinutes(LockExpiryMinutes) > DateTime.UtcNow;

        if (!lockActive || dispute.LockedByUserId != userId)
            return Conflict(new { message = "You do not hold the lock for this dispute. Please open it for editing first." });

        dispute.Status = request.Status;
        dispute.ResolutionNotes = request.ResolutionNotes ?? dispute.ResolutionNotes;

        if (request.Status == DisputeStatus.Resolved || request.Status == DisputeStatus.Refunded)
            dispute.ResolvedAt = DateTime.UtcNow;

        // Clear lock atomically with the update
        dispute.LockedByUserId = null;
        dispute.LockedByName = null;
        dispute.LockedAt = null;

        await _disputeRepository.UpdateAsync(dispute);
        return Ok(new DisputeDto(dispute));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDispute(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        var userId = GetUserId();

        // Admin can delete any; Client can only delete their own
        if (!User.IsInRole("Admin") && dispute.CustomerId != userId)
            return Forbid();

        // Bankers cannot delete disputes
        if (User.IsInRole("Banker") && !User.IsInRole("Admin"))
            return Forbid();

        await _disputeRepository.DeleteAsync(id);
        return NoContent();
    }

    // â”€â”€ Soft-Lock Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPost("{id}/lock")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> AcquireLock(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        var userId = GetUserId();
        var now = DateTime.UtcNow;

        bool lockHeldByAnother = dispute.LockedByUserId.HasValue &&
                                  dispute.LockedByUserId != userId &&
                                  dispute.LockedAt.HasValue &&
                                  dispute.LockedAt.Value.AddMinutes(LockExpiryMinutes) > now;

        if (lockHeldByAnother)
        {
            return Conflict(new
            {
                message = $"This dispute is currently being reviewed by {dispute.LockedByName}.",
                lockedByName = dispute.LockedByName,
                lockedAt = dispute.LockedAt
            });
        }

        await _disputeRepository.UpdateLockAsync(id, userId, GetUserFullName(), now);

        // Return updated dispute
        var updated = await _disputeRepository.GetByIdAsync(id);
        return Ok(new DisputeDto(updated!));
    }

    [HttpDelete("{id}/lock")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> ReleaseLock(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        if (dispute == null)
            return NotFound(new { message = "Dispute not found" });

        var userId = GetUserId();

        // Only the lock owner or Admin may release
        if (dispute.LockedByUserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        await _disputeRepository.UpdateLockAsync(id, null, null, null);
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
