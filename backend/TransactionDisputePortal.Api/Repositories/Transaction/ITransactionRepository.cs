using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
}
