using Google.Protobuf.WellKnownTypes;
using ImageMagick;
using Microsoft.VisualBasic;
using OpenCvSharp.ML;
using OpenCvSharp;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using static Google.Api.FieldInfo.Types;
using static Google.Rpc.Context.AttributeContext.Types;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Tensorflow.ApiDef.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DAO;


namespace UpRestEye3.Services.Recognition
{
    public class GPTSemanticEnvironment
    {
        private readonly string _invoiceSchema;
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTSemanticEnvironment()
        {
            _invoiceSchema = JsonHelper.GetInvoiceSchema();
        }

        public string GetSystemPrompt(InvoiceDTO currentInvoice)
        {
            if (currentInvoice.Supplier.TaxNumber == "502030712")
                return _systemPromptForParsingLiteralSpecial01;
            else
                return _systemPromptForParsingLiteral;
        }

        

        
        public string GetInvoiceInfForUserPrompt(InvoiceDTO currentInvoice)
        {        
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"**Known invoice details** for validation:
    - **InvoiceNumber**: {currentInvoice.InvoiceNumber},
    - **InvoiceDate**: {currentInvoice.InvoiceDate},
    - **SupplierTaxID**: {currentInvoice.Supplier.TaxNumber},
    - **ConsumerTaxID**: {currentInvoice.Consumer.TaxNumber},
    - **TotalAmount**: {currentInvoice.TotalAmount},
    - **TotalIVA**: {currentInvoice.TotalIVA},
    - **Tax Categories**: ```json\n{JsonSerializer.Serialize(currentInvoice.TaxCategories, JsonHelper.GetSerializerOptions())}\n```.
    ";

            return _invoiceInformationPrompt;
        }


        public string GetInvoiceSchema() => _invoiceSchema;
       
        public string GetURL() => _url;
        
        public string GetApiKey() => _apiKey;
        
        


        public string GetReceiptParsingRequestBody(TablesDataDocument tablesDataDocument, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Проекция для выбора только нужных полей
            var selectedMeasureUnits = measUnits.Select(mu => new
            {
                mu.Id,
                mu.Name,
                mu.Description
            }).ToList();

            // Формируем запрос
            var requestBody = new
            {
                //model = "gpt-4o-mini",
                model = "gpt-4o", 

                temperature = 0.0,
                top_p = 1.0,

                n = 1,
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPrompt(currentInvoice) },

                        new { role = "user", content = "Extract structured data from the following Invoice, use additional information and predefined rules. Return the results in the predefined JSON in the response_format section."},
                        new { role = "user", content = JsonSerializer.Serialize(new { InvoiceTablesData = tablesDataDocument }, options), },
                        new { role = "user", content = GetInvoiceInfForUserPrompt(currentInvoice)},
                        new { role = "user", content = JsonSerializer.Serialize(new { MeasureUnits = selectedMeasureUnits }, options), },

                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(GetInvoiceSchema()).RootElement
                    }
                }
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }


        private const string _systemPromptForParsingLiteralOld = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.
Your task is to extract the list of Grocery Products from the provided OCR Invoice text, and return a well-structured JSON according to the given schema.

### **Processing Guidelines:**
1. **General Rules**
    - Ensure strict adherence to the provided JSON structure.
    - **Do not return JSON schema descriptions**, only the extracted data.

2. **Invoice Information**
    - The data recognized by OCR consists of: 
        -- Product table headers in the ProductHeaders block, 
        -- Table rows with products in the ProductRows block, 
        -- Headers of the table of tax categories in the TaxCategoriesHeaders block
        -- List of tax categories in the TaxCategoriesRows block.
    - All data are accompanied by coordinates of their location on the cheque. The format of coordinates is: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
    - The invoice can be on A4 size paper and then it contains detailed data. Or it can be a narrow cashier's cheque from a cash register printer, in which case it contains abbreviated data.
    - Use the provided coordinates to determine positions and relationships between the fields.
       
3. **Product List Headers Extraction**
    - Identify the main product table headers and their coordinates. 
    - Headers may include terms like **Code** (if available), **Name**, **Unit**, **Quantity**, **Tax Category** or **IVA**, and **Total Price**, etc. These headers may also appear in Portuguese or as abbreviations.
    - A single header may consist of multiple words. Use semantic meaning and neighboring words to determine if a header is composite. If so, combine them into one and **recalculate the header’s coordinates** accordingly.
    - Next, **increase the width of the header**. Adjust the coordinates of each header as follows: **shift the left X coordinates** 3 pixels to the left (Left X - 3), **shift the right X coordinates** to the beginning of the **next right header** (Current Right X = Next Right Left X). 

4. **Product List Extraction**
    - Place each word in the product line under a matching header by the X coordinate. If necessary, combine the words into one word and recalculate their coordinates. 
    - Based on the mapping of product row data to headings, and based on the meaning of the heading, extract the following properties for each product:
        -- Product code and name;
        -- Measurement unit, container type, quantity per container, and number of containers;
        -- Tax category and the total cost value of the entire product (unit price multiplied by quantity).
    - The name of the unit of measure must match one of the entries from the provided dictionary **MeasureUnits** . 

5. **Determination of tax categories**
    - Each product’s **Tax Category** must match one of the predefined categories and percentage rates:
    - Valid tax values: `'23'`, `'13'`, `'6'` or the corresponding names: `'Normal'`, `'Intermedia'`, `'Redusido'`, including their common abbreviations (e.g., `'Nor'`, `'Int'`, `'Red'`, etc.).
    - In some cases, tax categories in the product list are represented by letters or numbers, which are explained in a summary section TaxCategoriesRows. For the product list, use the full tax category names instead of the designations.

6. **Total Verification**
    - Ensure the sum of all extracted product values matches the given **TotalAmount**, either:
        -- directly (if **ProductTotalValue** includes tax), or
        -- as **TotalAmount** - **TotalIVA** (if **ProductTotalValue** excludes tax).

4. **Output** 
    - Check every extracted product and ensure that the extracted data **matches** the expected values.
    - Output the extracted products in JSON format** strictly following the response schema.
    - If there is a discrepancy, return a warning in the **Comments** field.
";


private const string _systemPromptForParsingLiteral = $@"

You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.  
Your task is to extract only the list of **Grocery Products** from the provided structure `InvoiceTablesData` with OCR Invoice text, and return a well-structured JSON according to the given schema.

---

### 1. 🧾 General Processing Rules

- **Output**:  
  Return only the final structured JSON result.  
  **Do NOT include explanations, schema descriptions, or comments outside JSON.**
  Ensure strict adherence to the output structure and data types.

- **Input Format** — `InvoiceTablesData`:
  - The input includes:
    - `ProductHeaders`: list of product column headers with coordinates;
    - `ProductRows`: list of recognized words and their positions;
    - `TaxCategoriesHeaders` and `TaxCategoriesRows`: headers and entries of the tax legend table.
  - All data is provided as OCR output with coordinates:  
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
  - The document may be a full A4 invoice or a narrow cashier-style receipt.

---

### 2. 📌 Product Table Processing

**Header Detection**
- Identify the main product table headers and their coordinates.
- Headers may include terms like **Code**, **Name**, **Unit**, **Quantity**, **Tax Category**, **Price per Unit**, **Total Price**, etc.
- Headers may appear in Portuguese or English or use abbreviations (e.g., `Cod`, `Qtd`, `IVA`, `Desc`, etc.).
- Handle headers split across **multiple lines** (e.g., 'Desconto' / 'promocional'):
  - Group vertically stacked words in the same X-zone;
  - Merge into a single logical header and adjust bounding box accordingly.
- Adjust column zones:
  - Left X → `Left X - 3`
  - Right X → `Next header's Left X`
- Use merged header text to semantically map to internal schema fields.

---

### 3. 🔍 Product Row Parsing

- Process **every row** in `ProductRows`. **Do not skip damaged or incomplete rows**.

- ⚠️ Ensure that **no product rows are lost** during parsing:
  - If a row contains **price and quantity**, it must be treated as an **independent product**, even if its name is complex or resembles packaging/description.
  - If a row lacks price or quantity, but is close to a previous product row, treat it as a **continuation** — e.g., additional description, packaging format, lot number, or brand.

- One product may span **multiple consecutive rows**:
  - First row contains main values;
  - Following rows may include details such as variant, brand, or packaging.

- Be cautious with **technical or packaging-related products** (e.g., cups, lids, containers, bags):
  - These often have long descriptive names but **must not be skipped** if price and quantity are present.
  - If in doubt — include the row as a separate product rather than risk losing it.

- Match words to headers by:
  - Primary method: X-coordinate position;
  - Secondary method: semantic meaning of the text.

- Merge multi-word values left-to-right, clean whitespace and punctuation.
- If expected values are missing, attempt to infer them using nearby context.
- ⚠️ Always copy the **full product name** from OCR — including brand, packaging, size/volume units, and any descriptive details — **without abbreviation or transformation**.

---

### 4. 📦 Packaging & Unit Extraction Logic

**Packaging Fields:**

- `**Unit**`: base unit of measure (e.g., `kg`, `l`, `pcs`, `btl`, `unit`)
- `**Quantity**`: number of units or containers sold
- `**Container**`: packaging name/description (e.g., `Box6kg`, `24x0.33L`, `Pack250g`)
- `**Count**`: quantity/volume/units per container (in the specified `Unit`, or in KG for weighed products and in L for liquids). Leave blank if no container

---

### **Packaging Identification Rules**

1. **Direct sale without packaging**  
   If only base quantity and unit is specified (e.g., `1.820 KG`, `3 L`, `15 pcs`) and **no packaging is mentioned**:
   - `Unit = kg`, `l`, `pcs`, etc. (base unit)
   - `Quantity` = base quantity
   - `Container = """"`
   - `Count = null`

2. **pcs/unit + packaging info in product name**  
   If unit is `pcs` or `unit`, and product name contains packaging info (e.g., `50g`, `250g`, `0.33L`, `1l`, `330ml`):
   - `Unit` = `pcs`, `unit`, etc. (base unit)
   - `Quantity` = `1` or other quantity from OCR invoice text 
   - `Container` = formatted as `Pack 250g`, `Btl 0.33l`, `Cup 200ml`, etc.
   - `Count` = weight or volume in normalized units (in kg for weighed products and in l for liquids, e.g., `50g` → `0.05`, `330ml` → `0.33`)

3. **Explicit packaging and multi-packs**  
   If packaging is present (e.g., `Box6kg`, `24x0.33l`, `Emb12x200g`, `Pack 4x1l`):
   - `Unit` = smallest measurement unit (e.g., `kg`, `l`, `pcs`, `btl0,33l`, etc.)
   - `Quantity` = number of containers (e.g., `3`, `2`, etc.)
   - `Container` = extracted name (`Box6kg`, `24x0.33l`)
   - `Count` = content per container (e.g., `Box6kg` = 6, `24 x 0.33l` = 24 )

4. **Non-grocery packaging (disposables, utensils, etc.)**  
   If the product is a **non-food item** (e.g., packaging, cups, cleaning supplies), and the text contains **container/box/carton** info:
   - Set `Container` to the box/pack name (e.g., `Box`, `Carton`, `Pack`)
   - If possible, extract `Count` as the number of items inside (e.g., `100 cups`, `50 bags` → `Count = 100`)
   - If item count is not stated, leave `Count = null`

---

**Additional Handling Notes:**

- Normalize and correct OCR errors (`1` vs `l`, `g` vs `G`, etc.)
- Link related rows where packaging is split across multiple lines
- Ensure `Quantity`, `Unit`, `Container`, and `Count` form a consistent logical structure for each product

---

### 5. 🧾 Tax Category Mapping

- Each product row may contain a tax code (e.g., `2`, `4`, `5`).
- Convert to `TaxCategory` using:

  ```
  2 = 23.00% → ""Normal""
  4 = 6.00%  → ""Reduced""
  5 = 13.00% → ""Intermedia""
  ```

- If the tax code is not listed, resolve it from `TaxCategoriesRows`.

---

### 6. 🧠 OCR Error Correction & Row Recovery

- Correct common OCR mistakes:
  - `Totai` → `Total`, `1` ↔ `I`, `0` ↔ `O`, `Descriçâo` → `Descrição`
  - Numerical corrections: `3,4O` → `3.40`, etc.
- Do not discard partially recognized rows. Attempt to reconstruct from context and position.
- Include incomplete rows if any values can be interpreted.

---

### 7. ✅ Final Validation

- Sum all `ProductTotalValue` values.
- Cross-check against:
  - `TotalAmount` (if tax included)
  - `TotalAmount - TotalIVA` (if excluding tax)
- In the `Comments` field, note:
  - Any automatic corrections
  - Reconstructed or inferred data
  - Discrepancies in totals

---

### 8. 📤 JSON Output Format

```json
{{
  ""Products"": [
    {{
      ""ProductCode"": ""2880805018206"",
      ""ProductName"": ""MC 1OMAIE RAMA 1 67/82 V2"",
      ""Unit"": ""kg"",
      ""Quantity"": 1.820,
      ""Container"": """",
      ""Count"": null,
      ""PricePerUnitKG"": 2.180,
      ""PricePerContainer"": 3.97,
      ""ProductTotalValue"": 3.97,
      ""TaxCategory"": ""Reduced""
    }},
    {{
      ""ProductCode"": ""004321"",
      ""ProductName"": ""CERV. SUPER BOCK 24X33CL TP"",
      ""Unit"": ""btl"",
      ""Quantity"": 2,
      ""Container"": ""Box24x0.33L"",
      ""Count"": 24,
      ""PricePerUnitKG"": 0.560,
      ""PricePerContainer"": 13.44,
      ""ProductTotalValue"": 26.88,
      ""TaxCategory"": ""Normal""
    }}
  ]
}}
```
";



        private const string _systemPromptForParsingLiteralSpecial01 = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.  
Your task is to extract only the list of Grocery Products from the provided OCR Invoice text, and return a well-structured JSON according to the given schema.

---

### General Processing Rules

1. **Output**
   - Return only the final structured JSON result.
   - **Do NOT include explanations, schema descriptions, or comments outside JSON.**
   - Ensure strict adherence to the output structure and data types.

2. **Input Format**
   - The input includes:
     - `ProductHeaders`: list of product column headers with coordinates;
     - `ProductRows`: list of recognized words and their positions;
     - `TaxCategoriesHeaders` and `TaxCategoriesRows`: headers and entries of the tax legend table.
   - All data is provided as OCR output with coordinates: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
   - The document may be a full A4 invoice or a narrow cashier-style receipt.

---

### Product List Extraction

3. **Header Detection**
   - Identify the headers in `ProductHeaders` according to the list below, in the same order. 
   - One header may consist of multiple words. Combine adjacent words based on meaning and X-position.
   - Adjust header coordinate areas to improve coverage:
     - **Expand header zone**:
       - Left X → `Left X - 3`
       - Right X → `Next Header's Left X`

    | # | **Invoice Product Header** |   **Column Type**        | **Mapped JSON Field** | **Description** |
    |---|----------------------------|--------------------------|-----------------------|-----------------|
    | 1 | Código Artigo              | Number and letter string | ProductCode           | Code of the product from the recognized invoice |
    | 2 | Descrição Artigo           | Text                     | ProductName           | Name of the product from the recognized invoice |
    | 3 | PACK                       | 2-3 Symbols              | Container             | Packaging type, container (package, bottle, box, bag, sack, piece, kg, etc.) (also match the appropriate Measure Units by the value of this field in accordance with the provided dictionary **MeasureUnits**)|
    | 4 | PR Unit/KG                 | Decimal number           | PricePerUnitKG        | Price of one measure unit |
    | 5 | Unit/KG                    | Decimal number           | Count                 | Number of units inside the container |
    | 6 | Preço U.V.                 | Decimal number           | PricePerContainer     | Price of one container |
    | 7 | Quant                      | Integer number           | Quantity              | Number of container or units purchased |
    | 8 | Valor Total                | Decimal number           | ProductTotalValue     | Total cost of this product (with or without tax, based on invoice type) |
    | 9 | IvaDD                      | Integer number           | TaxCategory           | Designation of the Tax category of the product (match using tax summary list) |



4. **Product Row Parsing**
   - **Process every row** in `ProductRows`. **Do not skip any rows**, even if damaged or incomplete.
   - Match words in each row to the correct column by:
     - Primary: comparing X-coordinates with headers;
     - Secondary: interpreting semantics of content.
   - Based on the mapping of product row data to headings, extract **each field** and put to the corresponding mapped JSON Field in the same sequence as below.
   - If multiple words belong to the same column, **merge them** and adjust the bounding box.
   - If a row is missing values, attempt to **infer or repair** them based on context and format.


5. **Fix OCR Errors**
   - Actively identify and correct **OCR spelling and number recognition mistakes**:
     - Examples: `Totai` → `Total`, `1` ↔ `I`, `0` ↔ `O`, `Descriçâo` → `Descrição`
     - Misread decimals or formatting: `3,4O` → `3.40`
   - Correct invalid symbols in numeric fields (e.g., letters in price or quantity).
   - Normalize all values for consistency.

6. **Rescue Damaged Rows**
   - If a product row is fragmented or partially unreadable:
     - **Do not discard it.**
     - Try to recover as much as possible based on structure and column layout.
     - Partially filled rows are better than lost rows.

---

### Tax Category Mapping

7. **Decipher tax categories**
    - In the IvaDD column of each product row, you will find a tax category code (e.g., 2, 4, 5). These codes correspond to VAT percentages and category names, and must be mapped as follows:
        2 = 23.00% → `Normal`
        4 = 6.00%  → `Reduced`
        5 = 13.00% → `Intermedia`
         
    - **Use this fixed mapping** to convert the **IvaDD** code into the **TaxCategory** value in the output JSON.
    - If the tax code in the product row is not one of the above, look up the corresponding code in the TaxCategoriesRows block of the invoice (tax summary table).
    

---

### Final Validation

8. **Verify Totals**
   - Sum all `ProductTotalValue` values.
   - Confirm that the total matches:
     - `TotalAmount` (if values include tax), or
     - `TotalAmount - TotalIVA` (if values exclude tax).
   - Add a `Comments` field to describe:
     - Any automatic corrections;
     - Reconstructed or incomplete rows;
     - Discrepancies in totals.

---

### JSON Output Example
   json
   {{
     ""Products"": [
       {{
         ""ProductCode"": ""2880805018206"",
         ""ProductName"": ""MC 1OMAIE RAMA 1 67/82 V2"",
         ""Unit"": ""kg"",
         ""Container"": ""KG"",
         ""PricePerUnitKG"": 2.180,
         ""UnitsCount"": 1.820,
         ""PricePerContainer"": 3.97,
         ""QuantityOfContainers"": 1,
         ""ProductTotalValue"": 3.97,
         ""TaxCategory"": ""Reduced""
       }},
       {{
         ""ProductCode"": ""004321"",
         ""ProductName"": ""CERV. SUPER BOCK 24X33CL TP"",
         ""Unit"": ""btl"",
         ""Container"": ""Box"",
         ""PricePerUnitKG"": 0.560,
         ""UnitsCount"": 24,
         ""PricePerContainer"": 13.44,
         ""QuantityOfContainers"": 1,
         ""ProductTotalValue"": 13.44,
         ""TaxCategory"": ""Normal""
       }}
     ]
   }}

";

    }
}
