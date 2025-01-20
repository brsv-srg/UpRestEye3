using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int userId)
        {
            return await _userService.GetUserDTOByIdAsync(userId);
        }

        [HttpPost("save")]
        public async Task<ActionResult<UserDTO>> SaveInvoice(UserDTO user)
        {
            var userId = await _userService.SaveUserAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = userId }, user);
        }


        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<ActionResult<UserDTO>> Authenticate([FromBody] LoginModel loginModel)
        {
            var user = await _userService.AuthenticateAsync(loginModel.Login, loginModel.Password);
            if (user == null)
            {
                return Unauthorized();
            }
            return Ok(user);
        }


        public class LoginModel
        {
            public string Login { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
