using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Integration;

namespace UpRestEye3.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoragesController : ControllerBase
    {
        private readonly IRMSAccountsService _accountsService;
        private readonly IIntegrationRMSEntitiesService _integrationService;

        public StoragesController(IRMSAccountsService accountsService, IIntegrationRMSEntitiesService integrationService)
        {
            _accountsService = accountsService;
            _integrationService = integrationService;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<List<RMSAccountDTO>>> GetStorages(int consumerId)
        {
            var suppliers = await _accountsService.GetAccountsByConsumerIdAsync(consumerId);
            return Ok(suppliers);
        }



        [HttpPost("save")]
        public async Task<ActionResult<int?>> SaveStorage([FromBody] RMSAccountDTO account)
        {
            var accountId = await _accountsService.SaveAccountAsync(account);
            return Ok(accountId);
        }

        [HttpPost("synchronize")]
        public async Task<ActionResult<bool>> SynchronizeStorages([FromQuery] int consumerId)
        {
            var res = await _integrationService.GetAccountsAsync(consumerId);
            return Ok(res);
        }
    }
}
