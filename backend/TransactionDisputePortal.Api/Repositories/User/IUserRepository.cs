using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Repositories.User;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByUsernameAsync(string username);
    Task<ApplicationUser?> GetByIdAsync(int id);
    Task<ApplicationUser> AddAsync(ApplicationUser user);
    Task UpdateAsync(ApplicationUser user);
}
