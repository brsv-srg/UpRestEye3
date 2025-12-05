using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Recognition;



namespace UpRestEye3.Services.Controllers
{
    [ApiController]
    [Route("api/ragassistant")]
    public class RagAssistantController : ControllerBase
    {
        private readonly IRAGFileService _ragFileService;
        private readonly IRagAssistantService _ragAssistantService;

        public RagAssistantController(IRAGFileService ragFileService, IRagAssistantService ragAssistantService)
        {
            _ragFileService = ragFileService;
            _ragAssistantService = ragAssistantService;
        }


        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] string ConsumerTaxId)
        {
            if (string.IsNullOrEmpty(ConsumerTaxId))
                return BadRequest("Invalid request");

            // 1. Генерация RAG файла
            var flatRecords = await _ragFileService.GenerateRAGFileAsync(ConsumerTaxId);
            var ragJson = JsonSerializer.Serialize(flatRecords);

            // 2. Создание ассистента
            var result = await _ragAssistantService.CreateForConsumerAsync(
                ConsumerTaxId,
                ragJson);

            return Ok(result.AssistantId);
        }

       
    }
}
