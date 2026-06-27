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
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(idStr, out var id)) return id;
        return -1;
    }

    private bool IsClient() => User.IsInRole("Client");

    public TransactionsController(ITransactionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        IEnumerable<Transaction> transactions;

        if (IsClient())
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();
            transactions = await _repository.GetByCustomerIdAsync(userId);
        }
        else
        {
            transactions = await _repository.GetAllAsync();
        }

        var result = transactions.Select(t => new TransactionDto(t)).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });

        if (IsClient() && transaction.CustomerId != GetUserId())
            return Forbid();

        return Ok(new TransactionDto(transaction));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        if (userId <= 0) return Unauthorized();

        var transaction = new Transaction
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
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, new TransactionDto(result));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionRequest request)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });

        transaction.Description = request.Description ?? transaction.Description;
        transaction.Status = request.Status;

        await _repository.UpdateAsync(transaction);
        return Ok(new TransactionDto(transaction));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class UpdateTransactionRequest
{
    public string? Description { get; set; }
    public TransactionStatus Status { get; set; }
}
