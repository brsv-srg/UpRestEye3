using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Integration;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMSProductsController : ControllerBase
    {
        private readonly IRMSProductService _productService;
        private readonly IRMSMeasureUnitService _unitService;
        private readonly IIntegrationRMSProductsService _loadProductsService;
        private readonly IIntegrationRMSEntitiesService _loadRMSMeasureUnits;

        public RMSProductsController(IRMSProductService productService, IRMSMeasureUnitService unitService, IIntegrationRMSProductsService loadProductsService, IIntegrationRMSEntitiesService loadRMSMeasureUnits)
        {
            _productService = productService;
            _loadProductsService = loadProductsService;
            _loadRMSMeasureUnits = loadRMSMeasureUnits;
            _unitService = unitService;
        }

        [HttpGet("{consumerId}")]
        public async Task<ActionResult<List<RMSProductDTO>>> GetProductsByConsumerId(int consumerId, [FromQuery] string? statusFilter = null)
        {
            var products = await _productService.GetProductsByConsumerIdAsync(consumerId);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "NotFromRMS")
                {
                    products = products.Where(p => p.Status != RMSProductStatusEnum.Synchronized).ToList();
                }
                else if (Enum.TryParse<RMSProductStatusEnum>(statusFilter, out var status))
                {
                    products = products.Where(p => p.Status == status).ToList();
                }
            }

            return Ok(products);
        }

        [HttpGet("services/{consumerId}")]
        public async Task<ActionResult<List<RMSProductDTO>>> GetServicesByConsumerId(int consumerId)
        {
            var products = await _productService.GetServicesByConsumerIdAsync(consumerId);
            return Ok(products);
        }

        [HttpGet("units/{consumerId}")]
        public async Task<ActionResult<List<RMSMeasureUnitDTO>>> GetUnitsByConsumerId(int consumerId)
        {
            var units = await _unitService.GetUnitsByConsumerIdAsync(consumerId);
            return Ok(units);
        }


        [HttpPost("download")]
        public async Task<ActionResult> DownloadRMSProducts([FromQuery] int consumerId)
        {
            try
            {
                var resGetProds = await _loadProductsService.GetProductsAsync(consumerId);
                var resGetUnits = await _loadRMSMeasureUnits.GetMeasureUnitsAsync(consumerId);
                if (resGetProds && resGetUnits)
                    return Ok();
                else
                    return BadRequest("An error occurred when downloading the products.");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost("upload")]
        public async Task<ActionResult> UploadRMSProducts([FromQuery] int consumerId, [FromBody] int? productId)
        {
            try
            {
                if (await _loadProductsService.PostProductsAsync(consumerId, productId))
                    return Ok();
                else
                    return BadRequest("An error occurred when uploading the products.");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost("save")]
        public async Task<ActionResult<RMSProductDTO>> SaveRMSProduct([FromBody] RMSProductDTO rmsProduct)
        {
            var updatedProduct = await _productService.SaveProductAsync(rmsProduct);
            return Ok(updatedProduct);
        }


        [HttpPost("delete")]
        public async Task<IActionResult> DeleteProducts([FromBody] List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return BadRequest(new { Message = "No product IDs provided." });
            }

            try
            {
                foreach (var productId in productIds)
                {
                    await _productService.DeleteProductAsync(productId);
                }

                return Ok(new { Message = "Products deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting products.", Details = ex.Message });
            }
        }
    }
}
