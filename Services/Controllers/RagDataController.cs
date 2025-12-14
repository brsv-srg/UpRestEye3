using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Recognition;



namespace UpRestEye3.Services.Controllers
{
    [ApiController]
    [Route("api/ragdata")]
    public class RagDataController : ControllerBase
    {
        private readonly IRAGFileService _ragFileService;
        private readonly IRagManager _ragManager;
        private readonly IRagDataService _ragDataService;


        public RagDataController(IRAGFileService ragFileService, IRagManager ragManager, IRagDataService ragDataService)
        {
            _ragFileService = ragFileService;
            _ragManager = ragManager;
            _ragDataService = ragDataService;
        }


        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] string ConsumerTaxId)
        {
            if (string.IsNullOrEmpty(ConsumerTaxId))
                return BadRequest("Invalid request");

            // 1. Генерация RAG файла для мапинга
            var mappingRecords = await _ragFileService.GenerateMappingRAGFileAsync(ConsumerTaxId);
            var mappingRagJson = JsonSerializer.Serialize(mappingRecords);

            // Сохранение ragJson в файл для анализа
            var mappingFileName = $"RAG_Mapping_{ConsumerTaxId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var exeDirectory = AppContext.BaseDirectory;
            var mappingFilePath = Path.Combine(exeDirectory, mappingFileName);
            await System.IO.File.WriteAllTextAsync(mappingFilePath, mappingRagJson);


            // 2. Генерация RAG файла с продуктами 
            var productsRecords = await _ragFileService.GenerateProductsRAGFileAsync(ConsumerTaxId);
            var productsRagJson = JsonSerializer.Serialize(productsRecords);

            // Сохранение ragJson в файл для анализа
            var productFileName = $"RAG_Products_{ConsumerTaxId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var productsFilePath = Path.Combine(exeDirectory, productFileName);
            await System.IO.File.WriteAllTextAsync(productsFilePath, productsRagJson);


            // 2. Чтение идентификаторов векторого RAG хранилища 
            var savedRagData = await _ragDataService.GetRagDTOByCustomerIdAsync (ConsumerTaxId);

            // 3. Создание хранилища мапинга
            var mappingRagDTO = await _ragManager.CreateMappingVectorAsync(ConsumerTaxId, mappingRagJson, savedRagData);
            if (mappingRagDTO == null)
            {
                return StatusCode(500, "Failed to create mapping RAG vector store.");
            }
            else
            {
                if(savedRagData == null)
                {
                    savedRagData = new Models.DTO.RagManagementDTO
                    {
                        ConsumerTaxNumber = ConsumerTaxId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                // Используем mappingRagDTO для дальнейших действий, если необходимо
                savedRagData.MappingVectorStoreId = mappingRagDTO.MappingVectorStoreId;
                savedRagData.MappingFileId = mappingRagDTO.MappingFileId;

            }

            // 4. Создание хранилища продуктов
            var productsRagDTO = await _ragManager.CreateProductsVectorAsync(ConsumerTaxId, productsRagJson, savedRagData);
            if (productsRagDTO == null)
            {
                return StatusCode(500, "Failed to create product RAG vector store.");
            }
            else
            {
                if (savedRagData == null)
                {
                    savedRagData = new Models.DTO.RagManagementDTO
                    {
                        ConsumerTaxNumber = ConsumerTaxId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                // Используем mappingRagDTO для дальнейших действий, если необходимо
                savedRagData.ProductsVectorStoreId = mappingRagDTO.ProductsVectorStoreId;
                savedRagData.ProductsFileId = mappingRagDTO.ProductsFileId;

            }



            // 5. Сохранение информации о векторном RAG хранилище в БД
            await _ragDataService.SaveRagAsync(savedRagData);

            return Ok(savedRagData);
        }

       
    }
}
