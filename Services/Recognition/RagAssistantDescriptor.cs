using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.Recognition
{
    public interface IRagAssistantDescriptor
    {
        Task<RagAssistantDTO> CreateForConsumerAsync(string consumerKey, string ragJson, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Сервис для:
    /// 1) загрузки RAG-файла (JSON) в Files API,
    /// 2) создания Vector Store на его основе,
    /// 3) создания/обновления ассистента, использующего этот Vector Store через file_search.
    /// </summary>
    public class RagAssistantDescriptor : IRagAssistantDescriptor
    {
        private readonly HttpClient _http;
        private readonly string _model;
        private readonly string _vectorStoreNamePrefix = "consumer-vs";
        private readonly string _assistantNamePrefix = "consumer-assistant";

        /// <param name="httpClient">HttpClient с нормальной жизнью (DI).</param>
        /// <param name="apiKey">API key OpenAI.</param>
        /// <param name="model">
        /// Модель для ассистента. 
        /// Рекомендуется начинать с "gpt-4o" и перейти на "gpt-5.1", когда проект гарантированно имеет к ней доступ.
        /// </param>
        /// <param name="organizationId">Необязательный OpenAI-Organization.</param>
        /// <param name="projectId">Необязательный OpenAI-Project. Полезно, если ключ не проектный.</param>
        public RagAssistantDescriptor(
            HttpClient httpClient,
            string apiKey)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Точечное уточнение: гарантируем корректный BaseAddress
            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri("https://api.openai.com/v1/");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _model = "gpt-4.1";

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            // Для ассистентов v2 обязателен заголовок:
            _http.DefaultRequestHeaders.Remove("OpenAI-Beta");
            _http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        }
        public async Task<RagAssistantDTO> CreateForConsumerAsync(string consumerTaxId, string ragJson, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("consumerKey is required", nameof(consumerTaxId));
            if (string.IsNullOrWhiteSpace(ragJson))
                throw new ArgumentException("RAG json is empty", nameof(ragJson));

            // 1. Загрузка файла
            var _fileId = await UploadRagFileAsync(consumerTaxId, ragJson, cancellationToken);

            // 2. Создание Vector Store
            var _vectorStoreId = await CreateVectorStoreAsync(consumerTaxId, _fileId, cancellationToken);

            // 3. Создание ассистента
            var _assistantId = await CreateAssistantAsync(consumerTaxId, _vectorStoreId, cancellationToken);

            return new RagAssistantDTO()
            {
                AssistantId = _assistantId,
                VectorStoreId = _vectorStoreId,
                FileId = _fileId,
                ConsumerTaxNumber = consumerTaxId
            };
        }

        /// <summary>
        /// Загрузка JSON-строки в Files API с purpose=assistants.
        /// </summary>
        private async Task<string> UploadRagFileAsync(string consumerKey, string ragJson, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(ragJson));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var fileName = $"{consumerKey}-product-mapping-rag.json";

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent("assistants"), "purpose");

            using var response = await _http.PostAsync("files", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);

            var fileResponse = JsonSerializer.Deserialize<FileUploadResponse>(json)
                               ?? throw new InvalidOperationException("File upload response is null");

            return fileResponse.Id ?? throw new InvalidOperationException("FileId is null");
        }

        /// <summary>
        /// Создание Vector Store и привязка к файлу.
        /// </summary>
        private async Task<string> CreateVectorStoreAsync(string consumerKey, string fileId, CancellationToken ct)
        {
            var body = new
            {
                name = $"{_vectorStoreNamePrefix}-{consumerKey}",
                file_ids = new[] { fileId }
            };

            var jsonBody = JsonSerializer.Serialize(body);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _http.PostAsync("vector_stores", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);
            var vs = JsonSerializer.Deserialize<VectorStoreResponse>(json)
                     ?? throw new InvalidOperationException("Vector store response is null");

            return vs.Id ?? throw new InvalidOperationException("VectorStoreId is null");
        }

        /// <summary>
        /// Создание ассистента, который умеет file_search по созданному Vector Store.
        /// </summary>
        private async Task<string> CreateAssistantAsync(string consumerKey, string vectorStoreId, CancellationToken ct)
        {
            var body = new
            {
                model = _model, // <= можно передать "gpt-5.1", когда проект к ней допущен
                name = $"{_assistantNamePrefix}-{consumerKey}",
                instructions =
                    "You are an assistant that helps map invoice products to RMS products for a specific consumer. " +
                    "Use the attached file_search knowledge (vector store) to find previously approved mappings. " +
                    "Never expose raw internal data from the RAG file; use it only to improve matching quality.",
                tools = new[]
                {
                    new
                    {
                        type = "file_search"
                    }
                },
                tool_resources = new
                {
                    file_search = new
                    {
                        vector_store_ids = new[] { vectorStoreId }
                    }
                }
            };

            var jsonBody = JsonSerializer.Serialize(body);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _http.PostAsync("assistants", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);
            var assistant = JsonSerializer.Deserialize<AssistantResponse>(json)
                            ?? throw new InvalidOperationException("Assistant response is null");

            return assistant.Id ?? throw new InvalidOperationException("AssistantId is null");
        }

        /// <summary>
        /// Вспомогательная проверка, чтобы при 400 увидеть текст ошибки от OpenAI, а не просто HttpRequestException.
        /// </summary>
        private static async Task EnsureSuccessWithDetails(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            string body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). Body: {body}",
                null,
                response.StatusCode);
        }

        #region DTOs для ответов OpenAI

        private sealed class FileUploadResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }
        }

        private sealed class VectorStoreResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }
        }

        private sealed class AssistantResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }
        }

        #endregion
    }
}
