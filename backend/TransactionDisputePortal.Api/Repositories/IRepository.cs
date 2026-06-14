using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByCustomerIdAsync(int customerId);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
}

public interface IDisputeRepository
{
    Task<IEnumerable<Dispute>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Dispute>> GetByTransactionIdAsync(int transactionId);
    Task<Dispute?> GetByIdAsync(int id);
    Task<Dispute> AddAsync(Dispute dispute);
    Task UpdateAsync(Dispute dispute);
    Task DeleteAsync(int id);
}
