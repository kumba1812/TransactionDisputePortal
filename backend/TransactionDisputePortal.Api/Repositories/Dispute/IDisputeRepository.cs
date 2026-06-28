using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public interface IDisputeRepository
{
    Task<IEnumerable<Dispute>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Dispute>> GetAllAsync();
    Task<IEnumerable<Dispute>> GetByTransactionIdAsync(int transactionId);
    Task<Dispute?> GetByIdAsync(int id);
    Task<Dispute> AddAsync(Dispute dispute);
    Task UpdateAsync(Dispute dispute);
    Task DeleteAsync(int id);
    Task UpdateLockAsync(int id, int? userId, string? name, DateTime? lockedAt);
}
