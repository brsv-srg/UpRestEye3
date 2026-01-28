using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.Recognition
{
    public sealed class GPTMappingEnvironment
    {
        private readonly string _schema;
        private static readonly string _url = "https://api.openai.com/v1/responses";
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";

        public GPTMappingEnvironment()
        {
            _schema = JsonHelper.GetMappedProductSchema();
        }

        public string GetURL() => _url;
        public string GetApiKey() => _apiKey;


        // -----------------------------
        // BUILD OUTPUT HELPERS (DB exact path)
        // -----------------------------
        public MatchedInvoiceProduct BuildMatchedFromStored(InvoiceProductDTO p, StoredExactHit hit,  List<RMSMeasureUnitDTO> measUnits)

        {
            // Ваша схема output не содержит TaxCategory, поэтому:
            // - либо вы расширяете MatchedInvoiceProduct DTO,
            // - либо храните TaxCategory отдельно (в вашем пайплайне).
            // Здесь: Comments содержит указание, что TaxCategory взята из истории.
            return new MatchedInvoiceProduct
            {
                InvoiceProduct = new InvoiceProductMappingDTO
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    Container = p.Container ?? "",
                    Count = p.Count ?? 0
                },
                RMSProduct = new RMSProductMappingDTO
                {
                    Id = hit.RmsProduct.Id,
                    Name = hit.RmsProduct.Name,
                    Description = hit.RmsProduct.Description,
                    Num = hit.RmsProduct.Num,
                    MainUnit = measUnits.FirstOrDefault(u => u.EntityExtGuid == hit.RmsProduct.MainUnit)?.Name ?? string.Empty,

                    Containers = hit.RmsProduct.Containers?.Select(c => new RMSContainerMappingDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Num = c.Num,
                        Count = c.Count
                    }).ToList() ?? new List<RMSContainerMappingDTO>()
                },

                RMSContainer = new RMSContainerMappingDTO
                {
                    Id = hit.RmsContainer?.Id,
                    Name = hit.RmsContainer?.Name,
                    Num = hit.RmsContainer?.Num,
                    Count = hit.RmsContainer?.Count ?? 0
                },
                
                NewRMSProduct = false,
                NewRMSContainer = false,
                Storage = hit.StorageName ?? InferStorageFallback(p),
                Comments = $"DB exact hit (incl. storage/tax if available). Tax={hit.TaxCategoryCodeOrName ?? "n/a"}"
            };
        }

        public MatchedInvoiceProduct BuildEmptyMatched(InvoiceProductDTO p, List<RMSAccountDTO> storages)
        {
            return new MatchedInvoiceProduct
            {
                InvoiceProduct = new InvoiceProductMappingDTO
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    Container = p.Container ?? "",
                    Count = p.Count ?? 0
                },
                RMSProduct = new RMSProductMappingDTO

                {
                    Id = null,
                    Name = "",
                    Description = "",
                    Num = null,
                    MainUnit = "",
                    Containers = new List<RMSContainerMappingDTO>()

                },
                RMSContainer = new RMSContainerMappingDTO { Id = null, Name = "", Count = 0, Num = null },
                NewRMSProduct = false,
                NewRMSContainer = false,
                Storage = InferStorageFallback(p),
                Comments = "No mapping found (fallback empty)"
            };
        }

        private string InferStorageFallback(InvoiceProductDTO p)
        {
            // простой fallback; реальную логику у вас уже есть в промптах
            var name = (p.ProductName ?? "").ToUpperInvariant();
            if (name.Contains("CERVEJA") || name.Contains("COLA") || name.Contains("VINHO") || name.Contains("BEB"))
                return "Bar";
            return "Kitchen";
        }

        // -----------------------------
        // REQUEST BUILDERS
        // -----------------------------

        /// <summary>
        /// Stage 3: LLM chooses RMS+Container+Storage from DB candidates.
        /// No file_search tools used (deterministic candidate set).
        /// </summary>
        public string BuildResolveFromDbCandidatesRequestBody(
            InvoiceDTO currentInvoice,
            List<InvoiceProductDTO> invoiceProducts,
            Dictionary<int, List<StoredCandidate>> candidatesByProductId,
            ConnectionParameterDTO conParam,
            List<RMSMeasureUnitDTO> measUnits,
            List<RMSAccountDTO> storages)
        {
            var options = JsonHelper.GetSerializerOptions();

            var storageList = storages.Select(s => new { s.Id, s.Name }).ToList();

            // компактный input: только нужные поля
            var inputPayload = new
            {
                CurrentSupplierTaxNumber = currentInvoice.Supplier?.TaxNumber,
                MeasureUnits = measUnits.Select(mu => new { mu.Id, mu.Name }).ToList(),
                StorageList = storageList,
                InvoiceProducts = invoiceProducts.Select(p => new
                {
                    p.Id,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.Quantity,
                    p.Container,
                    p.Count
                }).ToList(),
                Candidates = invoiceProducts.Select(p => new
                {
                    InvoiceProductId = p.Id,
                    InvoiceNameNormalized = InvoiceHelper.NormalizeName(p.ProductName ?? ""),
                    CandidateRmsProducts = (candidatesByProductId.TryGetValue((int)p.Id, out var cands) ? cands : new List<StoredCandidate>())
                        .GroupBy(x => x.RmsProduct.Id) // unique RMS
                        .Select(g =>
                        {
                            var first = g.First();
                            return new
                            {
                                RMSProduct = first.RmsProduct,
                                Containers = first.Containers,
                                LastChosenContainer = first.LastChosenContainer,
                                LastChosenStorage = first.LastChosenStorage,
                                LastChosenTaxCategory = first.LastChosenTaxCategory
                            };
                        })
                        .ToList()
                }).ToList()
            };

            var requestData = new
            {
                model = "gpt-5.1",
                tool_choice = "none",
                reasoning = new { effort = "low" },

                instructions = BuildResolveFromCandidatesSystemPrompt(),

                input = new object[]
                {
                    new { role = "user", content = JsonSerializer.Serialize(inputPayload, options) }
                },

                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "MatchedInvoiceProducts",
                        schema = JsonDocument.Parse(_schema).RootElement,
                        strict = true
                    }
                }
            };

            return JsonSerializer.Serialize(requestData, options);
        }

        /// <summary>
        /// Stage 4: LLM search in product catalog embedded in system prompt. Enables prompt caching.
        /// </summary>
        public string BuildProductCatalogSearchRequestBody(
            InvoiceDTO currentInvoice,
            List<InvoiceProductDTO> invoiceProducts,
            ConnectionParameterDTO conParam,
            List<RMSMeasureUnitDTO> measUnits,
            List<RMSAccountDTO> storages,
            string productCatalogText)
        {
            var options = JsonHelper.GetSerializerOptions();

            var storageList = storages.Select(s => new { s.Id, s.Name }).ToList();

            var deliveryService = new
            {
                conParam.DeliveryServiceId,
                conParam.DeliveryServiceName
            };

            // Каталог строго в instructions (для кеша), input — только инвойс-линии и справочники
            var inputPayload = new
            {
                CurrentSupplierTaxNumber = currentInvoice.Supplier?.TaxNumber,
                DeliveryService = deliveryService,
                MeasureUnits = measUnits.Select(mu => new { mu.Id, mu.Name }).ToList(),
                StorageList = storageList,
                InvoiceProducts = invoiceProducts.Select(p => new
                {
                    p.Id,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.Quantity,
                    p.Container,
                    p.Count
                }).ToList()
            };

            var cacheKey = BuildCatalogCacheKey(currentInvoice, productCatalogText);

            var requestData = new
            {
                model = "gpt-5.1",
                tool_choice = "none",
                reasoning = new { effort = "low" },

                // prompt caching (если ваша версия Responses API поддерживает)
                prompt_cache_key = cacheKey,
                prompt_cache_retention = "24h",

                instructions = BuildCatalogSystemPrompt(productCatalogText),

                input = new object[]
                {
                    new { role = "user", content = JsonSerializer.Serialize(inputPayload, options) }
                },

                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "MatchedInvoiceProducts",
                        schema = JsonDocument.Parse(_schema).RootElement,
                        strict = true
                    }
                }
            };

            return JsonSerializer.Serialize(requestData, options);
        }

        private string BuildCatalogCacheKey(InvoiceDTO invoice, string catalogText)
        {
            // ключ должен быть стабильным
            var consumer = invoice.Consumer?.TaxNumber ?? "unknown";

            // Формируем логический источник ключа
            var rawKey = $"prodcat|{consumer}|{catalogText}";
            var hash = Sha256Hex(rawKey);

            return hash[..64];

        }

        private static string Sha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string BuildResolveFromCandidatesSystemPrompt()
        {
            return @"
You perform DETERMINISTIC resolution of invoice products
using ONLY the provided CandidateRmsProducts (from DB history).

STRICT RULES:
- You MUST NOT invent products.
- You MUST NOT use file_search or any external knowledge.
- Selection is deterministic, not creative.

============================================================
1) RMS PRODUCT SELECTION
============================================================

For each InvoiceProduct:
- Choose the single best RMSProduct from CandidateRmsProducts.
- Selection must be based on the closest PRODUCT TYPE and historical usage.
- If several candidates exist, choose the one most frequently or most recently used.
- Do NOT create new RMSProducts in this stage.
- NewRMSProduct = false always.

============================================================
2) CONTAINER SELECTION / CREATION
============================================================

Container logic is deterministic and based on invoice packaging.

Selection:
- If invoice specifies container/count:
  - Reuse RMSProduct.Containers with identical Count (preferred).
- If no matching container exists:
  - Create a new container using deterministic container logic
  - Id = null, Num = null
  - NewRMSContainer = true

If invoice has no relevant packaging:
- RMSContainer = null
- NewRMSContainer = false

============================================================
3) STORAGE SELECTION
============================================================

- Prefer LastChosenStorage for this RMSProduct if provided AND logically applicable.
- Otherwise infer storage by meaning using StorageList
  (Food→Kitchen, Drinks→Bar, Retail→Retail, Cleaning/technical→Household).
- Use ONLY values from StorageList.

============================================================
4) TAX CATEGORY
============================================================

- Reuse the LastChosenTaxCategory associated with the selected RMSProduct.
- Do NOT infer, change, or invent tax categories.

============================================================
OUTPUT
============================================================

Return STRICT JSON following the schema.
No explanations, comments, or extra text outside JSON.
";

        }

        private static string BuildCatalogSystemPrompt(string catalogText)
        {

            string systemRequest = $@"
You perform DETERMINISTIC mapping of InvoiceProducts → RMSProducts
using ONLY the provided RMS PRODUCT CATALOG below.

RMS PRODUCT CATALOG (authoritative):
{catalogText}

============================================================
CORE PRINCIPLES (STRICT)
============================================================

- REUSE > CREATE. If a correct PRODUCT TYPE exists, you MUST reuse it.
- In the case of several InvoiceProducts of the same type, and only one suitable RMSProducts, reuse it several times
- Create a new RMSProduct ONLY if NO product of the same TYPE exists at all.
- NEVER map across incompatible types
  (fruit ≠ juice, dairy ≠ plant-based, fresh ≠ frozen, mozzarella ≠ cream cheese).
- This task is NOT creative.

============================================================
1) TYPE-FIRST MATCHING
============================================================

- Extract PRODUCT TYPE from invoice name
  (e.g. potato, avocado, mozzarella, honey, oat milk, microgreens).
- Ignore brand, supplier text, packaging, quantities, numeric values, and word order.
- Search catalog by TYPE only.

If no direct TYPE match:
- Retry using common / restaurant synonyms of the same TYPE.

If MULTIPLE RMS products match the same TYPE:
- Use FORM / PROPERTIES ONLY as a tie-breaker to select the closest one.
- FORM / PROPERTIES MUST NOT be used to justify creating a new product.

If a TYPE match exists:
- Reuse that RMSProduct.
- NewRMSProduct = false.

============================================================
2) PRODUCT CREATION (LAST RESORT)
============================================================

Create a new RMSProduct ONLY if:
- No RMSProduct of the same TYPE exists in the catalog.

Creation rules:
- Name = generic English TYPE (""potato"", ""avocado"", ""oat milk"", etc.)
- Exclude brand, supplier text, packaging, numeric values, size indicators.
- MainUnit = typical unit for this TYPE
  (kg for food solids, l for liquids, pcs for countable items;
   prefer units already used by similar catalog products).
- Id = null, Num = null
- NewRMSProduct = true

============================================================
3) CONTAINER LOGIC (DETERMINISTIC)
============================================================

A container converts invoice packaging into RMSProduct.MainUnit.

Container selection rules:
- If RMSProduct has a container with identical Count → reuse it (NewRMSContainer = false)
- Otherwise create a new container (NewRMSContainer = true)
  Id = null, Num = null, generic Name allowed (e.g. ""Pack {{Count}}{{MainUnit}}"").
- Never create duplicates (same Count + same MainUnit)

Determine Count:

1) No container:
   - Invoice Unit == MainUnit AND packaging irrelevant
   → RMSContainer = null, NewRMSContainer = false

2) Implied packaging:
   - Extract size from name (50g, 250g, 0.75l, 330ml)
   - Convert to MainUnit → Count

3) Explicit packaging:
   - Detect patterns (6x1L, 24x33cl, 8x0.5kg, Box 6kg, etc.)
   - Compute Count in MainUnit

============================================================
4) STORAGE ASSIGNMENT
============================================================

Select Storage strictly from StorageList:
- Food → Kitchen
- Drinks / alcohol / coffee → Bar
- Retail goods → Retail
- Cleaning / technical / disposables → Household

============================================================
5) DELIVERY LINES
============================================================

If invoice line indicates delivery
(Entrega, Portes, Delivery, Transport):
- Map to provided DeliveryService
- NewRMSProduct = false
- NewRMSContainer = false

============================================================
OUTPUT
============================================================

Return STRICT JSON following the schema.
No explanations, comments, or extra text.
";
            return systemRequest;
        }
    }
}
