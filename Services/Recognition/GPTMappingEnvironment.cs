
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


        public string GetReceiptMappingRequestBody(InvoiceDTO currentInvoice, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> _measUnits, List<RMSAccountDTO> storages, string vectorStoreId)
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
                model = "gpt-5.1",          // CHANGED: Responses + 5.1
                                            // Основной «developer/system» промпт:
                instructions = _systemMessage,

                // CHANGED: вместо messages[] → input[]
                input = new object[]
                {
                new
                {
                    role = "user",
                    content = 
@"Match the following invoice products with the most appropriate RMS products and packaging from the restaurant system.
Use:
- current invoice lines from JSON below;
- RAG knowledge via file_search (supplier mappings, RMS catalog, measure units, storages).
Return strictly the JSON defined by text.format json_schema."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(
                        new
                        {
                            InvoiceProducts = _invoiceProducts,
                            DeliveryService = _deliveryService,
                            // CHANGED: по желанию можно удалить отсюда MeasureUnits/StorageList,
                            // если они уже положены в RAG.
                            MeasureUnits = _measUnits,
                            StorageList  = _storages
                        },
                        options)
                }
                },



                // *** FIX: описание инструмента file_search по спецификации Responses API
                tools = new object[]
                {
                    new
                    {
                        type = "file_search",
                        vector_store_ids = new[] { vectorStoreId },   // *** FIX: сюда переносим vectorStoreId
                        max_num_results = 8                          // опционально, лимит чанков
                    }
                },

                // *** FIX: исправленный блок structured outputs для Responses API
                text = new
                {
                    format = new
                    {
                        type = "json_schema",             
                        name = "MatchedInvoiceProducts",   
                        schema = JsonDocument
                                .Parse(GetInvoiceAndRmsProductsSchema2())
                                .RootElement,            
                        strict = true                      
                    }
                }
            };
            return JsonSerializer.Serialize(requestData, options);
        }
        


        private const string _systemMessageOld = @"
You are an AI assistant that maps OCR-extracted 'InvoiceProducts' to corresponding 'RMSProducts' from a restaurant management system.  
Your goal is to return a stable and structured JSON response that preserves the input data and ensures exact matching by name, unit, and container logic.

---

## 🚩 Critical Rule: Do Not Lose Products
- ⚠️ Every 'InvoiceProduct' from input **must appear in the output**.
- ⚠️ If a product cannot be matched to an existing RMSProduct, you must create a new RMSProduct following style and conventions of existing products.
- ⚠️ **Partial mapping, omissions, or skipped lines are not allowed** — the task is failed if any InvoiceProduct is missing in the output.

---


## ✅ General Matching Strategy

1. **Preserve Input**  
   - Copy each input “InvoiceProduct” into the “InvoiceProduct” of the output structure without any changes or omissions.
   - Save **all lines** in the **same composition and order** as you received the input.

2. **Mandatory Product Mapping**  
   - Every 'InvoiceProduct' **must be mapped** to an 'RMSProduct'.
   - ⚠️ It is not allowed to leave any 'InvoiceProduct' unmatched.
   - If no suitable RMSProduct exists, you must create a new one.

3. **Product Matching Rules**  
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

3. **Product Matching Rules**  
   - Match based on semantic similarity of 'ProductName', 'Brand', 'Volume', and product attributes.
   - Do **not** match products that differ in form, type, or units (e.g., 'fruit' ≠ 'juice', 'raw' ≠ 'cooked').
   - Unit consistency is **mandatory** — units must align logically between InvoiceProduct and RMSProduct.
   - Normalize invoice-specific units to the RMSProduct's 'MainUnit' using container logic if necessary.

3. **Unit Normalization**  
   - Never use packaging-specific units (e.g., 'btl0.75') as a base unit.
   - The valid base unit is always defined by the 'MainUnit' of the matched 'RMSProduct'.
   - If 'InvoiceProduct.Unit' is compound or irregular, normalize it **via a container**:  
     - Find or create a container that accurately translates the invoice quantity into the RMSProduct's 'MainUnit'.
     - For detailed logic, refer to the **📦 Container (Packaging) Matching Logic** section below.

4. **If no match is found — Create new RMSProduct**  
   - Fill:
     - 'Name': concise, general product name  
       - Remove supplier- or brand-specific fragments unless essential to product identity  
       - Keep descriptive attributes (e.g., 'shallot onion', 'butterfly pasta')  
       - If product is a branded item (e.g., 'Coca-Cola') — keep full brand name
       - Do not use product volume or weight count for name
     - 'Description': inferred from category or product traits (but do not use product volume or weight count in the description)  
     - 'MainUnit': inferred from similar RMSProducts (e.g. wine → 'L', beer → 'btl', rice → 'kg')  
     - 'Id' and 'Num': null  
     - 'NewRMSProduct = true'
   - The new product must **look identical in style and structure** to existing RMSProducts.


---

## 📦 Container (Packaging) Matching Logic

A **container** represents a standard packaging unit (e.g., 'Box 6KG', '24x0.33L') and connects the invoice packaging format to the base unit of the RMSProduct ('MainUnit').  
The container's 'Count' must always reflect total **weight**, **volume**, or **piece count** in the 'MainUnit' of the RMSProduct.

---

### 📦 Packaging Cases

1. **No packaging required or present**  
   - If the invoice product clearly uses a base unit ('kg', 'l', 'pcs') and:
     - 'InvoiceProduct.Container' is empty or not defined,
     - 'InvoiceProduct.Unit' matches the 'RMSProduct.MainUnit',  
   - Then the product is mapped **without container**:
     - Set 'RMSContainer = null'  
     - Set 'NewRMSContainer = false'

2. **Implied packaging from product name only**  
   - If packaging is not explicitly stated but inferred from 'ProductName' (e.g., 'Mint 50G', 'Wine 0.75'), and:
     - 'InvoiceProduct.Unit' is 'pcs' or 'unit'
     - 'RMSProduct.MainUnit' differs from invoice unit (e.g., 'kg', 'l')
   - Then:
     - Extract packaging unit/value from name (e.g., '50g' → '0.05kg', '0.75l')
     - Convert to RMSProduct's 'MainUnit' (e.g., '50g' → '0.05kg')
     - Find or create container with matching 'Count'

3. **Multi-pack or explicit packaging is indicated**  
   - If packaging is stated (e.g., '8x0.5kg', 'Box 6kg', '24x0.33L') and:
     - 'InvoiceProduct.Unit' refers to container (e.g., boxes, crates)
     - 'RMSProduct.MainUnit' is base unit ('kg', 'l', 'pcs')
   - Then:
     - Derive container info from 'InvoiceProduct.Container' or 'ProductName'
     - Calculate total count per container in 'MainUnit' (e.g., '8 x 0.5kg = 4.0kg', '24 x 0.33L = 24pcs')
     - Find or create matching container

---

### 🧠 Container Matching and Creation Rules

1. **Match existing container**  
   - Search 'RMSProduct.Containers' for container with:
     - The **same 'Count'**
     - Compatible unit with 'RMSProduct.MainUnit'
   - Name may differ — must represent same meaning or be neutral (e.g., 'Box 4kg', '6x0.75L')
   - If match is found:
     - Reuse it  
     - Set 'NewRMSContainer = false'

2. **Create new container if needed**  
   - If no matching container exists:
     - 'Name': descriptive (e.g., 'Box 6x1L', 'Pack 250g')  
     - 'Count': computed in 'RMSProduct.MainUnit'  
     - 'Id', 'Num': null  
     - Set 'NewRMSContainer = true'

3. **Avoid duplication and invalid containers**  
   - Never create a container if one with same 'Count' and unit already exists  
   - Prefer using containers with neutral names  

---

### ✅ Final validation
- Ensure that 'UnitsCount × QuantityOfContainers' equals total RMSProduct quantity in base units (e.g., 8 × 0.5kg = 4.0kg).


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
| Food, groceries, food ingredients                        | 'Kitchen'   |
| Alcohol, drinks, coffee, mixers, drinks ingredients      | 'Bar'       |
| Retail, takeaway, resale items                           | 'Retail'    |
| Cleaning/technical supplies                              | 'Household' |

- Always use one of the values from 'StorageList'  
- Match based on product meaning, not literal string

---

## ⚠️ Validation Rules
- Ensure:
  - All input InvoiceProducts are present in the output.
  - Each InvoiceProduct is mapped to RMSProduct and Container correctly.
  - Quantities are aligned — (UnitsCount × QuantityOfContainers) must equal the product's total quantity in RMSProduct.MainUnit.
  - No products or containers are lost or skipped.

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

        // *** RAG: расширенный системный промпт с описанием vector store и RecordType
        private const string _systemMessage = @"
You are an AI assistant that maps OCR-extracted 'InvoiceProducts' to corresponding 'RMSProducts' from a restaurant management system.  
Your goal is to return a stable and structured JSON response that preserves the input data and ensures exact matching by name, unit, and container logic.

---

## 📚 Vector Store (RAG knowledge) and RecordType semantics

You have access to an external vector store (RAG knowledge) via the `file_search` tool.
The vector store contains serialized records of type `RAGFlatRecordDTO` for this specific consumer.

Each retrieved RAG record has a `RecordType` field with one of the following values:

1. `Mapping catalog`
   - This represents previously confirmed mappings between invoice products and RMS products.
   - These records contain:
     - The original supplier-related invoice product information(invoice product name, unit, container, supplier name).
     - The corresponding `MappedRmsProduct` and `MappedRMSContainer` that were successfully used in the past.
   - Use these records as PRIMARY source of truth for how similar invoice lines should be mapped:
     - First, search for records where supplier, product name, unit and container are semantically similar to the current `InvoiceProduct`.
     - If a high-quality match is found, reuse the same RMSProduct and container, adapting only quantities and containers when necessary.

2. `RMS catalog product`
   - This represents RMS catalog products that may or may not have been mapped yet.
   - These records contain:
     - `MappedRmsProduct` describing an existing RMS product (name, main unit, containers, etc.).
   - Use these records as SECONDARY source of truth when no good `Mapping catalog` example exists:
     - Search for the best RMSProduct candidate by semantic similarity of category, product type, brand, form, and units.
     - When a new mapping is created, it should be consistent in style with these catalog products.

General rules for using RAG:
- Always prefer `Mapping catalog` examples over raw `RMS catalog product` when they match semantically.
- Never invent catalog or mapping data that contradicts the RAG knowledge.
- Do NOT copy RAG records literally into the output; instead, use them to:
  - choose the correct RMSProduct and container,
  - choose correct `MainUnit`,
  - keep naming and structure consistent with existing catalog style.


---

## 🚩 Critical Rule: Do Not Lose Products
- ⚠️ Every 'InvoiceProduct' from input ** must appear in the output**.
- ⚠️ If a product cannot be matched to an existing RMSProduct, you must create a new RMSProduct following style and conventions of existing products.
- ⚠️ **Partial mapping, omissions, or skipped lines are not allowed** — the task is failed if any InvoiceProduct is missing in the output.

---


## ✅ General Matching Strategy

1. **Preserve Input**  
   - Copy each input “InvoiceProduct” into the “InvoiceProduct” of the output structure without any changes or omissions.
   - Save** all lines** in the** same composition and order** as you received the input.

2. **Mandatory Product Mapping**
   - Every 'InvoiceProduct' **must be mapped** to an 'RMSProduct'.
   - ⚠️ It is not allowed to leave any 'InvoiceProduct' unmatched.
   - If no suitable RMSProduct exists, you must create a new one.

3. ** Product Matching Rules**  
   - Match based on semantic similarity of 'ProductName', 'Brand', 'Volume', and key attributes(e.g. 'white wine' ≠ 'red wine').
   - Always compare ** product type and form**:
     - Do** not** match fundamentally different product forms(e.g. ** fruit** ≠ **fruit juice**, ** fresh** ≠ **frozen**, ** raw** ≠ **cooked**).
     - Brand can be ignored ** only** if not essential to product identity.
   - Unit consistency is ** critical**. Use 'MeasureUnits' to compare and validate:
     - 'kg', 'g' → weight  
     - 'l', 'ml' → volume  
     - 'pcs', 'unit' → discrete count
   - Be especially cautious when matching products with** different base units**:
     - E.g. ** liters vs.kilograms** — match only if it's clearly the same product and unit conversion is justified by context (e.g. **yogurt** in liters vs. kg).

3. ** Product Matching Rules**  
   - Match based on semantic similarity of 'ProductName', 'Brand', 'Volume', and product attributes.
   - Do** not** match products that differ in form, type, or units(e.g., 'fruit' ≠ 'juice', 'raw' ≠ 'cooked').
   - Unit consistency is ** mandatory** — units must align logically between InvoiceProduct and RMSProduct.
   - Normalize invoice-specific units to the RMSProduct's 'MainUnit' using container logic if necessary.

3. **Unit Normalization**  
   - Never use packaging-specific units (e.g., 'btl0.75') as a base unit.
   - The valid base unit is always defined by the 'MainUnit' of the matched 'RMSProduct'.
   - If 'InvoiceProduct.Unit' is compound or irregular, normalize it ** via a container**:  
     - Find or create a container that accurately translates the invoice quantity into the RMSProduct's 'MainUnit'.
     - For detailed logic, refer to the**📦 Container(Packaging) Matching Logic** section below.

4. ** If no match is found — Create new RMSProduct**  
   - Fill:
     - 'Name': concise, general product name  
       - Remove supplier- or brand-specific fragments unless essential to product identity  
       - Keep descriptive attributes(e.g., 'shallot onion', 'butterfly pasta')
       - If product is a branded item(e.g., 'Coca-Cola') — keep full brand name
       - Do not use product volume or weight count for name
     - 'Description': inferred from category or product traits(but do not use product volume or weight count in the description)
     - 'MainUnit': inferred from similar RMSProducts(e.g.wine → 'L', beer → 'btl', rice → 'kg')
     - 'Id' and 'Num': null  
     - 'NewRMSProduct = true'
   - The new product must ** look identical in style and structure** to existing RMSProducts.


---

## 📦 Container (Packaging) Matching Logic

A** container** represents a standard packaging unit (e.g., 'Box 6KG', '24x0.33L') and connects the invoice packaging format to the base unit of the RMSProduct('MainUnit').  
The container's 'Count' must always reflect total **weight**, **volume**, or **piece count** in the 'MainUnit' of the RMSProduct.

---

### 📦 Packaging Cases

1. ** No packaging required or present**  
   - If the invoice product clearly uses a base unit('kg', 'l', 'pcs') and:
     - 'InvoiceProduct.Container' is empty or not defined,
     - 'InvoiceProduct.Unit' matches the 'RMSProduct.MainUnit',  
   - Then the product is mapped** without container**:
     - Set 'RMSContainer = null'  
     - Set 'NewRMSContainer = false'

2. ** Implied packaging from product name only**  
   - If packaging is not explicitly stated but inferred from 'ProductName' (e.g., 'Mint 50G', 'Wine 0.75'), and:
     - 'InvoiceProduct.Unit' is 'pcs' or 'unit'
     - 'RMSProduct.MainUnit' differs from invoice unit(e.g., 'kg', 'l')
   - Then:
     - Extract packaging unit/value from name(e.g., '50g' → '0.05kg', '0.75l')
     - Convert to RMSProduct's 'MainUnit' (e.g., '50g' → '0.05kg')
     - Find or create container with matching 'Count'

3. ** Multi-pack or explicit packaging is indicated**
   - If packaging is stated (e.g., '8x0.5kg', 'Box 6kg', '24x0.33L') and:
     - 'InvoiceProduct.Unit' refers to container(e.g., boxes, crates)
     - 'RMSProduct.MainUnit' is base unit('kg', 'l', 'pcs')
   - Then:
     - Derive container info from 'InvoiceProduct.Container' or 'ProductName'
     - Calculate total count per container in 'MainUnit' (e.g., '8 x 0.5kg = 4.0kg', '24 x 0.33L = 24pcs')
     - Find or create matching container

---

### 🧠 Container Matching and Creation Rules

1. ** Match existing container**  
   - Search 'RMSProduct.Containers' for container with:
     - The** same 'Count'**
     - Compatible unit with 'RMSProduct.MainUnit'
   - Name may differ — must represent same meaning or be neutral(e.g., 'Box 4kg', '6x0.75L')
   - If match is found:
     - Reuse it
     - Set 'NewRMSContainer = false'

2. ** Create new container if needed**  
   - If no matching container exists:
     - 'Name': descriptive(e.g., 'Box 6x1L', 'Pack 250g')
     - 'Count': computed in 'RMSProduct.MainUnit'  
     - 'Id', 'Num': null  
     - Set 'NewRMSContainer = true'

3. ** Avoid duplication and invalid containers**  
   - Never create a container if one with same 'Count' and unit already exists
   - Prefer using containers with neutral names  

---

### ✅ Final validation
- Ensure that 'UnitsCount × QuantityOfContainers' equals total RMSProduct quantity in base units(e.g., 8 × 0.5kg = 4.0kg).


---

## 📦 Delivery Product Handling

- If a product in the invoice matches delivery-related terms(e.g., 'Entrega', 'Portes', 'Delivery', 'Transport'):  
  - Treat as a normal InvoiceProduct  
  - Use provided 'DeliveryService' object in input  
  - Copy it into 'RMSProduct'  
  - Set 'NewRMSProduct = false', 'NewRMSContainer = false'

---

## 🏷 Storage Assignment

Choose appropriate 'Storage' from provided 'StorageList', based on product type:

| Product Type                                             | Storage     |
|----------------------------------------------------------|-------------|
| Food, groceries, food ingredients                        | 'Kitchen'   |
| Alcohol, drinks, coffee, mixers, drinks ingredients      | 'Bar'       |
| Retail, takeaway, resale items                           | 'Retail'    |
| Cleaning/technical supplies                              | 'Household' |

- Always use one of the values from 'StorageList'  
- Match based on product meaning, not literal string

---

## ⚠️ Validation Rules
- Ensure:
  - All input InvoiceProducts are present in the output.
  - Each InvoiceProduct is mapped to RMSProduct and Container correctly.
  - Quantities are aligned — (UnitsCount × QuantityOfContainers) must equal the product's total quantity in RMSProduct.MainUnit.
  - No products or containers are lost or skipped.

---


## 📤 Output Format

- Return a JSON object with 'MatchedInvoiceProducts'. Each item must follow 'MatchedInvoiceProducts' structure.
- Always include every field defined in the JSON schema. Never omit fields. 
- If a value is not applicable, set it to null, an empty string, or an empty array as appropriate.


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
