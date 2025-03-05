using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        private readonly IIntegrationSupplierService _integrationService;

        public SuppliersController(ISupplierService supplierService, IIntegrationSupplierService integrationService)
        {
            _supplierService = supplierService;
            _integrationService = integrationService;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<List<SupplierDTO>>> GetSuppliers(int consumerId)
        {
            var suppliers = await _supplierService.GetSuppliersDTOAsync(consumerId);
            return Ok(suppliers);
        }

        [HttpPost("save")]
        public async Task<ActionResult<RMSProductDTO>> SaveSupplier([FromBody] SupplierDTO supplier)
        {
            supplier.Status = SupplierStatus.Changed;
            var supplierId = await _supplierService.SaveSupplierAsync(supplier);
            var updatedSupplier = await _supplierService.GetSupplierDTOByIdAsync((int)supplierId);
            return Ok(updatedSupplier);
        }

        [HttpPost("synchronize")]
        public async Task<ActionResult<bool>> SynchronizeSupplier([FromQuery] int consumerId)
        {
            var res = await _integrationService.SynchronizeSuppliersAsync(consumerId);
            return Ok(res);
        }
    }
}
