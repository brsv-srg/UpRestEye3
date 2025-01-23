using System.ComponentModel.DataAnnotations;

namespace UpRestEye3.Models.Account
{
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Login { get; set; } = string.Empty;


        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDTO : AppUser
    {
        public string Token { get; set; } = string.Empty;
    }
}
