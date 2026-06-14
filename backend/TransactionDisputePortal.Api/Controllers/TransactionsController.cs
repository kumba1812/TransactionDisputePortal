using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;
    private const int CustomerId = 1; // Hardcoded for demo, would come from auth in production

    public TransactionsController(ITransactionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        var transactions = await _repository.GetByCustomerIdAsync(CustomerId);
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var transaction = new Transaction
        {
            CustomerId = CustomerId,
            Amount = request.Amount,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            Merchant = request.Merchant,
            Category = request.Category,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.AddAsync(transaction);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionRequest request)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transaction not found" });

        transaction.Description = request.Description ?? transaction.Description;
        transaction.Status = request.Status;

        await _repository.UpdateAsync(transaction);
        return Ok(transaction);
    }

    [HttpDelete("{id}")]
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
