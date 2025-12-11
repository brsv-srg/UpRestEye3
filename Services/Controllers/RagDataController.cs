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

            // 1. Генерация RAG файла
            var flatRecords = await _ragFileService.GenerateRAGFileAsync(ConsumerTaxId);
            var ragJson = JsonSerializer.Serialize(flatRecords);

            // Сохранение ragJson в файл для анализа
            var fileName = $"RAG_{ConsumerTaxId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var exeDirectory = AppContext.BaseDirectory;
            var filePath = Path.Combine(exeDirectory, fileName);
            await System.IO.File.WriteAllTextAsync(filePath, ragJson);


            // 2. Чтение идентификаторов векторого RAG хранилища 
            var savedRagData = await _ragDataService.GetRagDTOByCustomerIdAsync (ConsumerTaxId);

            // 3. Создание хранилища
            var ragDTO = await _ragManager.CreateForConsumerAsync(ConsumerTaxId, ragJson, savedRagData);

            // 4. Сохранение информации о векторном RAG хранилище в БД
            await _ragDataService.SaveRagAsync(ragDTO);

            return Ok(ragDTO.VectorStoreId);
        }

       
    }
}
