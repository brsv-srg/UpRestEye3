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

        
        public string GetInvoiceForUserPrompt(RecognizedDocument invoiceText, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Проекция для выбора только нужных полей
            var selectedMeasureUnits = measUnits.Select(mu => new
            {
                mu.Id,
                mu.Name,
                mu.Description
            }).ToList();

            var _invoiceInformationPrompt = $@"\n
Extract structured data from this Invoice: {JsonSerializer.Serialize(invoiceText,options)}. 

Use the following **known invoice details** for validation:
    - **InvoiceNumber**: {currentInvoice.InvoiceNumber},
    - **InvoiceDate**: {currentInvoice.InvoiceDate},
    - **SupplierTaxID**: {currentInvoice.Supplier.TaxNumber},
    - **ConsumerTaxID**: {currentInvoice.Consumer.TaxNumber},
    - **TotalAmount**: {currentInvoice.TotalAmount},
    - **TotalIVA**: {currentInvoice.TotalIVA},
    - **Tax Categories**: {JsonSerializer.Serialize(currentInvoice.TaxCategories, JsonHelper.GetSerializerOptions())}.
    And fot Unit use this dictionary MeasureUnits: {JsonSerializer.Serialize(selectedMeasureUnits, options)}\n
    ";

            return _invoiceInformationPrompt;
        }

        public string GetInvoiceForUserPrompt2(string invoiceText, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Проекция для выбора только нужных полей
            var selectedMeasureUnits = measUnits.Select(mu => new
            {
                mu.Id,
                mu.Name,
                mu.Description
            }).ToList();

            var _invoiceInformationPrompt = $@"\n
Extract structured data from this Invoice: {invoiceText}. 

Use the following **known invoice details** for validation:
    - **InvoiceNumber**: {currentInvoice.InvoiceNumber},
    - **InvoiceDate**: {currentInvoice.InvoiceDate},
    - **SupplierTaxID**: {currentInvoice.Supplier.TaxNumber},
    - **ConsumerTaxID**: {currentInvoice.Consumer.TaxNumber},
    - **TotalAmount**: {currentInvoice.TotalAmount},
    - **TotalIVA**: {currentInvoice.TotalIVA},
    - **Tax Categories**: {JsonSerializer.Serialize(currentInvoice.TaxCategories, JsonHelper.GetSerializerOptions())}.
    And fot Unit use this dictionary MeasureUnits: {JsonSerializer.Serialize(selectedMeasureUnits, options)}\n
    ";

            return _invoiceInformationPrompt;
        }


        public string GetInvoiceSchema() => _invoiceSchema;
       
        public string GetURL() => _url;
        
        public string GetApiKey() => _apiKey;
        
        public string GetReceiptParsingRequestBody(RecognizedDocument invoiceText, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o",
                //model = "gpt-4o-mini",

                temperature = 0.0,
                top_p = 1.0,

                n = 1,
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPrompt(currentInvoice) }, 
                        new { role = "user", content = GetInvoiceForUserPrompt(invoiceText, currentInvoice, measUnits)}  
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


        public string GetReceiptParsingRequestBody2(string invoiceText, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
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
                        new { role = "user", content = GetInvoiceForUserPrompt2(invoiceText, currentInvoice, measUnits)}
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
   - Identify the main product table headers and their coordinates. 
   - Headers may include terms like **Code** (if available), **Name**, **Unit**, **Quantity**, **Tax Category** or **IVA**, and **Total Price**, etc. These headers may also appear in Portuguese or as abbreviations.
   - One header may consist of multiple words. Combine adjacent words based on meaning and X-position.
   - Adjust header coordinate areas to improve coverage:
     - **Expand header zone**:
       - Left X → `Left X - 3`
       - Right X → `Next Header's Left X`



3. **Header Detection**

   - Identify the main product table headers and their coordinates.
   - Headers may include terms like **Code**, **Name**, **Unit**, **Quantity**, **Tax Category** or **IVA**, **Total Price**, etc. These headers may also appear in Portuguese or as abbreviations.
   - One logical header may consist of multiple words split **horizontally (adjacent)** or **vertically (stacked across multiple lines)**.

   - To correctly identify multi-line headers:
       - Group vertically stacked header elements that share the same or similar X-coordinates (i.e. appear in the same column).
       - Combine them into a **single logical header**, preserving word order (top to bottom).
       - Merge their text and recalculate the **bounding box** so that it spans the full vertical area of the grouped parts.

   - Adjust header coordinate areas to improve coverage:
       - **Expand header zone**:
         - Left X → `Left X - 3`
         - Right X → `Next Header's Left X`

   - After combination, match full header text (including merged multi-line content) to known header types, and map to internal field names using semantic similarity and expected keywords.



4. **Product Row Parsing**
   - **Process every row** in `ProductRows`. **Do not skip any rows**, even if damaged, partially filled, or incomplete.
   - Recognize that a single product may span **multiple consecutive rows** (two or more):
       - The **first row** contains the main structured data (code, name, quantity, price, etc.).
       - The following row(s) may contain additional information such as:Product description extensions, Brand names, Technical details, Packaging types, Variant labels, etc.

   - When a row contains **only text fragments** (typically under the `ProductName` column) and lacks numerical fields such as quantity, prices, tax, or codes — assume it belongs to the **previous product**.
   - Merge its content into the appropriate field of the previous product, most often into `ProductName` or `Container`.
   - Ensure merged fields are clean, properly spaced, and semantically valid.

   - Match words in each row to the correct column by:
       - **Primary method**: comparing X-coordinates with header column boundaries.
       - **Secondary method**: interpreting semantic meaning of the text (e.g., distinguishing numbers from descriptions).

   - For every complete or merged product, extract **each field** and place it into the corresponding mapped JSON field, following the defined field order.

   - If multiple words fall under the same column, merge them in natural reading order (left to right) and adjust bounding boxes if needed.

   - If a row is missing expected values, attempt to **infer or reconstruct** them using:
       - Column mappings,
       - Context from surrounding rows,
       - Known invoice structures or patterns.

   - **Never skip rows**. If a row cannot be parsed as a new product, always evaluate whether it continues the previous one.

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
         ""UnitsCountInContainer"": 1.820,
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
         ""UnitsCountInContainer"": 24,
         ""PricePerContainer"": 13.44,
         ""QuantityOfContainers"": 1,
         ""ProductTotalValue"": 13.44,
         ""TaxCategory"": ""Normal""
       }}
     ]
   }}

";



        private const string _systemPromptForParsingLiteralSpecial02 = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.
Your task is to extract only the list of Grocery Products from the provided OCR Invoice text, and return a well-structured JSON according to the given schema.

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
    - All data are accompanied by coordinates of their location on the cheque.
    - The invoice can be on A4 size paper and then it contains detailed data. Or it can be a narrow cashier's cheque from a cash register printer, in which case it contains abbreviated data.
    - Use the provided coordinates to determine positions and relationships between the fields.

3. **Product List Extraction**  
    - **Headers**
        -- Identify the headers and their coordinates in ProductHeaders according to the list below.
        -- One heading can correspond to several words. Identify all the necessary words to fully match the header, combine them and **calculate the header coordinates** accordingly.
        -- Next, **increase the width of the header**. Adjust the coordinates of each header as follows: **shift the left X coordinates** 3 pixels to the left (Left X - 3), **shift the right X coordinates** to the beginning of the **next right header** (Current Right X = Next Right Left X). 
    - **Rows**
        -- Process every rows in the ProductRows block, don't forgive a single line. One line is one product.
        -- Place each word in the product row under a matching header by the X coordinate. If more than one word match with the header by X coordinate, **combine them all** and **calculate the header coordinates** accordingly.
        -- Based on the mapping of product row data to headings, extract **each field** from the ProductRows and put to the corresponding mapped JSON Field in the same sequence as below.
        -- **Ensure that the extracted data matches the expected values and column type**. Correct OCR errors in text and numbers.

    | # | **Invoice Product Header** |   **Column Type**        | **Mapped JSON Field** | **Description** |
    |---|----------------------------|--------------------------|-----------------------|-----------------|
    | 1 | Código Artigo              | Number and letter string | ProductCode           | Code of the product from the recognized invoice (if specified) |
    | 2 | Descrição Artigo           | Text                     | ProductName           | Name of the product from the recognized invoice |
    | 3 | PACK                       | 2-3 Symbols              | Container             | Packaging type, container (package, bottle, box, bag, sack, piece, kg, etc.) (also match the appropriate Measure Units by the value of this field in accordance with the provided dictionary **MeasureUnits**)|
    | 4 | PR Unit/KG                 | Decimal number           | PricePerUnitKG        | Price of one measure unit |
    | 5 | Unit/KG                    | Decimal number           | UnitsCountInContainer | Number of units inside the container |
    | 6 | Preço U.V.                 | Decimal number           | PricePerContainer     | Price of one container |
    | 7 | Quant                      | Integer number           | QuantityOfContainers  | Number of container units purchased |
    | 8 | Valor Total                | Decimal number           | ProductTotalValue     | Total cost of this product (with or without tax, based on invoice type) |
    | 9 | IvaDD                      | Integer number           | TaxCategory           | Designation of the Tax category of the product (match using tax summary list) |


4. **Tax Category Mapping**  
   - In the table, tax categories (`IvaDD`) are designated by numbers.  
   - Use these numbers to find the corresponding **Tax Category Name** from the **Tax Summary List**.  
   - Replace the `IvaDD` number with the corresponding **category name** in the output JSON.  

5. **Total Verification**  
   - Verify that the sum of all `ProductTotalValue` in the extracted product list:  
     - Matches **TotalAmount** (if `ProductTotalValue` includes tax)  
     - Matches **TotalAmount - TotalIVA** (if `ProductTotalValue` does not include tax).  
   - **Describe all your actions** in the `Comments` field. If there are any inconsistencies, also add in this field.
   - Output the extracted products in JSON format** strictly following the response schema.

### **Example of Expected JSON Output**  
   json
   {{
     ""Products"": [
       {{
         ""ProductCode"": ""2880805018206"",
         ""ProductName"": ""MC 1OMAIE RAMA 1 67/82 V2"",
         ""Unit"": ""kg"",
         ""Container"": ""KG"",
         ""PricePerUnitKG"": 2.180,
         ""UnitsCountInContainer"": 1.820,
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
         ""UnitsCountInContainer"": 24,
         ""PricePerContainer"": 13.44,
         ""QuantityOfContainers"": 1,
         ""ProductTotalValue"": 13.44,
         ""TaxCategory"": ""Normal""
       }}
     ]
   }}

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
    | 5 | Unit/KG                    | Decimal number           | UnitsCountInContainer | Number of units inside the container |
    | 6 | Preço U.V.                 | Decimal number           | PricePerContainer     | Price of one container |
    | 7 | Quant                      | Integer number           | QuantityOfContainers  | Number of container units purchased |
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
         ""UnitsCountInContainer"": 1.820,
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
         ""UnitsCountInContainer"": 24,
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
