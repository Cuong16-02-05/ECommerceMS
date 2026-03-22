using System.ComponentModel.DataAnnotations;
namespace UserService.Application.DTOs
{
    public class RegisterDTO
    {
        [Required] public string UserName { get; set; } = null!;
        [Required][EmailAddress] public string Email { get; set; } = null!;
        [Required][StringLength(100, MinimumLength = 8)] public string Password { get; set; } = null!;
        [Phone] public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
    }
}
