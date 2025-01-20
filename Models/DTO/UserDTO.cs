using Microsoft.AspNetCore.Identity;

namespace UpRestEye3.Models.DTO
{
    public class UserDTO : IdentityUser
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConsumerId { get; set; } = -1;
        public string ConsumerTaxNumber { get; set; } = string.Empty;
    }

}
