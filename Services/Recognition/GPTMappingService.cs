using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.Recognition
{
    public interface IGPTMappingService
    {
        Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, ConnectionParameterDTO conParam,
            List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages, CancellationToken ct = default);
    }

    /// <summary>
    /// DB-first mapping:
    /// 1) Exact DB match: Name+Container+Count + ok stage statuses
    /// 2) DB name match: candidates RMS+containers
    /// 3) LLM chooses from candidates (no RAG)
    /// 4) Remaining -> LLM searches in product catalog embedded in system prompt (cached)
    /// 5) Merge all results
    /// </summary>
    public sealed class GPTMappingService : IGPTMappingService
    {
        private readonly GPTMappingEnvironment _env;
        private readonly HttpClient _http;

        // Эти интерфейсы вы подключите к вашей БД (EF/ADO/Dapper)
        private readonly IInvoiceMappingHistoryRepository _historyRepo;
        private readonly IProductCatalogProvider _catalogProvider;

        public GPTMappingService(
            IInvoiceMappingHistoryRepository historyRepo,
            IProductCatalogProvider catalogProvider,
            HttpClient? httpClient = null,
            GPTMappingEnvironment? env = null)
        {
            _env = env ?? new GPTMappingEnvironment();
            _historyRepo = historyRepo;
            _catalogProvider = catalogProvider;

            _http = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(7)
            };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _env.GetApiKey());
        }

        public async Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(
            InvoiceDTO currentInvoice,
            ConnectionParameterDTO conParam,
            List<RMSMeasureUnitDTO> measUnits,
            List<RMSAccountDTO> storages,
            CancellationToken ct = default)
        {
            try
            {
                var all = new List<MatchedInvoiceProduct>();

                // -----------------------------
                // 0) Подготовка
                // -----------------------------
                var invoiceProducts = currentInvoice.Products?.ToList() ?? new List<InvoiceProductDTO>();
                if (invoiceProducts.Count == 0)
                    return all;

                var supplierTax = currentInvoice.Supplier?.TaxNumber ?? string.Empty;
                var consumerId = currentInvoice.Consumer.Id;

                // Нормализуем инвойс-строки один раз
                var normalized = invoiceProducts.ToDictionary(
                    p => p.Id,
                    p => new NormalizedInvoiceKey(
                        ProductId: (int)p.Id,
                        SupplierTaxNumber: supplierTax,
                        NameNorm: InvoiceHelper.NormalizeName(p.ProductName ?? ""),
                        ContainerNorm: InvoiceHelper.NormalizeContainer(p.Container ?? ""),
                        CountNorm: InvoiceHelper.NormalizeCount(p.Count ?? 0m)));

                // -----------------------------
                // 1) DB exact: name+container+count + успешные стадии
                // -----------------------------
                var exactHits = await _historyRepo.FindLatestExactMatchesAsync(
                    (int)consumerId,
                    supplierTax,
                    normalized.Values.ToList(),
                    ct);

                // exactHits: productId -> StoredMapping (RMS, container, storage, taxCategory)
                foreach (var hit in exactHits)
                {
                    var p = invoiceProducts.First(x => x.Id == hit.ProductId);

                    all.Add(_env.BuildMatchedFromStored(p, hit, measUnits));
                }

                var exactMappedIds = exactHits.Select(x => x.ProductId).ToHashSet();

                // -----------------------------
                // 2) DB by name only -> candidates for LLM resolve
                // -----------------------------
                var remainingAfterExact = invoiceProducts.Where(p => !exactMappedIds.Contains((int)p.Id)).ToList();
                if (remainingAfterExact.Count == 0)
                    return all;

                var normalized2 = remainingAfterExact
                    .Select(p => new NormalizedInvoiceKey(
                        ProductId: (int)p.Id,
                        SupplierTaxNumber: supplierTax,
                        NameNorm: InvoiceHelper.NormalizeName(p.ProductName ?? ""),
                        ContainerNorm: InvoiceHelper.NormalizeContainer(p.Container ?? ""),
                        CountNorm: InvoiceHelper.NormalizeCount(p.Count ?? 0m)
                    ))
                    .ToList();


                var nameCandidates = await _historyRepo.FindNameCandidatesAsync(
                    (int)consumerId,
                    supplierTax,
                    normalized2,
                    ct);

                // nameCandidates: productId -> list of StoredMappingCandidate (RMS + containers + storage + taxCategory maybe)
                // Берём только те, где есть хотя бы 1 кандидат RMS
                var needResolveFromCandidates = remainingAfterExact
                    .Where(p => nameCandidates.TryGetValue((int)p.Id, out var c) && c != null && c.Count > 0)
                    .ToList();

                if (needResolveFromCandidates.Count > 0)
                {
                    var resolveBody = _env.BuildResolveFromDbCandidatesRequestBody(
                        currentInvoice,
                        needResolveFromCandidates,
                        nameCandidates,
                        conParam,
                        measUnits,
                        storages);

                    var resolved = await CallOpenAIAndParseAsync(resolveBody, ct);

                    // Мержим и отмечаем какие закрыли
                    all.AddRange(resolved);

                    var resolvedIds = resolved
                        .Where(x => x?.InvoiceProduct != null)
                        .Select(x => x.InvoiceProduct.Id)
                        .ToHashSet();

                    remainingAfterExact = remainingAfterExact.Where(p => !resolvedIds.Contains(p.Id)).ToList();
                }

                // -----------------------------
                // 3) Remaining -> LLM search in product catalog in system prompt (cached)
                // -----------------------------
                if (remainingAfterExact.Count > 0)
                {
                    var catalogText = await _catalogProvider.GetProductCatalogTextAsync(currentInvoice, ct);

                    var catalogBody = _env.BuildProductCatalogSearchRequestBody(
                        currentInvoice,
                        remainingAfterExact,
                        conParam,
                        measUnits,
                        storages,
                        catalogText);

                    var catalogMapped = await CallOpenAIAndParseAsync(catalogBody, ct);

                    all.AddRange(catalogMapped);
                }

                // -----------------------------
                // 4) Финальный merge (сохраняем порядок инвойса)
                // -----------------------------
                var byId = all
                    .Where(x => x?.InvoiceProduct != null)
                    .GroupBy(x => x.InvoiceProduct.Id)
                    .ToDictionary(g => g.Key, g => g.First()); // при конфликте — берём первый (exact > resolve > catalog, т.к. мы так добавляли)

                var ordered = new List<MatchedInvoiceProduct>(invoiceProducts.Count);
                foreach (var p in invoiceProducts)
                {
                    if (byId.TryGetValue(p.Id, out var mapped))
                    {
                        ordered.Add(mapped);
                        continue;
                        continue;
                    }

                    // fallback: если вообще ничего — пустой ответ для строки
                    ordered.Add(_env.BuildEmptyMatched(p, storages));
                }

                return ordered;
            }
            catch (Exception ex)
            {
                currentInvoice.StageStatus = InvoiceStatusEnum.Error;
                throw new Exception("Error mapping invoice products to RMS products (DB-first pipeline)", ex);
            }
        }

        // -----------------------------
        // OpenAI call + parse (ваш парсер сохранён по сути)
        // -----------------------------
        private async Task<List<MatchedInvoiceProduct>> CallOpenAIAndParseAsync(string requestJson, CancellationToken ct)
        {
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(_env.GetURL(), content, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"OpenAI API returned {(int)resp.StatusCode} ({resp.ReasonPhrase}). Body: {error}");
            }

            var body = await resp.Content.ReadAsStringAsync(ct);
            var parsed = ResponseParsing(body);
            return parsed;
        }

        private List<MatchedInvoiceProduct> ResponseParsing(string responseContent)
        {
            // Взято из вашей текущей реализации, адаптировано на List<>
            // :contentReference[oaicite:2]{index=2}
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                if (!root.TryGetProperty("output", out var outputArray) ||
                    outputArray.ValueKind != JsonValueKind.Array ||
                    outputArray.GetArrayLength() == 0)
                    throw new Exception("OpenAI response does not contain non-empty 'output' array.");

                JsonElement? messageElement = null;
                foreach (var item in outputArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var typeEl) &&
                        typeEl.ValueKind == JsonValueKind.String &&
                        typeEl.GetString() == "message")
                    {
                        messageElement = item;
                        break;
                    }
                }
                if (messageElement is null)
                    throw new Exception("OpenAI response does not contain 'message' output.");

                var msg = messageElement.Value;

                if (!msg.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array ||
                    contentArray.GetArrayLength() == 0)
                    throw new Exception("OpenAI response 'message' does not contain non-empty 'content' array.");

                JsonElement? outputTextElement = null;
                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var ctType) &&
                        ctType.ValueKind == JsonValueKind.String &&
                        ctType.GetString() == "output_text")
                    {
                        outputTextElement = contentItem;
                        break;
                    }
                }
                if (outputTextElement is null)
                    throw new Exception("OpenAI response 'content' does not contain 'output_text' item.");

                var ot = outputTextElement.Value;
                if (!ot.TryGetProperty("text", out var textElement))
                    throw new Exception("OpenAI 'output_text' does not contain 'text' field.");

                string? jsonPayload = textElement.ValueKind == JsonValueKind.String
                    ? textElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(jsonPayload))
                    throw new Exception("OpenAI 'output_text.text' is null or empty.");

                using var payloadDoc = JsonDocument.Parse(jsonPayload);
                var payloadRoot = payloadDoc.RootElement;

                if (!payloadRoot.TryGetProperty("MatchedInvoiceProducts", out var matchedArray) ||
                    matchedArray.ValueKind != JsonValueKind.Array)
                    throw new Exception("Parsed payload JSON does not contain 'MatchedInvoiceProducts' array.");

                var options = JsonHelper.GetSerializerOptions();

                var list = JsonSerializer.Deserialize<List<MatchedInvoiceProduct>>(
                               matchedArray.GetRawText(),
                               options)
                           ?? new List<MatchedInvoiceProduct>();

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to MatchedInvoiceProducts object", ex);
            }
        }
    }


    // -----------------------------
    // DTOs for DB hits
    // -----------------------------
    public sealed record NormalizedInvoiceKey(int ProductId, string SupplierTaxNumber, string NameNorm, string ContainerNorm, decimal? CountNorm);

    public sealed record StoredExactHit(
        int ProductId,
        RMSProductDTO RmsProduct,
        RMSContainerDTO? RmsContainer,
        string? StorageName,
        string? TaxCategoryCodeOrName);

    public sealed record StoredCandidate(
        int ProductId,
        RMSProductDTO RmsProduct,
        List<RMSContainerDTO> Containers,
        // optional "last chosen" hints:
        RMSContainerDTO? LastChosenContainer,
        string? LastChosenStorage,
        string? LastChosenTaxCategory);
}
