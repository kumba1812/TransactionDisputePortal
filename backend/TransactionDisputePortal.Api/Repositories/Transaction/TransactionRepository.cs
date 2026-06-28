using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(ApplicationDbContext context, ILogger<TransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<TransactionEntity>> GetByCustomerIdAsync(int customerId)
    {
        try
        {
            _logger.LogInformation("Fetching transactions for customer {CustomerId}", customerId);
            var transactions = await _context.Transactions
                .Where(t => t.CustomerId == customerId)
                .Include(t => t.Disputes)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} transactions for customer {CustomerId}", transactions.Count, customerId);
            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<TransactionEntity>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all transactions");
            var transactions = await _context.Transactions
                .Include(t => t.Disputes)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} transactions", transactions.Count);
            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all transactions");
            throw;
        }
    }

    public async Task<TransactionEntity?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching transaction {TransactionId}", id);
            var transaction = await _context.Transactions
                .Include(t => t.Disputes)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (transaction == null)
                _logger.LogWarning("Transaction {TransactionId} not found", id);
            else
                _logger.LogInformation("Successfully retrieved transaction {TransactionId}", id);
            
            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction {TransactionId}", id);
            throw;
        }
    }

    public async Task<TransactionEntity> AddAsync(TransactionEntity transaction)
    {
        try
        {
            _logger.LogInformation("Adding new transaction for customer {CustomerId} with amount {Amount}", transaction.CustomerId, transaction.Amount);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added transaction {TransactionId} for customer {CustomerId}", transaction.Id, transaction.CustomerId);
            return transaction;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adding transaction for customer {CustomerId}", transaction.CustomerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding transaction for customer {CustomerId}", transaction.CustomerId);
            throw;
        }
    }

    public async Task UpdateAsync(TransactionEntity transaction)
    {
        try
        {
            _logger.LogInformation("Updating transaction {TransactionId}", transaction.Id);
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated transaction {TransactionId}", transaction.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating transaction {TransactionId}", transaction.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting transaction {TransactionId}", id);
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted transaction {TransactionId}", id);
            }
            else
            {
                _logger.LogWarning("Transaction {TransactionId} not found for deletion", id);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting transaction {TransactionId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting transaction {TransactionId}", id);
            throw;
        }
    }
}
