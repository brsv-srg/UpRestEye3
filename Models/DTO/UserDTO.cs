using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UpRestEye3.Models.DTO
{
    public class UserDTO : IdentityUser
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConsumerId { get; set; } = -1;
        public string ConsumerTaxNumber { get; set; } = string.Empty;
    }


    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Login { get; set; } = string.Empty;


        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDTO : UserDTO
    {
        public string Token { get; set; } = string.Empty;
    }
}
