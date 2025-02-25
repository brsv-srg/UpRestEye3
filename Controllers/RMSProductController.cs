using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMSProductsController : ControllerBase
    {
        private readonly IRMSProductService _productService;
        private readonly ILoadRMSProductsService _loadProductsService;
        private readonly ILoadRMSMeasureUnitsService _loadRMSMeasureUnits;

        public RMSProductsController(IRMSProductService productService, ILoadRMSProductsService loadProductsService, ILoadRMSMeasureUnitsService loadRMSMeasureUnits)
        {
            _productService = productService;
            _loadProductsService = loadProductsService;
            _loadRMSMeasureUnits = loadRMSMeasureUnits;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<List<RMSProductDTO>>> GetProductsByConsumerId(int consumerId)
        {
            var products = await _productService.GetProductsByConsumerIdAsync(consumerId);
            return Ok(products);
        }

        [HttpPost("load")]
        public async Task<ActionResult> LoadRMSProducts([FromQuery] int consumerId)
        {
            try
            {
                await _loadProductsService.LoadProductsAsync(consumerId);
                await _loadRMSMeasureUnits.LoadMeasureUnitsAsync(consumerId);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost("save")]
        public async Task<ActionResult<RMSProductDTO>> SaveRMSProduct([FromBody] RMSProductDTO rmsProduct)
        {
            var invoiceId = await _productService.SaveProductAsync(rmsProduct);
            var updatedInvoice = await _productService.GetProductByIdAsync((int)invoiceId);
            return Ok(updatedInvoice);
        }

    }
}
