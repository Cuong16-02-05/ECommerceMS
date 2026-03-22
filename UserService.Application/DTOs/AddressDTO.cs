using System.ComponentModel.DataAnnotations;
namespace UserService.Application.DTOs
{
    public class AddressDTO
    {
        public Guid? Id { get; set; }
        [Required] public Guid userId { get; set; }
        [Required] public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        [Required] public string City { get; set; } = null!;
        [Required] public string State { get; set; } = null!;
        [Required] public string PostalCode { get; set; } = null!;
        [Required] public string Country { get; set; } = null!;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }
    public class DeleteAddressDTO
    {
        [Required] public Guid UserId { get; set; }
        [Required] public Guid AddressId { get; set; }
    }
}
