using System.ComponentModel.DataAnnotations;
namespace UserService.Application.DTOs
{
    public class ProfileDTO
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
    public class UpdateProfileDTO
    {
        [Required] public Guid UserId { get; set; }
        [Required] public string FullName { get; set; } = null!;
        [Required][Phone] public string PhoneNumber { get; set; } = null!;
        [Url] public string? ProfilePhotoUrl { get; set; }
    }
    public class ResetPasswordDTO
    {
        [Required] public Guid UserId { get; set; }
        [Required] public string Token { get; set; } = null!;
        [Required][StringLength(100, MinimumLength = 6)] public string NewPassword { get; set; } = null!;
    }
    public class ChangePasswordDTO
    {
        [Required] public string CurrentPassword { get; set; } = null!;
        [Required][StringLength(100, MinimumLength = 6)] public string NewPassword { get; set; } = null!;
    }
}
