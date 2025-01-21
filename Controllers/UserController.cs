using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<UserDTO> _userManager;
        private readonly IServerAuthService _serverAuthService;

        public UserController(IUserService userService, UserManager<UserDTO> userManager, IServerAuthService serverAuthService)
        {
            _userService = userService;
            _userManager = userManager;
            _serverAuthService = serverAuthService;

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

        // todo вынести в отдельный контроллер

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<ActionResult<LoginResponseDTO>> Authenticate([FromBody] LoginRequestDTO loginModel)
        {
            var user = await _userService.AuthenticateAsync(loginModel.Login, loginModel.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _serverAuthService.GenerateJwtToken(user);

            return Ok(new LoginResponseDTO
            {
                Token = token,
                Login = user.Login,
                ConsumerId = user.ConsumerId,
                ConsumerTaxNumber = user.ConsumerTaxNumber
            });
        }

    }
}
