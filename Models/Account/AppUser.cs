
using Microsoft.AspNetCore.Identity;

namespace UpRestEye3.Models.Account
{
    public class AppUser : IdentityUser
    {
        public int ConsumerId { get; set; }
        public string ConsumerTaxNumber { get; set; } = string.Empty;
    }

}
