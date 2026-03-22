using Microsoft.AspNetCore.Identity;
namespace UserService.Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
