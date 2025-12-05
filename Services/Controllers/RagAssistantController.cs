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
        private readonly IRagAssistantDescriptor _ragAssistantDescriptor;
        private readonly IRagAssistantDataService _ragAssistantDataService;


        public RagAssistantController(IRAGFileService ragFileService, IRagAssistantDescriptor ragAssistantDescriptor, IRagAssistantDataService ragAssistantDataService)
        {
            _ragFileService = ragFileService;
            _ragAssistantDescriptor = ragAssistantDescriptor;
            _ragAssistantDataService = ragAssistantDataService;
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
            var ragAssistantDTO = await _ragAssistantDescriptor.CreateForConsumerAsync(ConsumerTaxId, ragJson);
            // 3. Сохранение информации об ассистенте в БД
            await _ragAssistantDataService.SaveRagAssistantAsync(ragAssistantDTO);

            return Ok(ragAssistantDTO.AssistantId);
        }

       
    }
}
