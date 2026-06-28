using TransactionDisputePortal.Api.Integration;

namespace TransactionDisputePortal.Api.Repositories;

public interface IDisputeRepository
{
    Task<IEnumerable<DisputeEntity>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<DisputeEntity>> GetAllAsync();
    Task<IEnumerable<DisputeEntity>> GetByTransactionIdAsync(int transactionId);
    Task<DisputeEntity?> GetByIdAsync(int id);
    Task<DisputeEntity> AddAsync(DisputeEntity dispute);
    Task UpdateAsync(DisputeEntity dispute);
    Task DeleteAsync(int id);
    Task UpdateLockAsync(int id, int? userId, string? name, DateTime? lockedAt);
}
