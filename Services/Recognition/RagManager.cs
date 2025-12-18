using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;

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
    /// Новый режим загрузки RAG:
    /// - Mapping Catalog: 1 файл = 1 пара InvoiceProduct↔RMSProduct (один JSON-объект из массива)
    /// - RMS Product Catalog: 1 файл = 1 RMSProduct (один JSON-объект из массива)
    ///
    /// Вектор-сторы раздельные: mapping / products.
    /// </summary>
    public sealed class RagManager : IRagManager
    {
        private readonly HttpClient _http;

        private const int UploadConcurrency = 6;
        private const int FileBatchSize = 100;

        private readonly string _vectorStoreNamePrefixMapping = "consumer-vs-mapping";
        private readonly string _vectorStoreNamePrefixProducts = "consumer-vs-products";

        public RagManager(HttpClient httpClient, string apiKey)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri("https://api.openai.com/v1/");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Assistants v2 / Vector stores
            _http.DefaultRequestHeaders.Remove("OpenAI-Beta");
            _http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<RagManagementDTO> CreateMappingVectorAsync(
            string consumerTaxId,
            string ragJson,
            RagManagementDTO? saved,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("consumerTaxId is required", nameof(consumerTaxId));
            if (string.IsNullOrWhiteSpace(ragJson))
                throw new ArgumentException("ragJson is empty", nameof(ragJson));

            // 1) Split JSON строго под Mapping Catalog (массив объектов)
            var mappingItems = SplitMappingCatalogJson(consumerTaxId, ragJson);

            if (mappingItems.Count == 0)
                throw new InvalidOperationException("No mapping records found in ragJson (RecordType=Mapping Catalog).");

            // удаление всего
            await DeleteAllFilesAsync(cancellationToken);
            // закоментировать выше после 

            // 2) Create/Reuse vector store
            var vectorStoreId = await EnsureVectorStoreAsync(
                consumerTaxId,
                kind: "mapping",
                existingVectorStoreId: saved?.MappingVectorStoreId,
                cancellationToken);

            // 3) Очистить store (detach+delete), чтобы не копить мусор
            await ClearVectorStoreAsync(vectorStoreId, cancellationToken);

            // 4) Upload files
            var fileIds = await UploadManyFilesAsync(mappingItems, cancellationToken);

            // 5) Attach via file_batches (батчами) + wait completed
            //await AddFilesToVectorStoreWithMetaAsync (vectorStoreId, fileIds, cancellationToken);
            await AddFilesToVectorStoreInBatchesWithAttributesAsync(vectorStoreId, fileIds, cancellationToken);


            saved ??= new RagManagementDTO();
            saved.ConsumerTaxNumber = consumerTaxId;
            saved.MappingVectorStoreId = vectorStoreId;

            return saved;
        }

        public async Task<RagManagementDTO> CreateProductsVectorAsync(
            string consumerTaxId,
            string ragJson,
            RagManagementDTO? saved,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("consumerTaxId is required", nameof(consumerTaxId));
            if (string.IsNullOrWhiteSpace(ragJson))
                throw new ArgumentException("ragJson is empty", nameof(ragJson));

            // 1) Split JSON строго под RMS Product Catalog (массив объектов)
            var productItems = SplitProductsCatalogJson(consumerTaxId, ragJson);

            if (productItems.Count == 0)
                throw new InvalidOperationException("No product records found in ragJson (RecordType=RMS Product Catalog).");

            // 2) Create/Reuse vector store
            var vectorStoreId = await EnsureVectorStoreAsync(
                consumerTaxId,
                kind: "products",
                existingVectorStoreId: saved?.ProductsVectorStoreId,
                cancellationToken);

            // 3) Очистить store
            await ClearVectorStoreAsync(vectorStoreId, cancellationToken);

            // 4) Upload files
            var fileIds = await UploadManyFilesAsync(productItems, cancellationToken);

            // 5) Attach via file_batches + wait completed
            //await AddFilesToVectorStoreInBatchesAsync(vectorStoreId, fileIds, cancellationToken);
            await AddFilesToVectorStoreInBatchesWithAttributesAsync(vectorStoreId, fileIds, cancellationToken);

            saved ??= new RagManagementDTO();
            saved.ConsumerTaxNumber = consumerTaxId;
            saved.ProductsVectorStoreId = vectorStoreId;

            return saved;
        }

        // =========================
        // JSON splitting utilities
        // =========================

        /// <summary>
        /// Mapping-файл: массив объектов вида как в RAG_Mapping_*.json
        /// (RecordType, EmbeddingText, SupplierName, SupplierTaxNumber, InvoiceProductId, ...).
        /// 1 объект => 1 файл.
        /// </summary>
        private static List<RagFileToUpload> SplitMappingCatalogJson(string consumerTaxId, string ragJson)
        {
            var options = JsonHelper.GetSerializerOptions();

            using var doc = JsonDocument.Parse(ragJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Mapping ragJson must be a JSON array.");

            var result = new List<RagFileToUpload>();
            int index = 0;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                var supplierTax = GetString(el, "SupplierTaxNumber");
                var invoiceProductId = GetInt32Nullable(el, "InvoiceProductId");
                var invoiceName = GetString(el, "InvoiceProductName");

                index++;

                // Имя файла делаем устойчивым и диагностичным
                var safeSupplierTax = SanitizeFilePart(string.IsNullOrWhiteSpace(supplierTax) ? "unknown" : supplierTax);
                var safeInvoiceId = invoiceProductId?.ToString() ?? $"idx{index}";
                var safeNamePart = SanitizeFilePart(Shorten(invoiceName, 40));

                var fileName = $"{consumerTaxId}-map-{safeSupplierTax}-{safeInvoiceId}-{safeNamePart}.json";

                var json = JsonSerializer.Serialize(el, options);
                result.Add(new RagFileToUpload(fileName, json, supplierTax));
            }

            return result;
        }

        /// <summary>
        /// Products-файл: массив объектов вида как в RAG_Products_*.json
        /// (RecordType, EmbeddingText, Id, Name, MainUnit, Containers[]).
        /// 1 объект => 1 файл.
        /// </summary>
        private static List<RagFileToUpload> SplitProductsCatalogJson(string consumerTaxId, string ragJson)
        {
            var options = JsonHelper.GetSerializerOptions();

            using var doc = JsonDocument.Parse(ragJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Products ragJson must be a JSON array.");

            var result = new List<RagFileToUpload>();
            int index = 0;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                var id = GetInt32Nullable(el, "Id");
                var name = GetString(el, "Name");

                index++;

                var safeId = id?.ToString() ?? $"idx{index}";
                var safeName = SanitizeFilePart(Shorten(name, 60));

                var fileName = $"{consumerTaxId}-prod-{safeId}-{safeName}.json";

                var json = JsonSerializer.Serialize(el, options);
                result.Add(new RagFileToUpload(fileName, json, null));
            }

            return result;
        }

        private static string GetString(JsonElement obj, string propName)
        {
            if (obj.ValueKind != JsonValueKind.Object) return string.Empty;
            if (!obj.TryGetProperty(propName, out var p)) return string.Empty;
            return p.ValueKind == JsonValueKind.String ? (p.GetString() ?? string.Empty) : string.Empty;
        }

        private static int? GetInt32Nullable(JsonElement obj, string propName)
        {
            if (obj.ValueKind != JsonValueKind.Object) return null;
            if (!obj.TryGetProperty(propName, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v)) return v;
            return null;
        }

        private static string Shorten(string? s, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(s)) return "noname";
            s = s.Trim();
            return s.Length <= maxLen ? s : s.Substring(0, maxLen);
        }

        private static string SanitizeFilePart(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch == '-' || ch == '_') sb.Append(ch);
                else sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        // =========================
        // Files API
        // =========================

        private async Task<List<RagFileToConnectToVector>> UploadManyFilesAsync(List<RagFileToUpload> files, CancellationToken ct)
        {
            var result = new List<RagFileToConnectToVector>();
            using var sem = new SemaphoreSlim(UploadConcurrency);

            var tasks = files.Select(async (f, idx) =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    result.Add(new RagFileToConnectToVector(await UploadFileAsync(f.FileName, f.Content, ct), f.SupplierTaxNumber));

                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unexpected error in DeleteAllFilesAsync: {ex.Message}");
                    throw;
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;
        }

        private async Task<string> UploadFileAsync(string fileName, string json, CancellationToken ct)
        {
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent("assistants"), "purpose");

            using var response = await _http.PostAsync("files", content, ct);
            await EnsureSuccessWithDetails(response);

            var respJson = await response.Content.ReadAsStringAsync(ct);
            var fileResponse = JsonSerializer.Deserialize<FileUploadResponse>(respJson)
                              ?? throw new InvalidOperationException("File upload response is null");

            return fileResponse.Id ?? throw new InvalidOperationException("FileId is null");
        }

        private async Task SafeDeleteFileAsync(string fileId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                return;

            try { using var _ = await _http.DeleteAsync($"files/{fileId}", ct); }
            catch { /* log if needed */ }
        }

        // =========================
        // Vector Stores
        // =========================

        private async Task<string> EnsureVectorStoreAsync(
            string consumerKey,
            string kind,
            string? existingVectorStoreId,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(existingVectorStoreId))
                return existingVectorStoreId;

            var name = kind.Equals("mapping", StringComparison.OrdinalIgnoreCase)
                ? $"{_vectorStoreNamePrefixMapping}-{consumerKey}"
                : $"{_vectorStoreNamePrefixProducts}-{consumerKey}";

            var body = new { name };
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _http.PostAsync("vector_stores", content, ct);
            await EnsureSuccessWithDetails(response);

            var json = await response.Content.ReadAsStringAsync(ct);
            var vs = JsonSerializer.Deserialize<VectorStoreResponse>(json)
                     ?? throw new InvalidOperationException("Vector store response is null");

            return vs.Id ?? throw new InvalidOperationException("VectorStoreId is null");
        }

        /// <summary>
        /// Полная очистка store:
        /// - list all files in store
        /// - detach each file from store
        /// - delete file from Files API
        /// </summary>
        private async Task ClearVectorStoreAsync(string vectorStoreId, CancellationToken ct)
        {
            var fileIds = await ListAllVectorStoreFileIdsAsync(vectorStoreId, ct);

            foreach (var fileId in fileIds)
            {
                await SafeDeleteVectorStoreFileAsync(vectorStoreId, fileId, ct);
                await SafeDeleteFileAsync(fileId, ct);
            }
        }

        private async Task<List<string>> ListAllVectorStoreFileIdsAsync(string vectorStoreId, CancellationToken ct)
        {
            var ids = new List<string>();
            string? after = null;

            while (true)
            {
                var url = $"vector_stores/{vectorStoreId}/files?limit=100";
                if (!string.IsNullOrWhiteSpace(after))
                    url += $"&after={Uri.EscapeDataString(after)}";

                using var response = await _http.GetAsync(url, ct);
                await EnsureSuccessWithDetails(response);

                var json = await response.Content.ReadAsStringAsync(ct);
                var page = JsonSerializer.Deserialize<VectorStoreFilesListResponse>(json)
                           ?? throw new InvalidOperationException("Vector store files list response is null");

                if (page.Data != null)
                {
                    foreach (var f in page.Data)
                        if (!string.IsNullOrWhiteSpace(f.Id))
                            ids.Add(f.Id);
                }

                if (page.HasMore != true || string.IsNullOrWhiteSpace(page.LastId))
                    break;

                after = page.LastId;
            }

            return ids;
        }

        private async Task SafeDeleteVectorStoreFileAsync(string vectorStoreId, string fileId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(vectorStoreId) || string.IsNullOrWhiteSpace(fileId))
                return;

            try { using var _ = await _http.DeleteAsync($"vector_stores/{vectorStoreId}/files/{fileId}", ct); }
            catch { /* log if needed */ }
        }

        private async Task AddFilesToVectorStoreInBatchesAsync(string vectorStoreId, List<RagFileToConnectToVector> fileIds, CancellationToken ct)
        {
            for (int i = 0; i < fileIds.Count; i += FileBatchSize)
            {
                var batchIds = fileIds.Skip(i).Take(FileBatchSize).ToArray();
                var body = new { file_ids = batchIds };

                using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync($"vector_stores/{vectorStoreId}/file_batches", content, ct);
                await EnsureSuccessWithDetails(response);

                var json = await response.Content.ReadAsStringAsync(ct);
                var batch = JsonSerializer.Deserialize<VectorStoreFileBatchResponse>(json);

                if (!string.IsNullOrWhiteSpace(batch?.Id))
                    await WaitForBatchCompletedAsync(vectorStoreId, batch.Id!, ct);
            }
        }

        private async Task AddFilesToVectorStoreInBatchesWithAttributesAsync( string vectorStoreId, List<RagFileToConnectToVector> fileIds, CancellationToken ct)
        {
            for (int i = 0; i < fileIds.Count; i += FileBatchSize)
            {
                var batch = fileIds.Skip(i).Take(FileBatchSize)
                    .Select(x => new
                    {
                        file_id = x.FileId,
                        attributes = new Dictionary<string, string>
                            {
                                { "SupplierTaxNumber", x.SupplierTaxNumber ?? string.Empty }
                            },
                    })
                    .ToArray();

                var body = new { files = batch };

                using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var response = await _http.PostAsync($"vector_stores/{vectorStoreId}/file_batches", content, ct);

                await EnsureSuccessWithDetails(response);

                var json = await response.Content.ReadAsStringAsync(ct);
                var batchResp = JsonSerializer.Deserialize<VectorStoreFileBatchResponse>(json);

                if (!string.IsNullOrWhiteSpace(batchResp?.Id))
                    await WaitForBatchCompletedAsync(vectorStoreId, batchResp.Id!, ct);
            }
        }




        private async Task AddFilesToVectorStoreWithMetaAsync(string vectorStoreId, List<RagFileToConnectToVector> fileIds, CancellationToken ct)
        {
            const int concurrency = 6;
            using var sem = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            foreach (var file in fileIds)
            {
                await sem.WaitAsync(ct);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await AddFileToVectorStoreAsync(
                            vectorStoreId,
                            file.FileId,
                            new Dictionary<string, string>
                            {
                                { "SupplierTaxNumber", file.SupplierTaxNumber ?? string.Empty }
                            },
                            ct);
                    }
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine($"Unexpected error in DeleteAllFilesAsync: {ex.Message}");
                        throw;
                    }
                    finally
                    {
                        sem.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);
        }

        private async Task AddFileToVectorStoreAsync(
            string vectorStoreId,
            string fileId,
            Dictionary<string, string> metadata,
            CancellationToken ct)
        {
            var body = new
            {
                file_id = fileId,
                attributes = metadata
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.PostAsync(
                $"vector_stores/{vectorStoreId}/files",
                content,
                ct);

            await EnsureSuccessWithDetails(response);
        }

        public async Task DeleteAllFilesAsync(CancellationToken ct)
        {
            try
            {
                string? last = null;

                do
                {
                    // 1) Получаем страницу файлов
                    var url = "files?limit=100";
                        //+(last != null ? $"&after={Uri.EscapeDataString(last)}" : "");

                    using var response = await _http.GetAsync(url, ct);
                    await EnsureSuccessWithDetails(response);

                    var json = await response.Content.ReadAsStringAsync(ct);
                    var page = JsonSerializer.Deserialize<FileListResponse>(json);

                    if (page?.Data == null || page.Data.Count == 0)
                        break;

                    // 2) Удаляем каждый файл
                    foreach (var file in page.Data)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(file.Id))
                                continue;

                            using var deleteResponse =
                                await _http.DeleteAsync($"files/{file.Id}", ct);

                            await EnsureSuccessWithDetails(deleteResponse);
                        }
                        catch
                        { }
                    }

                    // 3) Следующая страница
                    last = page.LastId; // или LastId / NextCursor — как в вашем JSON
                }
                while (!string.IsNullOrEmpty(last));
            }
            catch (Exception ex)
            {
                // Логирование всех остальных ошибок
                Console.Error.WriteLine($"Unexpected error in DeleteAllFilesAsync: {ex.Message}");
                throw;
            }
        }



        private async Task WaitForBatchCompletedAsync(string vectorStoreId, string batchId, CancellationToken ct)
        {
            while (true)
            {
                using var response = await _http.GetAsync($"vector_stores/{vectorStoreId}/file_batches/{batchId}", ct);
                await EnsureSuccessWithDetails(response);

                var json = await response.Content.ReadAsStringAsync(ct);
                var batch = JsonSerializer.Deserialize<VectorStoreFileBatchResponse>(json)
                            ?? throw new InvalidOperationException("Vector store file batch response is null");

                if (string.Equals(batch.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    return;

                if (string.Equals(batch.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(batch.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Vector store batch {batchId} finished with status={batch.Status}");

                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
            }
        }

        // =========================
        // Helpers / DTOs
        // =========================

        private static async Task EnsureSuccessWithDetails(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). Body: {body}",
                null,
                response.StatusCode);
        }

        private sealed record RagFileToUpload(string FileName, string Content, string? SupplierTaxNumber);
        private sealed record RagFileToConnectToVector(string FileId, string? SupplierTaxNumber);

        private sealed class FileUploadResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class VectorStoreResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class VectorStoreFilesListResponse
        {
            [JsonPropertyName("data")]
            public List<VectorStoreFileObject>? Data { get; set; }

            [JsonPropertyName("has_more")]
            public bool? HasMore { get; set; }

            [JsonPropertyName("last_id")]
            public string? LastId { get; set; }
        }

        public sealed class FileListResponse
        {
            [JsonPropertyName("data")]
            public List<FileItem> Data { get; set; } = new();

            [JsonPropertyName("has_more")]
            public bool HasMore { get; set; }

            [JsonPropertyName("last_id")]
            public string? LastId { get; set; }
        }

        public sealed class FileItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("filename")]
            public string FileName { get; set; } = "";
        }



        private sealed class VectorStoreFileObject
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class VectorStoreFileBatchResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }
    }
}
