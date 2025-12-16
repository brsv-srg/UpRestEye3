
using Google.Protobuf;
using OpenAI.Responses;
using System.Text.Json;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;



namespace UpRestEye3.Services.Recognition
{
    public class GPTMappingEnvironment
    {
        private readonly string _invoiceAndRmsProductsSchema2;
        // TODO Убрать URL в параметры 
        //private static readonly string _url = "https://api.openai.com/v1/chat/completions";
        private static readonly string _url = "https://api.openai.com/v1/responses";

        // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";

        public GPTMappingEnvironment()
        {
            _invoiceAndRmsProductsSchema2 = JsonHelper.GetMappedProductSchema();
        }

        public string GetInvoiceAndRmsProductsSchema2() => _invoiceAndRmsProductsSchema2;
        public string GetURL() => _url;
        public string GetApiKey() => _apiKey;


        public string GetMappingRequestBody(InvoiceDTO currentInvoice, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> _measUnits, List<RMSAccountDTO> storages, string mappingVectorStoreId)
        {
            var options = JsonHelper.GetSerializerOptions();


            var _invoiceProducts = new List<InvoiceProductMappingDTO>(currentInvoice.Products.Select(p => new InvoiceProductMappingDTO()
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Container = p.Container,
                Count = p.Count,
                Quantity = p.Quantity
            }));
           

            // Проекция для выбора только нужных полей
            var _storages = storages.Select(s => new
            {
                s.Id,
                s.Name
            }).ToList();

            

            // Формируем запрос
            // Формируем тело запроса для /v1/responses
            var requestData = new
            {
                model = "gpt-5.1",
                include = new[] { "file_search_call.results" },
                tool_choice = "required",

                // Просим модель тратить нормальные усилия на рассуждения 
                reasoning = new         
                {
                    effort = "medium"   // варианты: "low", "medium", "high"
                },

                // Основной «developer/system» промпт:
                instructions = _mappingSystemMessage,

                // CHANGED: вместо messages[] → input[]
                input = new object[]
                {
                
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(
                            new
                            {
                                CurrentSupplierTaxNumber = currentInvoice.Supplier.TaxNumber,
                                MeasureUnits = _measUnits,
                                StorageList  = _storages,
                                InvoiceProducts = _invoiceProducts
                            },
                            options)
                    }
                },


                // Описание инструмента file_search по спецификации Responses API
                tools = new object[]
                {
                    new
                    {
                        type = "file_search",
                        vector_store_ids = new[] { mappingVectorStoreId },
                        max_num_results = 10,
                        ranking_options = new
                        {
                            ranker = "auto",
                            score_threshold = 0.2
                        }
                    }
                },


                // Исправленный блок structured outputs для Responses API
                text = new
                {
                    format = new
                    {
                        type = "json_schema",             
                        name = "MatchedInvoiceProducts",   
                        schema = JsonDocument.Parse(GetInvoiceAndRmsProductsSchema2()).RootElement,            
                        strict = true                      
                    }
                }
            };
            return JsonSerializer.Serialize(requestData, options);
        }

        public string GetProductRequestBody(InvoiceDTO currentInvoice, List<InvoiceProductDTO> unmappedProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> _measUnits, List<RMSAccountDTO> storages, string productVectorStoreId)
        {
            var options = JsonHelper.GetSerializerOptions();


            var _invoiceProducts = new List<InvoiceProductMappingDTO>(currentInvoice.Products.Select(p => new InvoiceProductMappingDTO()
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Container = p.Container,
                Count = p.Count,
                Quantity = p.Quantity
            }));


            // Проекция для выбора только нужных полей
            var _storages = storages.Select(s => new
            {
                s.Id,
                s.Name,
                s.Description
            }).ToList();

            var _deliveryService = new
            {
                conParam.DeliveryServiceId,
                conParam.DeliveryServiceName
            };


            // Формируем запрос
            // Формируем тело запроса для /v1/responses
            var requestData = new
            {
                model = "gpt-5.1",
                include = new[] { "file_search_call.results" },
                tool_choice = "required",

                // Просим модель тратить нормальные усилия на рассуждения 
                reasoning = new
                {
                    effort = "medium"   // варианты: "low", "medium", "high"
                },

                // Основной «developer/system» промпт:
                instructions = _productSystemMessage,

                // CHANGED: вместо messages[] → input[]
                input = new object[]
                {
                
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(
                            new
                            {
                                CurrentSupplierTaxNumber = currentInvoice.Supplier.TaxNumber,
                                MeasureUnits = _measUnits,
                                StorageList  = _storages,
                                InvoiceProducts = _invoiceProducts,
                                DeliveryService = _deliveryService,
                                // CHANGED: по желанию можно удалить отсюда MeasureUnits/StorageList,
                                // если они уже положены в RAG.
                            },
                            options)
                    }
                },



                // Описание инструмента file_search по спецификации Responses API
                tools = new object[]
                {
                    new
                    {
                        type = "file_search",
                        vector_store_ids = new[] { productVectorStoreId },
                        max_num_results = 10,
                        ranking_options = new
                        {
                            ranker = "auto",
                            score_threshold = 0.2
                        }
                    }
                },


                // Исправленный блок structured outputs для Responses API
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "MatchedInvoiceProducts",
                        schema = JsonDocument.Parse(GetInvoiceAndRmsProductsSchema2()).RootElement,
                        strict = true
                    }
                }
            };
            return JsonSerializer.Serialize(requestData, options);
        }


        private const string _mappingSystemMessage = @"
You perform deterministic mapping of InvoiceProducts → RMSProducts using a Mapping Catalog only.
Your task is NOT creative. Always choose an existing product if it exists. 

============================================================
1) STRICT INVOICE NAME MATCH (MAPPING CATALOG)
============================================================

Matching keys:
- For each invoice product, call file_search exactly once
- Use ONLY Mapping Catalog RAG.
- Query format MUST be:
  ""<SupplierTaxNumber> | <InvoiceProductName>""

If a Mapping Catalog record matches:
- A mapping is valid only if supplier tax matches AND InvoiceProductName matches exactly.
- Reuse exactly the same MappedRmsProduct
- NewRMSProduct = false
- Determine RMSContainer:
    • if packaging corresponds to a previously used container → reuse it  
    • otherwise reuse another existing matching container, or create a new one according to container logic (below) 
- NewRMSContainer = false unless a new container is created

- If no mapping exists → return InvoiceProduct with RMSProduct = null.

============================================================
2) CONTAINER LOGIC (DETERMINISTIC)
============================================================

Container selection:
- If RMSProduct has a container with identical Count → reuse it  
- Otherwise create a new container (NewRMSContainer = true)
- Never create duplicates (same Count + same MainUnit)

Determine Count:
1) No container:
   - Invoice Unit matches MainUnit AND packaging is irrelevant  
   → RMSContainer = null, NewRMSContainer = false

2) Implied packaging:
   - Extract size from the name (50g, 250g, 0.75l, 330ml)
   - Convert to MainUnit to obtain Count

3) Explicit packaging:
   - Detect patterns (6x1L, 24x33cl, 8x0.5kg, Box 6kg, etc.)
   - Compute Count in MainUnit


============================================================
3) STORAGE ASSIGNMENT
============================================================

Select storage by meaning:
- Food → Kitchen  
- Drinks/alcohol/coffee → Bar  
- Retail goods → Retail  
- Cleaning/technical/disposables → Household  

Use only values from StorageList.


============================================================
4) OUTPUT
============================================================

Return JSON with 'MatchedInvoiceProducts' following schema exactly.
No explanations, comments, or extra text.


";


        private const string _productSystemMessage = @"
You perform TYPE-FIRST mapping of InvoiceProducts → RMSProducts.
Always choose an existing product if it exists. Create new products only when absolutely necessary.

============================================================
1) TYPE-FIRST LOGIC (RMS PRODUCT CATALOG)
============================================================

Goal: find a RMS product with the **same PRODUCT TYPE**.

Matching rules:
- Use ONLY RMS Product Catalog RAG.
- Extract PRODUCT TYPE from the invoice product (potato, avocado, mozzarella, honey, oat milk, microgreens, etc.);
- Ignore brand, packaging, quantity, word order in product name.
- Ignore PRODUCT FORM and PROPERTIES (full-fat/skimmed, fresh/frozen, liquid/solid, whole/sliced, raw/cooked);

- If not found, use common or restaurant synonyms of PRODUCT TYPE (nachos, pickles, etc.) and search again.

- If several products match: use PRODUCT FORM and PROPERTIES to find the closest in hierarchy of restaurant product.
    • use FORM and PROPERTIES **only if several products match**. Ignore in other cases. 


If a product of the same TYPE exists:
- You MUST reuse it;
- NewRMSProduct = false;
- Select or create RMSContainer using container logic (below);

- You MUST NOT create a new RMSProduct of the same type.


============================================================
2) PRODUCT CREATION RULE
============================================================

Create a new RMSProduct only if:
- Previous stage do not found any RMSProduct of the same TYPE (or required FORM differs and FORM is essential)

Creation rules:
- Name = generic English type (""potato"", ""avocado"", ""white wine"", ""oat milk"", ""microgreens"")
- Do NOT include brand, supplier text, packaging, numeric values, or size indicators
- MainUnit = inferred from typical products of the same general category
- Id = null, Num = null
- NewRMSProduct = true

============================================================
3) CONTAINER LOGIC (DETERMINISTIC)
============================================================

A container converts invoice packaging into the RMSProduct MainUnit.

Determine Count:
1) No container:
   - Invoice Unit matches MainUnit AND packaging is irrelevant  
   → RMSContainer = null, NewRMSContainer = false

2) Implied packaging:
   - Extract size from the name (50g, 250g, 0.75l, 330ml)
   - Convert to MainUnit to obtain Count

3) Explicit packaging:
   - Detect patterns (6x1L, 24x33cl, 8x0.5kg, Box 6kg, etc.)
   - Compute Count in MainUnit

Container selection:
- If RMSProduct has a container with identical Count → reuse it  
- Otherwise create a new container (NewRMSContainer = true)
- Never create duplicates (same Count + same MainUnit)

============================================================
4) DELIVERY PRODUCTS
============================================================

If invoice text indicates a delivery fee (Entrega, Portes, Delivery, Transport):
- Map to the provided DeliveryService
- NewRMSProduct = false
- NewRMSContainer = false

============================================================
5) STORAGE ASSIGNMENT
============================================================

Select storage by meaning:
- Food → Kitchen  
- Drinks/alcohol/coffee → Bar  
- Retail goods → Retail  
- Cleaning/technical/disposables → Household  

Use only values from StorageList.

============================================================
6) CRITICAL RESTRICTIONS (STRICT)
============================================================

- NEVER map across product types (fruit ≠ juice, mozzarella ≠ cream cheese, fresh ≠ frozen, dairy ≠ plant-based)
- NEVER create a new product if a correct TYPE exists in RMS catalog
- NEVER treat brand, supplier text, packaging, or numeric text as defining the product type
- ALWAYS include all invoice products in output in the input order
- NEVER merge lines, skip lines, or reinterpret meaning

============================================================
7) OUTPUT
============================================================

Return JSON with 'MatchedInvoiceProducts' following schema exactly.
No explanations, comments, or extra text.

```json
{
  ""MatchedInvoiceProducts"": [
    {
      ""InvoiceProduct"": {
        ""Id"": 1,
        ""ProductCode"": ""123456"",
        ""ProductName"": ""CERVEJA SUPER BOCK 24X33CL"",
        ""Unit"": ""btl"",
        ""Quantity"": 1,
        ""Container"": ""Box 24x33cl"",
        ""Count"": 24
      },
      ""RMSProduct"": {
        ""Id"": 321,
        ""Name"": ""SUPER BOCK CERVEJA"",
        ""Description"": ""Cerveja portuguesa 33cl"",
        ""Num"": ""PRD-00992"",
        ""MainUnit"": ""btl"",
        ""Containers"": [
          {
            ""Id"": 102,
            ""Num"": ""CONT-0054"",
            ""Name"": ""Box 24x33cl"",
            ""Count"": 24
          }
        ]
      },
      ""RMSContainer"": {
        ""Id"": 102,
        ""Num"": ""CONT-0054"",
        ""Name"": ""Box 24x33cl"",
        ""Count"": 24
      },
      ""NewRMSProduct"": false,
      ""NewRMSContainer"": false,
      ""Storage"": ""Bar"",
      ""Comments"": ""Matched by name and packaging""
    }
  ]
}

";


    }
}
