using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly ApplicationDbContext _context;

    public DisputeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Dispute>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Disputes
            .Where(d => d.CustomerId == customerId)
            .Include(d => d.Transaction)
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dispute>> GetAllAsync()
    {
        return await _context.Disputes
            .Include(d => d.Transaction)
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dispute>> GetByTransactionIdAsync(int transactionId)
    {
        return await _context.Disputes
            .Where(d => d.TransactionIdFk == transactionId)
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dispute?> GetByIdAsync(int id)
    {
        return await _context.Disputes
            .Include(d => d.Transaction)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Dispute> AddAsync(Dispute dispute)
    {
        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync();
        return dispute;
    }

    public async Task UpdateAsync(Dispute dispute)
    {
        _context.Disputes.Update(dispute);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var dispute = await _context.Disputes.FindAsync(id);
        if (dispute != null)
        {
            _context.Disputes.Remove(dispute);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLockAsync(int id, int? userId, string? name, DateTime? lockedAt)
    {
        var dispute = await _context.Disputes.FindAsync(id);
        if (dispute != null)
        {
            dispute.LockedByUserId = userId;
            dispute.LockedByName = name;
            dispute.LockedAt = lockedAt;
            await _context.SaveChangesAsync();
        }
    }
}
