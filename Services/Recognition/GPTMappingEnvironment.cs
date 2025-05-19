
using Google.Protobuf;
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
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
        // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTMappingEnvironment()
        {
            _invoiceAndRmsProductsSchema2 = JsonHelper.GetMappedProductSchema();
        }



        public string GetInvoiceAndRmsProductsSchema2() => _invoiceAndRmsProductsSchema2;

        public string GetURL() => _url;

        public string GetApiKey() => _apiKey;



        public string GetReceiptMappingRequestBody2(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> _measUnits, List<RMSAccountDTO> storages)
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
            var _rmsProducts = new List<RMSProductMappingDTO>(rmsProducts.Select(p => new RMSProductMappingDTO()
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Num = p.Num,
                MainUnit = _measUnits.FirstOrDefault(mu => mu.EntityExtGuid == p.MainUnit)?.Name ?? "pcs", 
                Containers = new List<RMSContainerMappingDTO>(p.Containers.Select(c => new RMSContainerMappingDTO()
                {
                    Id = c.Id,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count
                }))
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

            var requestData = new
            {
                model = "gpt-4.1",
                //model = "gpt-4o", 
                //"gpt-4o-mini",

                temperature = 0.0,
                top_p = 1.0,

                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = _systemMessage
                    },
                    new
                    {
                        role = "user",
                        content = @$"Match the following invoice products with the most appropriate RMS products and packaging from the restaurant system. Use predefined rules and return the results in the predefined JSON in the response_format section."
                    },
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { InvoiceProducts = _invoiceProducts}, options),

                    },

                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { RMSProducts = _rmsProducts }, options),

                    },


                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { DeliveryService = _deliveryService }, options),

                    },


                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { MeasureUnits = _measUnits,  }, options),

                    },

                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { StorageList = _storages,  }, options),
                    },
                },

                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "MatchedInvoiceProducts",
                        schema = JsonDocument.Parse(GetInvoiceAndRmsProductsSchema2()).RootElement
                    }
                }
            };
            return JsonSerializer.Serialize(requestData, options);
        }
        


        private const string _systemMessage = @"
You are an AI assistant that maps OCR-extracted 'InvoiceProducts' to corresponding 'RMSProducts' from a restaurant management system.  
Your goal is to return a stable and structured JSON response that preserves the input data and ensures exact matching by name, unit, and container logic.

---

## ✅ General Matching Strategy

1. **Preserve Input**  
   - Copy each 'InvoiceProduct' to the 'InvoiceProduct' field of the output object without modifications.

2. **Product Matching Rules**  
   - Match based on semantic similarity of 'ProductName', 'Brand', 'Volume', and key attributes (e.g. 'white wine' ≠ 'red wine').
   - Always compare **product type and form**:
     - Do **not** match fundamentally different product forms (e.g. **fruit** ≠ **fruit juice**, **fresh** ≠ **frozen**, **raw** ≠ **cooked**).
     - Brand can be ignored **only** if not essential to product identity.
   - Unit consistency is **critical**. Use 'MeasureUnits' to compare and validate:
     - 'kg', 'g' → weight  
     - 'l', 'ml' → volume  
     - 'pcs', 'unit' → discrete count
   - Be especially cautious when matching products with **different base units**:
     - E.g. **liters vs. kilograms** — match only if it's clearly the same product and unit conversion is justified by context (e.g. **yogurt** in liters vs. kg).

3. **Unit Normalization**  
   - Never use packaging-specific units (e.g., 'btl0.75') as a base unit.
   - The valid base unit is always defined by the `MainUnit` of the matched `RMSProduct`.
   - If 'InvoiceProduct.Unit' is compound or irregular, normalize it **via a container**:  
     - Find or create a container that accurately translates the invoice quantity into the RMSProduct's `MainUnit`.
   - ⚠️ If `RMSProduct.MainUnit = pcs`, avoid creating containers that define only volume/weight per item — treat them as attributes, not packaging.
   - For detailed logic, refer to the **📦 Container (Packaging) Matching Logic** section below.

4. **If no match is found — Create new RMSProduct**  
   - Fill:
     - 'Name': concise, general product name  
       - Remove supplier- or brand-specific fragments unless essential to product identity  
       - Keep descriptive attributes (e.g., 'shallot onion', 'butterfly pasta')  
       - If product is a branded item (e.g., 'Coca-Cola') — keep full brand name
     - 'Description': inferred from category or traits  
     - 'MainUnit': inferred from similar RMSProducts (e.g. wine → 'L', beer → 'btl', rice → 'kg')  
   - 'Id' and 'Num': null  
   - 'NewRMSProduct = true'

---
## 📦 Container (Packaging) Matching Logic

A **container** defines standard packaging (e.g. 'Box 6KG', '24x0.33L') and connects invoice packaging to the RMSProduct's base unit.  
Its 'Count' must always reflect total weight or volume in the 'MainUnit' of the RMSProduct.

> 🔁 **Important:** Rules 1,2,3 are mutually exclusive —  
> if one rule matches and is applied, the following rules must be skipped.

### 🔹 Rules:

1. **If no packaging info**  
   - If 'InvoiceProduct.Unit' is base ('kg', 'l', 'pcs'), matches `RMSProduct.MainUnit`, and no container is defined:  
     - Set 'RMSContainer = null'  
     - Set 'NewRMSContainer = false'

2. **If unit characteristic, not true packaging**  
   - If `RMSProduct.MainUnit = pcs` and:
     - the container value `InvoiceProduct.Count' equals **1 unit worth of volume/weight** (e.g., `0.25L`, `100g`)
   - Then treat it as a product attribute, not as a packaging:
     - Set `RMSContainer = null`  
     - Set `NewRMSContainer = false`  
     - Normalize invoice:
       - `InvoiceProduct.Container = ""`  
       - `InvoiceProduct.Count = null`

3. **If packaging/multipack is indicated**  
    
    3.1. **Define container info**
         - Derive container info from 'InvoiceProduct.Container' or 'InvoiceProduct.ProductName'  
         - Compute total content in 'RMSProduct.MainUnit':  
            - e.g. '8 x 0.5kg' → 'Count = 4.0'  
            - e.g. 'Btl 0.75l' → 'Count = 0.75'

    3.2 **Match existing container**  
         - Search `RMSProduct.Containers` for container with **same 'Count'** and compatible unit  
            - Container name may differ — must represent same meaning or be neutral (e.g., `Box 4kg`, `6x0.75L`)  
         - If match is found:  
            - Reuse container  
         - Set `NewRMSContainer = false`

    3.3. **Create new container if needed**  
         - If no matching container exists:
             - `Name`: descriptive (e.g., `Pack 4kg`, `Box 6x1L`)  
             - `Count`: calculated in `RMSProduct.MainUnit`  
             - `Id`, `Num`: null  
             - Set `NewRMSContainer = true`

    3.4. **Avoid duplication**  
        - Never create container if one with same `Count` in same unit already exists  
        - Prefer existing containers with neutral names

✅ **Final validation**:  
Always check that the total quantity implied by the `RMSContainer.Count × InvoiceProduct.QuantityOfContainers` matches the total amount in the original `InvoiceProduct` (based on unit and container logic).

---

## 📦 Delivery Product Handling

- If a product in the invoice matches delivery-related terms (e.g., 'Entrega', 'Portes', 'Delivery', 'Transport'):  
  - Treat as a normal InvoiceProduct  
  - Use provided 'DeliveryService' object in input  
  - Copy it into 'RMSProduct'  
  - Set 'NewRMSProduct = false', 'NewRMSContainer = false'

---

## 🏷 Storage Assignment

Choose appropriate 'Storage' from provided 'StorageList', based on product type:

| Product Type                                             | Storage     |
|----------------------------------------------------------|-------------|
| Food, groceries, ingredients                             | 'Kitchen'   |
| Alcohol, drinks, coffee, mixers                          | 'Bar'       |
| Retail, takeaway, resale items                           | 'Retail'    |
| Cleaning/technical supplies                              | 'Household' |

- Always use one of the values from 'StorageList'  
- Match based on product meaning, not literal string

---

## 📤 Output Format

Return a JSON object with 'MatchedInvoiceProducts'. Each item must follow this structure:

```json
{
  ""MatchedInvoiceProducts"": [
    {
      ""InvoiceProduct"": {
        ""Id"": 1,
        ""ProductCode"": ""123456"",
        ""ProductName"": ""CERVEJA SUPER BOCK 24X33CL"",
        ""Unit"": ""btl"",
        ""Container"": ""Box 24x33cl"",
        ""UnitsCount"": 24,
        ""QuantityOfContainers"": 1
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

"
;



    }
}
