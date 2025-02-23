using Microsoft.AspNetCore.Mvc;
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

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<List<SupplierDTO>>> GetSuppliers(int consumerId)
        {
            var suppliers = await _supplierService.GetSuppliersDTOAsync(consumerId);
            return Ok(suppliers);
        }
       
    }
}
