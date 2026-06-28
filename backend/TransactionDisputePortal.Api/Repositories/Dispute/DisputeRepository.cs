using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DisputeRepository> _logger;

    public DisputeRepository(ApplicationDbContext context, ILogger<DisputeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<DisputeEntity>> GetByCustomerIdAsync(int customerId)
    {
        try
        {
            _logger.LogInformation("Fetching disputes for customer {CustomerId}", customerId);
            var disputes = await _context.Disputes
                .Where(d => d.CustomerId == customerId)
                .Include(d => d.Transaction)
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} disputes for customer {CustomerId}", disputes.Count, customerId);
            return disputes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching disputes for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<DisputeEntity>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all disputes");
            var disputes = await _context.Disputes
                .Include(d => d.Transaction)
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} disputes", disputes.Count);
            return disputes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all disputes");
            throw;
        }
    }

    public async Task<IEnumerable<DisputeEntity>> GetByTransactionIdAsync(int transactionId)
    {
        try
        {
            _logger.LogInformation("Fetching disputes for transaction {TransactionId}", transactionId);
            var disputes = await _context.Disputes
                .Where(d => d.TransactionIdFk == transactionId)
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} disputes for transaction {TransactionId}", disputes.Count, transactionId);
            return disputes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching disputes for transaction {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<DisputeEntity?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching dispute {DisputeId}", id);
            var dispute = await _context.Disputes
                .Include(d => d.Transaction)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (dispute == null)
                _logger.LogWarning("Dispute {DisputeId} not found", id);
            else
                _logger.LogInformation("Successfully retrieved dispute {DisputeId}", id);
            
            return dispute;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dispute {DisputeId}", id);
            throw;
        }
    }

    public async Task<DisputeEntity> AddAsync(DisputeEntity dispute)
    {
        try
        {
            _logger.LogInformation("Adding new dispute for transaction {TransactionId} with status {Status}", dispute.TransactionIdFk, dispute.Status);
            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added dispute {DisputeId} for transaction {TransactionId}", dispute.Id, dispute.TransactionIdFk);
            return dispute;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adding dispute for transaction {TransactionId}", dispute.TransactionIdFk);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding dispute for transaction {TransactionId}", dispute.TransactionIdFk);
            throw;
        }
    }

    public async Task UpdateAsync(DisputeEntity dispute)
    {
        try
        {
            _logger.LogInformation("Updating dispute {DisputeId} to status {Status}", dispute.Id, dispute.Status);
            _context.Disputes.Update(dispute);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated dispute {DisputeId}", dispute.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating dispute {DisputeId}", dispute.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating dispute {DisputeId}", dispute.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting dispute {DisputeId}", id);
            var dispute = await _context.Disputes.FindAsync(id);
            if (dispute != null)
            {
                _context.Disputes.Remove(dispute);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted dispute {DisputeId}", id);
            }
            else
            {
                _logger.LogWarning("Dispute {DisputeId} not found for deletion", id);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting dispute {DisputeId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting dispute {DisputeId}", id);
            throw;
        }
    }

    public async Task UpdateLockAsync(int id, int? userId, string? name, DateTime? lockedAt)
    {
        try
        {
            _logger.LogInformation("Updating lock on dispute {DisputeId}. UserId: {UserId}, Name: {Name}, LockedAt: {LockedAt}", id, userId, name, lockedAt);
            var dispute = await _context.Disputes.FindAsync(id);
            if (dispute != null)
            {
                dispute.LockedByUserId = userId;
                dispute.LockedByName = name;
                dispute.LockedAt = lockedAt;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated lock on dispute {DisputeId}", id);
            }
            else
            {
                _logger.LogWarning("Dispute {DisputeId} not found for lock update", id);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating lock on dispute {DisputeId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating lock on dispute {DisputeId}", id);
            throw;
        }
    }
}
