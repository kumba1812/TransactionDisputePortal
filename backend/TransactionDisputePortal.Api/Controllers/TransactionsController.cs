using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransactionDisputePortal.Api.Dtos;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models.Transaction;
using TransactionDisputePortal.Api.Repositories;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransactionsController> _logger;

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(idStr, out var id)) return id;
        return -1;
    }

    private bool IsClient() => User.IsInRole("Client");

    public TransactionsController(ITransactionRepository repository, ILogger<TransactionsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        try
        {
            _logger.LogInformation("GetTransactions called by user {UserId}, role: {Role}", GetUserId(), User.FindFirst(ClaimTypes.Role)?.Value);

            IEnumerable<TransactionEntity> transactions;

            if (IsClient())
            {
                var userId = GetUserId();
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid user ID for client role");
                    return Unauthorized();
                }
                transactions = await _repository.GetByCustomerIdAsync(userId);
                _logger.LogInformation("Retrieved {Count} transactions for customer {CustomerId}", transactions.Count(), userId);
            }
            else
            {
                transactions = await _repository.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} transactions for admin/banker", transactions.Count());
            }

            var result = transactions.Select(t => new TransactionDto(t)).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving transactions" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        try
        {
            _logger.LogInformation("GetTransaction called for transaction {TransactionId} by user {UserId}", id, GetUserId());

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", id);
                return NotFound(new { message = "Transaction not found" });
            }

            if (IsClient() && transaction.CustomerId != GetUserId())
            {
                _logger.LogWarning("Client user {UserId} attempted to access transaction {TransactionId} belonging to customer {CustomerId}", GetUserId(), id, transaction.CustomerId);
                return Forbid();
            }

            return Ok(new TransactionDto(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving transaction" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            _logger.LogInformation("CreateTransaction called by user {UserId} with amount {Amount}", GetUserId(), request.Amount);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateTransaction");
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID for transaction creation");
                return Unauthorized();
            }

            var transaction = new TransactionEntity
            {
                CustomerId = userId,
                Amount = request.Amount,
                Description = request.Description,
                TransactionDate = request.TransactionDate,
                Merchant = request.Merchant,
                Category = request.Category,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _repository.AddAsync(transaction);
            _logger.LogInformation("Transaction {TransactionId} created successfully by user {UserId} with amount {Amount}", result.Id, userId, request.Amount);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, new TransactionDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating transaction" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionRequest request)
    {
        try
        {
            _logger.LogInformation("UpdateTransaction called for transaction {TransactionId} by user {UserId}", id, GetUserId());

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found for update", id);
                return NotFound(new { message = "Transaction not found" });
            }

            transaction.Description = request.Description ?? transaction.Description;
            transaction.Status = request.Status;

            await _repository.UpdateAsync(transaction);
            _logger.LogInformation("Transaction {TransactionId} updated successfully. New status: {Status}", id, request.Status);
            return Ok(new TransactionDto(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {TransactionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating transaction" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        try
        {
            _logger.LogInformation("DeleteTransaction called for transaction {TransactionId} by user {UserId}", id, GetUserId());

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found for deletion", id);
                return NotFound(new { message = "Transaction not found" });
            }

            await _repository.DeleteAsync(id);
            _logger.LogInformation("Transaction {TransactionId} deleted successfully by user {UserId}", id, GetUserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting transaction" });
        }
    }
}
