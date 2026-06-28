using TransactionDisputePortal.Api.Integration;

namespace TransactionDisputePortal.Api.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<TransactionEntity>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<TransactionEntity>> GetAllAsync();
    Task<TransactionEntity?> GetByIdAsync(int id);
    Task<TransactionEntity> AddAsync(TransactionEntity transaction);
    Task UpdateAsync(TransactionEntity transaction);
    Task DeleteAsync(int id);
}
