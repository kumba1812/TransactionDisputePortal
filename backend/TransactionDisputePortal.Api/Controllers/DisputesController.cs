using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransactionDisputePortal.Api.Dtos;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Models.Dispute;
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
    private readonly ILogger<DisputesController> _logger;

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
        IConfiguration config,
        ILogger<DisputesController> logger)
    {
        _disputeRepository = disputeRepository;
        _transactionRepository = transactionRepository;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDisputes()
    {
        try
        {
            _logger.LogInformation("GetDisputes called by user {UserId}, role: {Role}", GetUserId(), User.FindFirst(ClaimTypes.Role)?.Value);

            IEnumerable<DisputeEntity> disputes;

            if (IsClient())
            {
                var userId = GetUserId();
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid user ID for client role");
                    return Unauthorized();
                }
                disputes = await _disputeRepository.GetByCustomerIdAsync(userId);
                _logger.LogInformation("Retrieved {Count} disputes for customer {CustomerId}", disputes.Count(), userId);
            }
            else
            {
                disputes = await _disputeRepository.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} disputes for admin/banker", disputes.Count());
            }

            return Ok(disputes.Select(d => new DisputeDto(d)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving disputes" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDispute(int id)
    {
        try
        {
            _logger.LogInformation("GetDispute called for dispute {DisputeId} by user {UserId}", id, GetUserId());

            var dispute = await _disputeRepository.GetByIdAsync(id);
            if (dispute == null)
            {
                _logger.LogWarning("Dispute {DisputeId} not found", id);
                return NotFound(new { message = "Dispute not found" });
            }

            if (IsClient() && dispute.CustomerId != GetUserId())
            {
                _logger.LogWarning("Client user {UserId} attempted to access dispute {DisputeId} belonging to customer {CustomerId}", GetUserId(), id, dispute.CustomerId);
                return Forbid();
            }

            return Ok(new DisputeDto(dispute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute {DisputeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving dispute" });
        }
    }

    [HttpGet("transaction/{transactionId}")]
    public async Task<IActionResult> GetDisputesByTransaction(int transactionId)
    {
        try
        {
            _logger.LogInformation("GetDisputesByTransaction called for transaction {TransactionId}", transactionId);

            var disputes = await _disputeRepository.GetByTransactionIdAsync(transactionId);
            _logger.LogInformation("Retrieved {Count} disputes for transaction {TransactionId}", disputes.Count(), transactionId);
            return Ok(disputes.Select(d => new DisputeDto(d)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for transaction {TransactionId}", transactionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving disputes" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Client")]
    public async Task<IActionResult> CreateDispute([FromBody] Models.Dispute.CreateDisputeRequest request)
    {
        try
        {
            _logger.LogInformation("CreateDispute called by user {UserId} for transaction {TransactionId}", GetUserId(), request.TransactionId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateDispute");
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID for dispute creation");
                return Unauthorized();
            }

            var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
            if (transaction == null || transaction.CustomerId != userId)
            {
                _logger.LogWarning("Invalid transaction {TransactionId} for user {UserId}", request.TransactionId, userId);
                return BadRequest(new { message = "Invalid transaction" });
            }

            var existingDisputes = await _disputeRepository.GetByTransactionIdAsync(request.TransactionId);
            if (existingDisputes.Any())
            {
                _logger.LogWarning("Dispute already exists for transaction {TransactionId}", request.TransactionId);
                return BadRequest(new { message = "A dispute already exists for this transaction" });
            }

            var dispute = new DisputeEntity
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
            _logger.LogInformation("Dispute {DisputeId} created successfully by user {UserId}", result.Id, userId);
            return CreatedAtAction(nameof(GetDispute), new { id = result.Id }, new DisputeDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute for transaction {TransactionId}", request?.TransactionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating dispute" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> UpdateDispute(int id, [FromBody] UpdateDisputeRequest request)
    {
        try
        {
            _logger.LogInformation("UpdateDispute called for dispute {DisputeId} by user {UserId}", id, GetUserId());

            var dispute = await _disputeRepository.GetByIdAsync(id);
            if (dispute == null)
            {
                _logger.LogWarning("Dispute {DisputeId} not found for update", id);
                return NotFound(new { message = "Dispute not found" });
            }

            var userId = GetUserId();
            bool lockActive = dispute.LockedByUserId.HasValue &&
                              dispute.LockedAt.HasValue &&
                              dispute.LockedAt.Value.AddMinutes(LockExpiryMinutes) > DateTime.UtcNow;

            if (!lockActive || dispute.LockedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to update dispute {DisputeId} without lock", userId, id);
                return Conflict(new { message = "You do not hold the lock for this dispute. Please open it for editing first." });
            }

            dispute.Status = request.Status;
            dispute.ResolutionNotes = request.ResolutionNotes ?? dispute.ResolutionNotes;

            if (request.Status == DisputeStatus.Resolved || request.Status == DisputeStatus.Refunded)
                dispute.ResolvedAt = DateTime.UtcNow;

            dispute.LockedByUserId = null;
            dispute.LockedByName = null;
            dispute.LockedAt = null;

            await _disputeRepository.UpdateAsync(dispute);
            _logger.LogInformation("Dispute {DisputeId} updated successfully. New status: {Status}", id, request.Status);
            return Ok(new DisputeDto(dispute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dispute {DisputeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating dispute" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDispute(int id)
    {
        try
        {
            _logger.LogInformation("DeleteDispute called for dispute {DisputeId} by user {UserId}", id, GetUserId());

            var dispute = await _disputeRepository.GetByIdAsync(id);
            if (dispute == null)
            {
                _logger.LogWarning("Dispute {DisputeId} not found for deletion", id);
                return NotFound(new { message = "Dispute not found" });
            }

            var userId = GetUserId();

            if (!User.IsInRole("Admin") && dispute.CustomerId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete dispute {DisputeId} without permission", userId, id);
                return Forbid();
            }

            if (User.IsInRole("Banker") && !User.IsInRole("Admin"))
            {
                _logger.LogWarning("Banker user {UserId} attempted to delete dispute {DisputeId}", userId, id);
                return Forbid();
            }

            await _disputeRepository.DeleteAsync(id);
            _logger.LogInformation("Dispute {DisputeId} deleted successfully by user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dispute {DisputeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting dispute" });
        }
    }

    [HttpPost("{id}/lock")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> AcquireLock(int id)
    {
        try
        {
            _logger.LogInformation("AcquireLock called for dispute {DisputeId} by user {UserId}", id, GetUserId());

            var dispute = await _disputeRepository.GetByIdAsync(id);
            if (dispute == null)
            {
                _logger.LogWarning("Dispute {DisputeId} not found for lock acquisition", id);
                return NotFound(new { message = "Dispute not found" });
            }

            var userId = GetUserId();
            var now = DateTime.UtcNow;

            bool lockHeldByAnother = dispute.LockedByUserId.HasValue &&
                                      dispute.LockedByUserId != userId &&
                                      dispute.LockedAt.HasValue &&
                                      dispute.LockedAt.Value.AddMinutes(LockExpiryMinutes) > now;

            if (lockHeldByAnother)
            {
                _logger.LogWarning("User {UserId} cannot acquire lock on dispute {DisputeId} - already locked by {LockedByName}", userId, id, dispute.LockedByName);
                return Conflict(new
                {
                    message = $"This dispute is currently being reviewed by {dispute.LockedByName}.",
                    lockedByName = dispute.LockedByName,
                    lockedAt = dispute.LockedAt
                });
            }

            await _disputeRepository.UpdateLockAsync(id, userId, GetUserFullName(), now);
            _logger.LogInformation("Lock acquired on dispute {DisputeId} by user {UserId}", id, userId);

            var updated = await _disputeRepository.GetByIdAsync(id);
            return Ok(new DisputeDto(updated!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock on dispute {DisputeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error acquiring lock" });
        }
    }

    [HttpDelete("{id}/lock")]
    [Authorize(Roles = "Admin,Banker")]
    public async Task<IActionResult> ReleaseLock(int id)
    {
        try
        {
            _logger.LogInformation("ReleaseLock called for dispute {DisputeId} by user {UserId}", id, GetUserId());

            var dispute = await _disputeRepository.GetByIdAsync(id);
            if (dispute == null)
            {
                _logger.LogWarning("Dispute {DisputeId} not found for lock release", id);
                return NotFound(new { message = "Dispute not found" });
            }

            var userId = GetUserId();

            if (dispute.LockedByUserId != userId && !User.IsInRole("Admin"))
            {
                _logger.LogWarning("User {UserId} attempted to release lock on dispute {DisputeId} without permission", userId, id);
                return Forbid();
            }

            await _disputeRepository.UpdateLockAsync(id, null, null, null);
            _logger.LogInformation("Lock released on dispute {DisputeId} by user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock on dispute {DisputeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error releasing lock" });
        }
    }
}
