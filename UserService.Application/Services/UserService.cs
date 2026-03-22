using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        private string GenerateJwtToken(User user, IList<string> roles, string clientId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim("clientId", clientId)
            };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"] ?? "60")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> RegisterAsync(RegisterDTO dto)
        {
            if (await _userRepository.FindByEmailAsync(dto.Email) != null) return false;
            if (await _userRepository.FindByUserNameAsync(dto.UserName) != null) return false;
            var user = new User { Id = Guid.NewGuid(), UserName = dto.UserName, Email = dto.Email, PhoneNumber = dto.PhoneNumber, FullName = dto.FullName, CreatedAt = DateTime.UtcNow, IsActive = true, IsEmailConfirmed = false };
            var created = await _userRepository.CreateUserAsync(user, dto.Password);
            if (!created) return false;
            await _userRepository.AddUserToRoleAsync(user, "Customer");
            return true;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO dto, string ipAddress, string userAgent)
        {
            var response = new LoginResponseDTO();
            if (!await _userRepository.IsValidClientAsync(dto.ClientId)) { response.ErrorMessage = "Invalid client ID."; return response; }
            var user = dto.EmailOrUserName.Contains("@") ? await _userRepository.FindByEmailAsync(dto.EmailOrUserName) : await _userRepository.FindByUserNameAsync(dto.EmailOrUserName);
            if (user == null) { response.ErrorMessage = "Invalid username or password."; return response; }
            if (await _userRepository.IsLockedOutAsync(user)) { response.ErrorMessage = "Account is locked."; response.RemainingAttempts = 0; return response; }
            if (!user.IsEmailConfirmed) { response.ErrorMessage = "Email not confirmed."; return response; }
            var passwordValid = await _userRepository.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                await _userRepository.IncrementAccessFailedCountAsync(user);
                var maxAttempts = await _userRepository.GetMaxFailedAccessAttemptsAsync();
                var failedCount = await _userRepository.GetAccessFailedCountAsync(user);
                response.ErrorMessage = "Invalid username or password.";
                response.RemainingAttempts = Math.Max(0, maxAttempts - failedCount);
                return response;
            }
            await _userRepository.ResetAccessFailedCountAsync(user);
            if (await _userRepository.IsTwoFactorEnabledAsync(user)) { response.RequiresTwoFactor = true; return response; }
            await _userRepository.UpdateLastLoginAsync(user, DateTime.UtcNow);
            var roles = await _userRepository.GetUserRolesAsync(user);
            response.Succeeded = true;
            response.Token = GenerateJwtToken(user, roles, dto.ClientId);
            response.RefreshToken = await _userRepository.GenerateAndStoreRefreshTokenAsync(user.Id, dto.ClientId, userAgent, ipAddress);
            return response;
        }

        public async Task<EmailConfirmationTokenResponseDTO?> SendConfirmationEmailAsync(string email)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null) return null;
            var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
            return token == null ? null : new EmailConfirmationTokenResponseDTO { UserId = user.Id, Token = token };
        }

        public async Task<bool> VerifyConfirmationEmailAsync(ConfirmEmailDTO dto)
        {
            var user = await _userRepository.FindByIdAsync(dto.UserId);
            if (user == null) return false;
            var result = await _userRepository.VerifyConfirmaionEmailAsync(user, dto.Token);
            if (result) { user.IsActive = true; await _userRepository.UpdateUserAsync(user); }
            return result;
        }

        public async Task<RefreshTokenResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO dto, string ipAddress, string userAgent)
        {
            var response = new RefreshTokenResponseDTO();
            var refreshToken = await _userRepository.GetRefreshTokenAsync(dto.RefreshToken);
            if (refreshToken == null || !refreshToken.IsActive) { response.ErrorMessage = "Invalid or expired refresh token."; return response; }
            var user = await _userRepository.FindByIdAsync(refreshToken.UserId);
            if (user == null) { response.ErrorMessage = "User not found."; return response; }
            await _userRepository.RevokeRefreshTokenAsync(refreshToken, ipAddress);
            var roles = await _userRepository.GetUserRolesAsync(user);
            response.Token = GenerateJwtToken(user, roles, dto.ClientId);
            response.RefreshToken = await _userRepository.GenerateAndStoreRefreshTokenAsync(user.Id, dto.ClientId, userAgent, ipAddress);
            return response;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string token, string ipAddress)
        {
            var refreshToken = await _userRepository.GetRefreshTokenAsync(token);
            if (refreshToken == null || !refreshToken.IsActive) return false;
            await _userRepository.RevokeRefreshTokenAsync(refreshToken, ipAddress);
            return true;
        }

        public async Task<ForgotPasswordResponseDTO?> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null) return null;
            var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
            return token == null ? null : new ForgotPasswordResponseDTO { UserId = user.Id, Token = token };
        }

        public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            return user != null && await _userRepository.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            return user != null && await _userRepository.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public async Task<ProfileDTO?> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return null;
            return new ProfileDTO { UserId = user.Id, FullName = user.FullName, PhoneNumber = user.PhoneNumber, Email = user.Email, UserName = user.UserName, LastLoginAt = user.LastLoginAt, ProfilePhotoUrl = user.ProfilePhotoUrl };
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileDTO dto)
        {
            var user = await _userRepository.FindByIdAsync(dto.UserId);
            if (user == null) return false;
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.ProfilePhotoUrl = dto.ProfilePhotoUrl;
            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<Guid> AddOrUpdateAddressAsync(AddressDTO dto)
        {
            var address = new Address { Id = dto.Id ?? Guid.NewGuid(), UserId = dto.userId, AddressLine1 = dto.AddressLine1, AddressLine2 = dto.AddressLine2, City = dto.City, State = dto.State, PostalCode = dto.PostalCode, Country = dto.Country, IsDefaultShipping = dto.IsDefaultShipping, IsDefaultBilling = dto.IsDefaultBilling };
            return await _userRepository.AddOrUpdateAddressAsync(address);
        }

        public async Task<IEnumerable<AddressDTO>> GetAddressesAsync(Guid userId)
        {
            var addresses = await _userRepository.GetAddressesByUserIdAsync(userId);
            return addresses.Select(a => new AddressDTO { Id = a.Id, userId = a.UserId, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, City = a.City, State = a.State, PostalCode = a.PostalCode, Country = a.Country, IsDefaultShipping = a.IsDefaultShipping, IsDefaultBilling = a.IsDefaultBilling });
        }

        public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId) => await _userRepository.DeleteAddressAsync(userId, addressId);

        public async Task<bool> IsUserExistsAsync(Guid userId) => await _userRepository.IsUserExistsAsync(userId);

        public async Task<AddressDTO?> GetAddressByUserIdAndAddressIdAsync(Guid userId, Guid addressId)
        {
            var a = await _userRepository.GetAddressByUserIdAndAddressIdAsync(userId, addressId);
            if (a == null) return null;
            return new AddressDTO { Id = a.Id, userId = a.UserId, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, City = a.City, State = a.State, PostalCode = a.PostalCode, Country = a.Country, IsDefaultShipping = a.IsDefaultShipping, IsDefaultBilling = a.IsDefaultBilling };
        }
    }
}
