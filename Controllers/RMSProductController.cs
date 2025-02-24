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

        public RMSProductsController(IRMSProductService productService, ILoadRMSProductsService loadProductsService)
        {
            _productService = productService;
            _loadProductsService = loadProductsService;
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
            await _loadProductsService.LoadProductsAsync(consumerId);
            return Ok();
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
