using System.ComponentModel.DataAnnotations;
namespace UserService.Application.DTOs
{
    public class EmailDTO { [Required][EmailAddress] public string Email { get; set; } = null!; }
    public class EmailConfirmationTokenResponseDTO { public Guid UserId { get; set; } public string Token { get; set; } = null!; }
    public class ConfirmEmailDTO { [Required] public Guid UserId { get; set; } [Required] public string Token { get; set; } = null!; }
    public class ForgotPasswordResponseDTO { public Guid UserId { get; set; } public string Token { get; set; } = null!; }
    public class RefreshTokenResponseDTO { public string? Token { get; set; } public string? RefreshToken { get; set; } public string? ErrorMessage { get; set; } }
}
