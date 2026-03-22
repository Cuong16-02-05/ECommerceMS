using System.ComponentModel.DataAnnotations;
namespace UserService.Application.DTOs
{
    public class LoginDTO
    {
        [Required] public string EmailOrUserName { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
        [Required] public string ClientId { get; set; } = null!;
    }
    public class LoginResponseDTO
    {
        public bool Succeeded { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? ErrorMessage { get; set; }
        public int? RemainingAttempts { get; set; }
    }
    public class RefreshTokenRequestDTO
    {
        [Required] public string RefreshToken { get; set; } = null!;
        [Required] public string ClientId { get; set; } = null!;
    }
}
