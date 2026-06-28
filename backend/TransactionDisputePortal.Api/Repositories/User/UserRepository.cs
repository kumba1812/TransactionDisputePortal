using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Integration;

namespace TransactionDisputePortal.Api.Repositories.User;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApplicationUser?> GetByUsernameAsync(string username)
    {
        try
        {
            _logger.LogInformation("Fetching user by username: {Username}", username);
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            
            if (user == null)
                _logger.LogWarning("User with username {Username} not found or inactive", username);
            else
                _logger.LogInformation("Successfully retrieved user {UserId} with username {Username}", user.Id, username);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user by username: {Username}", username);
            throw;
        }
    }

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching user {UserId}", id);
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                _logger.LogWarning("User {UserId} not found", id);
            else
                _logger.LogInformation("Successfully retrieved user {UserId}", id);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", id);
            throw;
        }
    }

    public async Task<ApplicationUser> AddAsync(ApplicationUser user)
    {
        try
        {
            _logger.LogInformation("Adding new user with username {Username} and role {Role}", user.Username, user.Role);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added user {UserId} with username {Username}", user.Id, user.Username);
            return user;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adding user with username {Username}", user.Username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding user with username {Username}", user.Username);
            throw;
        }
    }

    public async Task UpdateAsync(ApplicationUser user)
    {
        try
        {
            _logger.LogInformation("Updating user {UserId}", user.Id);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated user {UserId}", user.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating user {UserId}", user.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user {UserId}", user.Id);
            throw;
        }
    }
}
