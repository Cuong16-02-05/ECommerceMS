using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using UAParser;
using UserService.API.DTOs;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) => _userService = userService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var result = await _userService.RegisterAsync(dto);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Registration failed."));
            return Ok(ApiResponse<string>.SuccessResponse("User registered successfully."));
        }

        [HttpPost("send-confirmation-email")]
        public async Task<IActionResult> SendConfirmationEmail([FromBody] EmailDTO dto)
        {
            var result = await _userService.SendConfirmationEmailAsync(dto.Email);
            if (result == null) return NotFound(ApiResponse<string>.FailResponse("User not found."));
            return Ok(ApiResponse<EmailConfirmationTokenResponseDTO>.SuccessResponse(result, "Token generated."));
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] ConfirmEmailDTO dto)
        {
            var result = await _userService.VerifyConfirmationEmailAsync(dto);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Invalid token."));
            return Ok(ApiResponse<string>.SuccessResponse("Email confirmed successfully."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var ua = GetNormalizedUserAgent();
            var result = await _userService.LoginAsync(dto, ip, ua);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
                return Unauthorized(ApiResponse<LoginResponseDTO>.FailResponse(result.ErrorMessage, data: result));
            result.Succeeded = true;
            return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(result, "Login successful."));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var ua = GetNormalizedUserAgent();
            var result = await _userService.RefreshTokenAsync(dto, ip, ua);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
                return Unauthorized(ApiResponse<string>.FailResponse(result.ErrorMessage));
            return Ok(ApiResponse<RefreshTokenResponseDTO>.SuccessResponse(result, "Token refreshed."));
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDTO dto)
        {
            var result = await _userService.RevokeRefreshTokenAsync(dto.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Invalid or already revoked token."));
            return Ok(ApiResponse<string>.SuccessResponse("Token revoked."));
        }

        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfile(Guid userId)
        {
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound(ApiResponse<string>.FailResponse("Profile not found."));
            return Ok(ApiResponse<ProfileDTO>.SuccessResponse(profile));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var result = await _userService.UpdateProfileAsync(dto);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Update failed."));
            return Ok(ApiResponse<string>.SuccessResponse("Profile updated."));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDTO dto)
        {
            var result = await _userService.ForgotPasswordAsync(dto.Email);
            if (result == null) return NotFound(ApiResponse<string>.FailResponse("Email not found."));
            return Ok(ApiResponse<ForgotPasswordResponseDTO>.SuccessResponse(result, "Reset token generated."));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var result = await _userService.ResetPasswordAsync(dto.UserId, dto.Token, dto.NewPassword);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Reset failed."));
            return Ok(ApiResponse<string>.SuccessResponse("Password reset successfully."));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<string>.FailResponse("Invalid token."));
            var result = await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Change failed."));
            return Ok(ApiResponse<string>.SuccessResponse("Password changed."));
        }

        [HttpPost("addresses")]
        public async Task<IActionResult> AddOrUpdateAddress([FromBody] AddressDTO dto)
        {
            var id = await _userService.AddOrUpdateAddressAsync(dto);
            if (id == Guid.Empty) return BadRequest(ApiResponse<string>.FailResponse("Failed."));
            return Ok(ApiResponse<Guid>.SuccessResponse(id, "Address saved."));
        }

        [HttpGet("{userId}/addresses")]
        public async Task<IActionResult> GetAddresses(Guid userId)
        {
            var result = await _userService.GetAddressesAsync(userId);
            return Ok(ApiResponse<IEnumerable<AddressDTO>>.SuccessResponse(result));
        }

        [HttpPost("delete-address")]
        public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressDTO dto)
        {
            var result = await _userService.DeleteAddressAsync(dto.UserId, dto.AddressId);
            if (!result) return BadRequest(ApiResponse<string>.FailResponse("Delete failed."));
            return Ok(ApiResponse<string>.SuccessResponse("Address deleted."));
        }

        [HttpGet("{userId}/exists")]
        public async Task<IActionResult> UserExists(Guid userId)
        {
            var exists = await _userService.IsUserExistsAsync(userId);
            return Ok(new ApiResponse<bool> { Success = true, Data = exists, Message = exists ? "User exists." : "User does not exist." });
        }

        [HttpGet("{userId}/address/{addressId}")]
        public async Task<IActionResult> GetUserAddress(Guid userId, Guid addressId)
        {
            var address = await _userService.GetAddressByUserIdAndAddressIdAsync(userId, addressId);
            if (address == null) return NotFound(ApiResponse<string>.FailResponse("Address not found."));
            return Ok(ApiResponse<AddressDTO>.SuccessResponse(address));
        }

        private string GetNormalizedUserAgent()
        {
            var raw = HttpContext.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return "Unknown";
            try { var p = Parser.GetDefault(); var c = p.Parse(raw); return $"{c.UA.Family}-{c.UA.Major}_{c.OS.Family}"; }
            catch { return "Unknown"; }
        }
    }
}
