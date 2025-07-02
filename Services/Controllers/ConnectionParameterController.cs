using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.Account;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "RequireAuthenticatedUser")]
    public class ConnectionParametersController : ControllerBase
    {
        private readonly IConnectionParameterService _connectionParameterService;

        public ConnectionParametersController(IConnectionParameterService connectionParameterService)
        {
            _connectionParameterService = connectionParameterService;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<ConnectionParameterDTO?>> GetConnectionParameter(int consumerId)
        {
            var result = await _connectionParameterService.GetConnectionParameterDTOByCustomerIdAsync(consumerId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<ActionResult<ConnectionParameterDTO?>> SaveConnectionParameter(ConnectionParameterDTO connectionParameter)
        {
            await _connectionParameterService.SaveConnectionParameterAsync(connectionParameter);
            return CreatedAtAction(nameof(GetConnectionParameter), new { id = connectionParameter.ConsumerId }, connectionParameter);
        }
    }
}

