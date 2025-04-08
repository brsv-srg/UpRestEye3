
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



        public string GetReceiptMappingRequestBody2(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts, List<RMSMeasureUnitDTO> _measUnits, List<RMSAccountDTO> storages)
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
                Unit = p.MainUnit.ToString(), // todo убрать GUID, заменить на реальный юнит
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


            // Формируем запрос

            var requestData = new
            {
                model = "gpt-4o", //"gpt-4o-mini",

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
        
        private const string _storagePrompt = $@"- Also, for each product, choose a Storage to which it should go. Use the following Storages for this purpose: ""{{0}}"". Add the name of the selected Storage to the Product Storage field in the response_format section.";





        private const string _systemMessageOld = @$"
You are an AI assistant that matches OCR-extracted invoice products with products from the restaurant management system. 
Your primary task is to ensure accurate mapping of `InvoiceProducts` to `RMSProducts`, considering product names, units, and packaging, and put mapped products into the predefined JSON in the response_format section. 
If a product is missing in the RMS system, create a new one following the structured guidelines.

**Matching Rules**:
- Take each product from the `InvoiceProducts` list and **copy all fields** of Invoice Product to the predefined JSON in the response_format section.
- Match the InvoiceProducts with the most appropriate RMSProducts and packaging from the restaurant system.
- Return a JSON object that strictly follows the response_format section. Ensure all fields are correctly formatted and do not omit any required fields.
- Product names may be abbreviated or translated (Portuguese - English).
- Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
    - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name **but differ by color/type**, so they are **different products**. 
    - This should be taken into account for any different brands of wine or types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
    - Identify the main properties of both products - red or white, salty or sweet, frozen or fresh, and so on. And based on these properties, make your decision. 
    - Do not match products with different properties, even if they belong to the same brand.
- If a RMSProduct exists, **copy all fields** to the predefined JSON in the response_format section.
- If a RMSProduct exists but packaging does not, create a new packaging option as RMSContainer. Fill only the **Name and Count** (that means quantity, volume). Do not fill the **Id and Num fields**.
- If no matching RMSProduct exists, create a new one with a clear name. Fill only the **Name, Description and Unit**. Do not fill the **Id and Num fields**.
- When creating a new RMSProduct, also find the most appropriate Measure Unit for the new product in the **MeasureUnits dictionary** from the input data. Be careful, for weight products use kilograms (kg), for liquid products use litres (l), for piece products use pieces (pcs). Put the GUID of the selected Measure Unit into the mainUnit field of the new RMSProduct. 
- In the end, the measure units of the RMSProduct should logically match RMSContainers and the units of measure in InvoiceProduct. Check for compatibility again, if something does not match, then create a new RMSContainer or a new RMSProduct.
- **Minimize incorrect mappings** – when in doubt, prefer creating a new product.
{{0}}

Example Matching:
- InvoiceProduct: ""MORGADO QUINTAO BRANCO 2023 75CL 12%"" 
- RMSProduct: ""MORGADO QUINTAO BRANCO"" 
- Packaging match: 75CL -> Btl 0,75cl (use existing).";



private const string _systemMessage = @$"

You are an AI assistant that matches OCR-extracted invoice products with products from the restaurant management system (RMS).  
Your task is to ensure accurate mapping of `InvoiceProducts` to `RMSProducts`, considering names, units, and packaging, and return a well-structured JSON strictly following the provided response schema.

---

### 💡 Matching Logic

1. **Copy Each InvoiceProduct**  
   For each product from the `InvoiceProducts` list:
   - Copy all its fields into the `InvoiceProduct` field of the response JSON.

2. **Match to Existing RMS Product**  
   - Try to find the best match from the `RMSProducts` list.
   - Match by product name, brand, key attributes (e.g. ""red wine"" ≠ ""white wine"").
   - Match by unit: `kg`, `l`, `pcs` must be semantically compatible.

   ➤ **If a match is found**:
   - Copy all fields (`Id`, `Name`, `Description`, `Num`, `Unit`, `Containers`) into the `RMSProduct`.

   ➤ **If no match is found**:
   - Create a new RMSProduct.
   - For a new RMSProduct, fill only `Name`, `Description`, `Unit` (selected from `MeasureUnits` dictionary)..
   - Leave `Id` and `Num` as `null`.
   - Set `NewRMSProduct = true`.

---

### 📦 Container (RMSContainer) Matching Logic

1. **If InvoiceProduct has only simple unit (e.g., `KG`, `L`, `pcs`), no explicit container info and container field is empty**:
   - Leave `RMSContainer` empty (`null` or fields not filled).
   - Set `NewRMSContainer = false`.

2. **If InvoiceProduct has a container or packaging (e.g., ""Box6KG"", ""Btl 0.33L"", ""24x0.33L"")**:
   - Try to match the container to one from `RMSProduct.Containers`.
   - Match by:
     - Name (semantic similarity, abbreviations),
     - Count (volume or quantity inside container),
     - Unit consistency.

   ➤ **If matched container exists**:
   - Copy it to `RMSContainer`.
   - Set `NewRMSContainer = false`.

   ➤ **If not found, and container looks standard (box, bottle, pack, etc.)**:
   - Create a new RMSContainer.
   - Fill only `Name` and `Count` (e.g., `""Box6KG""`, `6.0`).
   - Leave `Id` and `Num` as `null`.
   - Set `NewRMSContainer = true`.

3. **Ensure consistency**:
   - Container Count and Unit must be logically consistent with the RMSProduct's main unit and InvoiceProduct's Unit.

---

### 🏷 Storage Assignment

- For each product, assign the most appropriate **Storage** based on product type and meaning.

- The available storages are provided in the input as StorageList.  
  - Always choose one of the storages from that list.
  - Match based on semantic meaning and intended use of the product.

- Use the following as general **guidelines**:
  - If the product is a food item, ingredient, grocery, or kitchen-use — assign to something like `""Kitchen""`.
  - If the product is a drink, alcohol, mixer, or bar-related — assign to something like `""Bar""`.
  - If the product is intended for resale, takeaway, or is branded merchandise — assign to something like `""Retail""`.
  - If the product is a cleaning item, maintenance, or non-consumable — assign to something like `""Household""`.

- If the storage name does not exactly match `""Kitchen""`, `""Bar""`, `""Retail""`, or `""Household""` analyze its meaning and pick the closest appropriate one.

- Fill the `Storage` field with the exact name of the selected storage from the list.


### ⚙️ JSON Output Rules

- Strictly follow this response structure:

```json
{{
  ""MatchedInvoiceProducts"": [
    {{
      ""InvoiceProduct"": {{
        ""Id"": 1,
        ""ProductCode"": ""123456"",
        ""ProductName"": ""CERVEJA SUPER BOCK 24X33CL"",
        ""Unit"": ""btl"",
        ""Container"": ""Box 24x33cl"",
        ""UnitsCount"": 24,
        ""QuantityOfContainers"": 1
      }},
      ""RMSProduct"": {{
        ""Id"": 321,
        ""Name"": ""SUPER BOCK CERVEJA"",
        ""Description"": ""Cerveja portuguesa 33cl"",
        ""Num"": ""PRD-00992"",
        ""Unit"": ""btl"",
        ""Containers"": [
          {{
            ""Id"": 102,
            ""Num"": ""CONT-0054"",
            ""Name"": ""Box 24x33cl"",
            ""Count"": 24
          }}
        ]
      }},
      ""RMSContainer"": {{
        ""Id"": 102,
        ""Num"": ""CONT-0054"",
        ""Name"": ""Box 24x33cl"",
        ""Count"": 24
      }},
      ""NewRMSProduct"": false,
      ""NewRMSContainer"": false,
      ""Storage"": ""Bar"",
      ""Comments"": ""Matched by name and packaging.""
    }}
  ]
}}


";




/*
        private const string _instruction2 = $@"
Be careful and use the following information:

01. Take each product from first to last from the InvoiceProducts list and find a suitable matching product in the RMSProducts List: 
    - Note that there may be abbreviated or branded product names in the Invoice Products list. And in the RMSProducts list there could be common names of products used to prepare dishes served in a restaurant. 
    - Product names in the Invoice Product List and in the RMSProducts list can be in Portuguese or English. If needed, interpret the names and make sure to consider possible translations (e.g., ""Tomate"" in Portuguese vs. ""Tomato"" in English).
    - Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
            - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name **but differ by color/type**, so they are **different products**. **Do not match** them to the same RMS product if **one is red and the other is white**.
            - This should be taken into account for any different brands of wine or types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
            - So, from the attributes in the name of the Invoice Product and RMSProduct, taking into account the possible different languages, English and Portuguese, identify the main properties of both products - red or white, salty or sweet, frozen or fresh, and so on. And based on these properties, make your decision. Do not match products with different properties, even if they belong to the same brand.
    - Use your knowledge of languages, typical foods and products used in restaurants to make the best match.
    - Note that the correct strategy is to minimise incorrect mappings. If there is the slightest doubt that the selected RMSProduct is suitable, continue searching. And if you don't find any that clearly fit, then feel free to create a new RMSProduct. 
    - Also try to find the most appropriate Container or Unit of measure for the Invoice Product being processed in the RMSContainers list of the selected RMSProduct. And if you can't find the right one, create a new RMSContainer.

02. Depending on the search results, select only one way: go to **02.01** if a matching **RMSProduct and RMSContainer are found**, go to **02.02** if **RMSProduct is not found**, go to **02.03** if a matching **RMSProduct is found** but **RMSContainer is not found**. Select only one option that matches the search results and perform only that option, do not perform the others:

    **02.01**. Only execute the current item if you **have found** the most appropriate RMSProduct and RMSContainer:
        - Before finalizing, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features. If you have any doubts, skip back to step 01.
        - Save the matched InvoiceProduct, RMSProduct and RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""false"" into the NewRMSProduct element.
        - Put ""false"" into the NewRMSContainer element.
        - Put description of the selection and potential issues in the Comments element.

    **02.02**. Only execute the current item if you **have not found** a matching RMSProduct:
        - Create a new RMSProduct based on the Invoice Product. Fill only the Name, Description and Unit. Do not fill the Id and Num fields.
        - Choose a common Name for the new RMSProduct without brand or abbreviation, if it is a typical grocery product to prepare dishes. If it is a product with an important brand name or a familiar product name (for example, some wine, or Coca-Cola), save this brand name in the RMSProduct.
        - Also create a new suitable RMSContainer. Fill only the Name and Count (that means quantity, volume). Do not fill the Id and Num fields.
        - Save the InvoiceProduct being processed, a new RMSProduct and a new RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""true"" into the NewRMSProduct element.
        - Put ""true"" into the NewRMSContainer element.
        - Put description of the creation and potential issues in the Comments element.

    **02.03**. Only execute the current item if you **have found** a matching RMSProduct, but **no matching** RMSContainer:
        - Before finalizing, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features. If you have any doubts, skip back to step 01.
        - Create a new suitable RMSContainer. Fill only the Name and Count (that means quantity, volume). Do not fill the Id and Num fields.
        - Save the matched InvoiceProduct, RMSProduct and a new RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""false"" into the NewRMSProduct element.
        - Put ""true"" into the NewRMSContainer element.
        - Put description of the selection and creation and potential issues in the Comments element.

03. Check everything again. Make sure you make the best choice in the selected or created RMSProducts and RMSContainers, each choice is made according to the rules in the items **01** and **02**.

";*/

    }
}
