
using Microsoft.AspNetCore.Identity;

namespace UpRestEye3.Models.Account
{
    public class AppUser : IdentityUser
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConsumerId { get; set; }
        public string ConsumerTaxId { get; set; } = string.Empty;
    }

}
