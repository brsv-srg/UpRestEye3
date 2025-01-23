using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.Account;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ConnectionParameterDTOController : ControllerBase
    {
        private readonly IConnectionParameterService _connectionParameterService;

        public ConnectionParameterDTOController(IConnectionParameterService connectionParameterService)
        {
            _connectionParameterService = connectionParameterService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConnectionParameterDTO>> GetConnectionParameter(int consumerId)
        {
            return await _connectionParameterService.GetConnectionParameterDTOByCustomerIdAsync(consumerId);
        }

        [HttpPost("save")]
        public async Task<ActionResult<AppUser>> SaveConnectionParameter(ConnectionParameterDTO connectionParameter)
        {
            await _connectionParameterService.SaveConnectionParameterAsync(connectionParameter);
            return CreatedAtAction(nameof(GetConnectionParameter), new { id = connectionParameter.ConsumerId }, connectionParameter);
        }
    }
}
