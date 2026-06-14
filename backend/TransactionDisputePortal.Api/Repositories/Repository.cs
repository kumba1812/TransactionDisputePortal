using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Transactions
            .Where(t => t.CustomerId == customerId)
            .Include(t => t.Disputes)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Disputes)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
    }
}

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
            .AsNoTracking()
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
}
