using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;


namespace UpRestEye3.Services.BusinessLogic
{
    public interface IServerAuthService
    {
        string GenerateJwtToken(UserDTO user);
    }
    

    public class ServerAuthService : IServerAuthService
    {
        private readonly IConfiguration _configuration;

        public ServerAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(UserDTO user)
        {
            try
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Login),
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim("ConsumerId", user.ConsumerId.ToString()),
                    new Claim("ConsumerTaxNumber", user.ConsumerTaxNumber)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddDays(1);

                var token = new JwtSecurityToken(
                    _configuration["Jwt:Issuer"],
                    _configuration["Jwt:Audience"],
                    claims,
                    expires: expires,
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);

            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }
    }
}
