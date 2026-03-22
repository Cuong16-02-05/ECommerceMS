using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Identity;
using UserService.Infrastructure.Persistence;
namespace UserService.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserDbContext _dbContext;
        public UserRepository(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, UserDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }
        private User MapToDomain(ApplicationUser appUser)
        {
            if (appUser == null) return null!;
            return new User
            {
                Id = appUser.Id, UserName = appUser.UserName, Email = appUser.Email,
                FullName = appUser.FullName, PhoneNumber = appUser.PhoneNumber,
                ProfilePhotoUrl = appUser.ProfilePhotoUrl, IsActive = appUser.IsActive,
                CreatedAt = appUser.CreatedAt, LastLoginAt = appUser.LastLoginAt,
                IsEmailConfirmed = appUser.EmailConfirmed
            };
        }
        private ApplicationUser MapToApplicationUser(User user)
        {
            return new ApplicationUser
            {
                Id = user.Id, UserName = user.UserName, Email = user.Email,
                FullName = user.FullName, PhoneNumber = user.PhoneNumber,
                ProfilePhotoUrl = user.ProfilePhotoUrl, IsActive = user.IsActive,
                CreatedAt = user.CreatedAt, LastLoginAt = user.LastLoginAt,
                EmailConfirmed = user.IsEmailConfirmed
            };
        }
        public async Task<User?> FindByEmailAsync(string email) { var u = await _userManager.FindByEmailAsync(email); return u == null ? null : MapToDomain(u); }
        public async Task<User?> FindByUserNameAsync(string userName) { var u = await _userManager.FindByNameAsync(userName); return u == null ? null : MapToDomain(u); }
        public async Task<User?> FindByIdAsync(Guid id) { var u = await _userManager.FindByIdAsync(id.ToString()); return u == null ? null : MapToDomain(u); }
        public async Task<bool> CreateUserAsync(User user, string password) { var u = MapToApplicationUser(user); var r = await _userManager.CreateAsync(u, password); return r.Succeeded; }
        public async Task<bool> CheckPasswordAsync(User user, string password) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u != null && await _userManager.CheckPasswordAsync(u, password); }
        public async Task<bool> UpdateUserAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return false; u.UserName = user.UserName; u.Email = user.Email; u.FullName = user.FullName; u.PhoneNumber = user.PhoneNumber; u.ProfilePhotoUrl = user.ProfilePhotoUrl; var r = await _userManager.UpdateAsync(u); return r.Succeeded; }
        public async Task<IList<string>> GetUserRolesAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u == null ? new List<string>() : await _userManager.GetRolesAsync(u); }
        public async Task<bool> AddUserToRoleAsync(User user, string role) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return false; var r = await _userManager.AddToRoleAsync(u, role); return r.Succeeded; }
        public async Task<string?> GenerateEmailConfirmationTokenAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u == null ? null : await _userManager.GenerateEmailConfirmationTokenAsync(u); }
        public async Task<bool> VerifyConfirmaionEmailAsync(User user, string token) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return false; var r = await _userManager.ConfirmEmailAsync(u, token); return r.Succeeded; }
        public async Task<string?> GeneratePasswordResetTokenAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u == null ? null : await _userManager.GeneratePasswordResetTokenAsync(u); }
        public async Task<bool> ResetPasswordAsync(User user, string token, string newPassword) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return false; var r = await _userManager.ResetPasswordAsync(u, token, newPassword); return r.Succeeded; }
        public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return false; var r = await _userManager.ChangePasswordAsync(u, currentPassword, newPassword); return r.Succeeded; }
        public async Task UpdateLastLoginAsync(User user, DateTime loginTime) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u == null) return; u.LastLoginAt = loginTime; await _userManager.UpdateAsync(u); }
        public async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, string clientId, string userAgent, string ipAddress)
        {
            var tokens = await _dbContext.RefreshTokens.Where(t => t.UserId == userId && t.ClientId == clientId && t.UserAgent == userAgent && t.RevokedAt == null).ToListAsync();
            foreach (var t in tokens) { t.RevokedAt = DateTime.UtcNow; t.RevokedByIp = ipAddress; }
            var refreshToken = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, ClientId = clientId, UserAgent = userAgent, Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()), CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedByIp = ipAddress };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();
            return refreshToken.Token;
        }
        public async Task<RefreshToken?> GetRefreshTokenAsync(string token) => await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string ipAddress) { refreshToken.RevokedAt = DateTime.UtcNow; refreshToken.RevokedByIp = ipAddress; await _dbContext.SaveChangesAsync(); }
        public async Task<List<Address>> GetAddressesByUserIdAsync(Guid userId) => await _dbContext.Addresses.Where(a => a.UserId == userId).ToListAsync();
        public async Task<Guid> AddOrUpdateAddressAsync(Address address)
        {
            var existing = await _dbContext.Addresses.FindAsync(address.Id);
            if (existing == null) { await _dbContext.Addresses.AddAsync(address); await _dbContext.SaveChangesAsync(); return address.Id; }
            existing.AddressLine1 = address.AddressLine1; existing.AddressLine2 = address.AddressLine2; existing.City = address.City; existing.State = address.State; existing.PostalCode = address.PostalCode; existing.Country = address.Country; existing.IsDefaultBilling = address.IsDefaultBilling; existing.IsDefaultShipping = address.IsDefaultShipping;
            await _dbContext.SaveChangesAsync(); return existing.Id;
        }
        public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId) { var a = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId); if (a == null) return false; _dbContext.Addresses.Remove(a); await _dbContext.SaveChangesAsync(); return true; }
        public async Task<bool> IsLockedOutAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u != null && await _userManager.IsLockedOutAsync(u); }
        public async Task<bool> IsTwoFactorEnabledAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u != null && await _userManager.GetTwoFactorEnabledAsync(u); }
        public async Task IncrementAccessFailedCountAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u != null) await _userManager.AccessFailedAsync(u); }
        public async Task ResetAccessFailedCountAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); if (u != null) await _userManager.ResetAccessFailedCountAsync(u); }
        public async Task<DateTime?> GetLockoutEndDateAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u?.LockoutEnd?.UtcDateTime; }
        public Task<int> GetMaxFailedAccessAttemptsAsync() => Task.FromResult(_userManager.Options.Lockout.MaxFailedAccessAttempts);
        public async Task<int> GetAccessFailedCountAsync(User user) { var u = await _userManager.FindByIdAsync(user.Id.ToString()); return u?.AccessFailedCount ?? 0; }
        public async Task<bool> IsValidClientAsync(string clientId) => await _dbContext.Clients.AnyAsync(c => c.ClientId == clientId);
        public async Task<bool> IsUserExistsAsync(Guid userId) => await _dbContext.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
        public async Task<Address?> GetAddressByUserIdAndAddressIdAsync(Guid userId, Guid addressId) => await _dbContext.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId && a.Id == addressId);
    }
}
