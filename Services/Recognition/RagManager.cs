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
    public interface IRagManager
    {
        Task<RagManagementDTO> CreateMappingVectorAsync(
            string consumerKey,
            string ragJson,
            RagManagementDTO? savedRagAssistantDTO,
            CancellationToken cancellationToken = default);

        Task<RagManagementDTO> CreateProductsVectorAsync(
            string consumerKey,
            string ragJson,
            RagManagementDTO? savedRagAssistantDTO,
            CancellationToken cancellationToken = default);



    }

    /// <summary>
    /// Сервис для:
    /// 1) загрузки RAG-файла (JSON) в Files API,
    /// 2) создания/обновления Vector Store (обновление файлов),
    /// 3) создания/обновления ассистента, использующего этот Vector Store через file_search.
    /// </summary>
    public class RagManager : IRagManager
    {
        private readonly HttpClient _http;
        private readonly string _model;
        private readonly string _vectorStoreNamePrefix = "consumer-vs";
        private readonly string _assistantNamePrefix = "consumer-assistant";

        public RagManager(HttpClient httpClient, string apiKey)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri("https://api.openai.com/v1/");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));


            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            // Для ассистентов v2 обязателен заголовок:
            _http.DefaultRequestHeaders.Remove("OpenAI-Beta");
            _http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<RagManagementDTO> CreateMappingVectorAsync(string consumerTaxId, string ragJson, RagManagementDTO? savedRagAssistantDTO, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("consumerKey is required", nameof(consumerTaxId));
            if (string.IsNullOrWhiteSpace(ragJson))
                throw new ArgumentException("RAG json is empty", nameof(ragJson));

            // 1. Всегда загружаем новый файл (полная замена RAG)
            var newFileId = await UploadMappingRagAsync(consumerTaxId, ragJson, cancellationToken);

            string vectorStoreId;
            string assistantId;
             

            // 2. Если vector store ранее не создавался — создаём новый, привязав к нему файл
            if (savedRagAssistantDTO == null ||
                string.IsNullOrWhiteSpace(savedRagAssistantDTO.MappingVectorStoreId))
            {
                vectorStoreId = await CreateVectorStoreAsync(consumerTaxId, newFileId, cancellationToken);
                return new RagManagementDTO
                {
                    MappingVectorStoreId = vectorStoreId,
                    MappingFileId = newFileId,
                    ConsumerTaxNumber = consumerTaxId
                };

            }
            else
            {
                // 3. Vector store уже есть — переиспользуем его
                vectorStoreId = savedRagAssistantDTO.MappingVectorStoreId;

                // 3.1. Если был старый файл — удаляем из vector store и из Files API
                if (!string.IsNullOrWhiteSpace(savedRagAssistantDTO.MappingFileId))
                {
                    await SafeDeleteVectorStoreFileAsync(vectorStoreId, savedRagAssistantDTO.MappingFileId, cancellationToken);
                    await SafeDeleteFileAsync(savedRagAssistantDTO.MappingFileId, cancellationToken);
                }

                // 3.2. Привязываем новый файл к существующему vector store
                await AddFileToVectorStoreAsync(vectorStoreId, newFileId, cancellationToken);

                savedRagAssistantDTO.MappingVectorStoreId = vectorStoreId;
                savedRagAssistantDTO.MappingFileId = newFileId;
                return savedRagAssistantDTO;

            }

        }


        public async Task<RagManagementDTO> CreateProductsVectorAsync(string consumerTaxId, string ragJson, RagManagementDTO? savedRagAssistantDTO, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("consumerKey is required", nameof(consumerTaxId));
            if (string.IsNullOrWhiteSpace(ragJson))
                throw new ArgumentException("RAG json is empty", nameof(ragJson));

            // 1. Всегда загружаем новый файл (полная замена RAG)
            var newFileId = await UploadProductsRagAsync(consumerTaxId, ragJson, cancellationToken);

            string vectorStoreId;
            string assistantId;

            // 2. Если vector store ранее не создавался — создаём новый, привязав к нему файл
            if (savedRagAssistantDTO == null ||
                string.IsNullOrWhiteSpace(savedRagAssistantDTO.ProductsVectorStoreId))
            {
                vectorStoreId = await CreateVectorStoreAsync(consumerTaxId, newFileId, cancellationToken);
                return new RagManagementDTO
                {
                    ProductsVectorStoreId = vectorStoreId,
                    ProductsFileId = newFileId,
                    ConsumerTaxNumber = consumerTaxId
                };

            }
            else
            {
                // 3. Vector store уже есть — переиспользуем его
                vectorStoreId = savedRagAssistantDTO.ProductsVectorStoreId;

                // 3.1. Если был старый файл — удаляем из vector store и из Files API
                if (!string.IsNullOrWhiteSpace(savedRagAssistantDTO.ProductsFileId))
                {
                    await SafeDeleteVectorStoreFileAsync(vectorStoreId, savedRagAssistantDTO.ProductsFileId, cancellationToken);
                    await SafeDeleteFileAsync(savedRagAssistantDTO.ProductsFileId, cancellationToken);
                }

                // 3.2. Привязываем новый файл к существующему vector store
                await AddFileToVectorStoreAsync(vectorStoreId, newFileId, cancellationToken);

                savedRagAssistantDTO.ProductsVectorStoreId = vectorStoreId;
                savedRagAssistantDTO.ProductsFileId = newFileId;
                return savedRagAssistantDTO;

            }

        }

        #region Files API

        /// <summary>
        /// Загрузка JSON-строки в Files API с purpose=assistants.
        /// </summary>
        private async Task<string> UploadMappingRagAsync(string consumerKey, string ragJson, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(ragJson));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var fileName = $"{consumerKey}-mapping-rag.json";

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent("assistants"), "purpose");

            using var response = await _http.PostAsync("files", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);

            var fileResponse = JsonSerializer.Deserialize<FileUploadResponse>(json)
                               ?? throw new InvalidOperationException("File upload response is null");

            return fileResponse.Id ?? throw new InvalidOperationException("FileId is null");
        }

        private async Task<string> UploadProductsRagAsync(string consumerKey, string ragJson, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(ragJson));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var fileName = $"{consumerKey}-products-rag.json";

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent("assistants"), "purpose");

            using var response = await _http.PostAsync("files", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);

            var fileResponse = JsonSerializer.Deserialize<FileUploadResponse>(json)
                               ?? throw new InvalidOperationException("File upload response is null");

            return fileResponse.Id ?? throw new InvalidOperationException("FileId is null");
        }

        private async Task SafeDeleteFileAsync(string fileId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                return;

            try
            {
                using var response = await _http.DeleteAsync($"files/{fileId}", ct);
                // Если уже удалён или не найден — игнорируем
            }
            catch
            {
                // логировать при необходимости
            }
        }

        #endregion

        #region Vector Stores

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
        /// Добавление файла в существующий Vector Store.
        /// POST /vector_stores/{vector_store_id}/files { file_id = "..." }
        /// </summary>
        private async Task AddFileToVectorStoreAsync(string vectorStoreId, string fileId, CancellationToken ct)
        {
            var body = new { file_id = fileId };
            var jsonBody = JsonSerializer.Serialize(body);

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync($"vector_stores/{vectorStoreId}/files", content, ct);
            await EnsureSuccessWithDetails(response);
        }

        /// <summary>
        /// Удаляем файл из vector store (но не сам файл).
        /// </summary>
        private async Task SafeDeleteVectorStoreFileAsync(string vectorStoreId, string fileId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(vectorStoreId) || string.IsNullOrWhiteSpace(fileId))
                return;

            try
            {
                using var response =
                    await _http.DeleteAsync($"vector_stores/{vectorStoreId}/files/{fileId}", ct);
                // Ошибки (404 и т.п.) не считаем критичными
            }
            catch
            {
                // логировать при необходимости
            }
        }

        #endregion

        #region Assistants

        /// <summary>
        /// Создание ассистента, который умеет file_search по созданному Vector Store.
        /// </summary>
        private async Task<string> CreateAssistantAsync(string consumerKey, string vectorStoreId, CancellationToken ct)
        {
            var body = new
            {
                model = _model,
                name = $"{_assistantNamePrefix}-{consumerKey}",
                instructions =
                    "You are an assistant that helps map invoice products to RMS products for a specific consumer. " +
                    "Use the attached file_search knowledge (vector store) to find previously approved mappings. " +
                    "Never expose raw internal data from the RAG file; use it only to improve matching quality.",
                tools = new[]
                {
                    new { type = "file_search" }
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
        /// Обновление существующего ассистента (POST /assistants/{assistant_id}).
        /// </summary>
        private async Task<string> UpdateAssistantAsync(
            string assistantId,
            string consumerKey,
            string vectorStoreId,
            CancellationToken ct)
        {
            var body = new
            {
                model = _model,
                name = $"{_assistantNamePrefix}-{consumerKey}",
                instructions =
                    "You are an assistant that helps map invoice products to RMS products for a specific consumer. " +
                    "Use the attached file_search knowledge (vector store) to find previously approved mappings. " +
                    "Never expose raw internal data from the RAG file; use it only to improve matching quality.",
                tools = new[]
                {
                    new { type = "file_search" }
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

            using var response = await _http.PostAsync($"assistants/{assistantId}", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);
            var assistant = JsonSerializer.Deserialize<AssistantResponse>(json)
                            ?? throw new InvalidOperationException("Assistant response is null");

            return assistant.Id ?? assistantId;
        }

        #endregion

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
