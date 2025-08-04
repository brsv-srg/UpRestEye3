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
using Org.BouncyCastle.Asn1.Ocsp;
using static Tensorflow.TensorSliceProto.Types;
using System.Net.Http.Json;


namespace UpRestEye3.Services.Recognition
{
    public class GPTTablesLayoutEnvironment
    {
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTTablesLayoutEnvironment()
        {
        }

        private string GetSystemPromptRows(InvoiceDTO currentInvoice)
        {
            if (currentInvoice.Supplier.TaxNumber == "502030712")
                return _systemPromptForParsingLiteralSpecialRows;
            else
                return _systemPromptForParsingLiteralRows;
        }

        private string GetSystemPromptWords(InvoiceDTO currentInvoice)
        {
            if (currentInvoice.Supplier.TaxNumber == "502030712")
                return _systemPromptForParsingLiteralSpecialWords;
            else
                return _systemPromptForParsingLiteralWords;
        }
        private string GetUserPrompt(string invoiceText)
        {
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"\n
Extract the table of products/services and list of TAXes from this provided OCR invoice text: {invoiceText}.
    ";

            return _invoiceInformationPrompt;
        }

        private string GetInvoiceDetails(InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"
Use the following **known invoice details** for validation:
    - **InvoiceNumber**: {currentInvoice.InvoiceNumber},
    - **InvoiceDate**: {currentInvoice.InvoiceDate},
    - **SupplierTaxID**: {currentInvoice.Supplier.TaxNumber},
    - **ConsumerTaxID**: {currentInvoice.Consumer.TaxNumber},
    - **TotalAmount**: {currentInvoice.TotalAmount},
    - **TotalIVA**: {currentInvoice.TotalIVA},
    - **Tax Categories**: {JsonSerializer.Serialize(currentInvoice.TaxCategories, options)}
    ";

            return _invoiceInformationPrompt;
        }

        private string GetProductsSchema() => _resultSchemeLiteral;

        public string GetURL() => _url;

        public string GetApiKey() => _apiKey;
        
        public string GetRowsLayoutRequestBody(SimplePageOfRows invoiceText, InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4.1",
                //model = "gpt-4o",
                //model = "gpt-4o-mini",

                temperature = 0.0,
                top_p = 1.0,
                n = 1,
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPromptRows(currentInvoice) }, 
                        new { role = "user", content = GetUserPrompt(JsonSerializer.Serialize(invoiceText, options)) },
                        new { role = "user", content = GetInvoiceDetails(currentInvoice) }  
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(GetProductsSchema()).RootElement
                    }
                }
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }


        public string GetWordsLayoutRequestBody(SimplePageOfWords invoiceText, InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4.1",
                //model = "gpt-4o",
                //model = "gpt-4o-mini",

                temperature = 0.0,
                top_p = 1.0,
                n = 1,
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPromptWords(currentInvoice) },
                        new { role = "user", content = GetUserPrompt(JsonSerializer.Serialize(invoiceText, options)) },
                        new { role = "user", content = GetInvoiceDetails(currentInvoice) }
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(GetProductsSchema()).RootElement
                    }
                }
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }



        private const string _systemPromptForParsingLiteralRows = @"
You are an AI assistant specialized in extracting structured product and tax data from OCR-recognized invoices.  
Your task is to analyze the OCR output and extract **product tables** and **tax breakdowns**, returning the result as structured JSON according to the provided `response_format` schema.

---

### 📄 **Input Format:**

- An Invoice may consist of several pages. But each time processing is done on a separate page.
- A page contains a flat list of lines combining recognized `Words` presented on the same line in the invoice.
- Each `Word` in a line has:
  - `WordText`
  - `WordCoordinates` in the format:  
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`

- Using this list of lines and words and their coordinates on the page, **define the product table** and **tax section**, including headers and lines for each of them.  
---

### 🛠️ **Extraction Tasks: Do it for each page**

#### 1. **Detect Sections:**

- Locate and retrieve the product table and tax section on the page. Products or taxes may not be present on individual pages, but make sure to double check.
- It is important to extract all rows and words of products and taxes. If even a single product or tax is missing, then the whole task of Invoice recognizing is not complete.
- If in doubt, **add that line to the result** so you don't lose important data.
- **Don't mix up the rows**, it is important to keep the sequence that is given in the coordinates.
- Ignore unrelated parts such as company details, addresses, notes, footers, etc.
- Save all found on the page data in the output JSON structure.

---

#### 2. **Extract Product Table Headers:**

- Identify and extract the row or **group of visually consecutive rows** containing product table column headers (e.g., `Code`, `Description`, `Qty`, `Unit`, `Price`, `IVA`, `Discount`, `Total`, etc.).
- Headers may be in **Portuguese, English**, or use common abbreviations (`Cod`, `Qtd`, `UNI`, `IVA`, etc.).
- **Headers may span multiple lines**. If a column name is split across several rows (e.g., 'Desconto' / 'promocional'), include **all relevant rows**.
- Store all Headers words found on the page **""as one line as one row""** in the output `ProductHeaders` array, **exactly as recognized**:
  - Preserve original spelling, diacritics, casing, symbols.
  - Save full, unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
  - ⚠️ **Do not translate, normalize, or merge** terms.

---

#### 3. **Extract Product Table Rows:**

- Identify and extract **all product-related rows** including:
  - Main product lines with all information about Product in line: Code, Name, Pack, Price, Quantity, Total, TAX, etc,
  - Additional rows without prices that provide additional info:
    - Descriptions,
    - Supplemental rows (e.g., Lote, reference).

- Continue extracting **all products** until explicitly unrelated content begins (e.g., totals, taxes, company information, notes) or page will be finished).
- Place all detected product rows found on the page in the output `ProductRows` array, **strictly in their order** on the page. **Do not mix or rearrange** rows.
- For each row preserve original sequence of **all words, symbols, numbers, etc., and raw coordinates without changes** in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
- ⚠️ **Do not skip lines and line words that represent products**.
- ⚠️ Once product rows are detected, **check that quantities and prices are correctly aligned** and not mistakenly linked to neighboring products, especially if the Invoice is garbled.

##### ⚠️ Quantity + Unit Handling:
- If a **quantity value** (e.g., `3,12`) is **next to** or **preceded/followed** by a unit (e.g., `KG`, `UNI`, `LT`, `UN`, etc.), treat them as a **logically linked pair**.
- Ensure they are both included in the **same product row**, even if slightly misaligned by coordinates.
- Units must **not be confused with headers** or discarded as noise.
**Examples of valid units (case-insensitive):** `UNI`, `UN`, `KG`, `G`, `L`, `LT`, `PC`, `PACK`, `EMB`, `CX`, `DZ`.

- Be careful. If even **one product line or word from product line is lost** from page, the whole recognition **task will be thwarted**.

---

#### 4. **Extract Tax Table Headers:**

- Identify and extract the header row(s) for the tax section (e.g., `Incidência`, `Taxa`, `IVA`, `Valor`, `Total`).
- Store all Tax Table Header words found on the Invoice page **as one row** in the output `TaxCategoriesHeaders` array, preserving:
  - Exact text (with all symbols, accents, etc.),
  - Coordinates unmodified in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`..

---

#### 5. **Extract Tax Table Rows:**

- Identify and extract all rows that belongs to the tax breakdown table.
- Use the header layout to guide column matching.
- Store all detected rows found on the Page in the output `TaxCategoriesRows` array, preserving full word details and unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

#### 6. **Correct OCR Errors:**

- Actively detect and correct common OCR mistakes (e.g., `L` instead of `1`, missing accents, fragmented words, misaligned lines).
- Use visual proximity and contextual clues to reassemble broken rows or fix incorrect groupings.

---

#### 7. **Output JSON Format:**

- Return the result strictly as **JSON**, following the `response_format` schema.
- **Do not return** the schema or any descriptive text — only the structured data.
- Check carefully that **all data** have been processed and saved correctly. **Be very careful**. If **any product or tax line is missing**, the recognition **task has failed**.
";

        private const string _systemPromptForParsingLiteralWords = @"
You are an AI assistant specialized in extracting structured product and tax data from OCR-recognized invoices.  
Your task is to analyze the OCR output and extract **product tables** and **taxes**, returning the result as structured JSON according to the provided `response_format` schema.

---

### 📄 **Input Format:**

- An Invoice may consist of several pages. But each time processing is done on a separate page.
- Page contains a flat list of recognized `Words`, and each word has:
  - `WordText`
  - `WordCoordinates` in the format:  
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`

- ⚠️ There are no pre-grouped blocks or rows. Words may not be in sorted order
- The invoice may be skewed or distorted. You **must create a spatial model** to detect the type of distortion** (e.g., skew angle, perspective shift) and **compensate distortions** for reconstructing the structure of product and tax tables.
- Model must infer structure based on layout, spacing, and alignment.

- Using this spatial model, taking into account document distortions, create a list of rows combining words located on the same line of the source document, from the left edge to the right. 
- ⚠️ All data processing should be done only on the basis of the constructed lines.

---

### 🛠️ **Extraction Tasks: Do it for each page**

#### 1. **Detect Sections:**

- Locate and retrieve the product table and tax section on the page. Products or taxes may not be present on individual pages, but make sure to double check.
- It is important to extract all rows and words of products and taxes. If even a single product or tax is missing, then the whole task of Invoice recognizing is not complete.
- If in doubt, **add that line to the result** so you don't lose important data.
- **Don't mix up the rows**, it is important to keep the sequence that is given in the coordinates.
- Ignore unrelated parts such as company details, addresses, notes, footers, etc.
- Save all found on the page data in the output JSON structure.

---

#### 2. **Extract Product Table Headers:**

- Identify and extract the row or **group of visually consecutive rows** containing product table column headers (e.g., `Code`, `Description`, `Qty`, `Unit`, `Price`, `IVA`, `Discount`, `Total`, etc.).
- Headers may be in **Portuguese, English**, or use common abbreviations (`Cod`, `Qtd`, `UNI`, `IVA`, etc.).
- **Headers may span multiple lines**. If a column name is split across several rows (e.g., 'Desconto' / 'promocional'), include **all relevant rows**.
- Store all Headers words found on the page **""as one line as one row""** in the output `ProductHeaders` array, **exactly as recognized**:
  - Preserve original spelling, diacritics, casing, symbols.
  - Save full, unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
  - ⚠️ **Do not translate, normalize, or merge** terms.

---

#### 3. **Extract Product Table Rows:**

- Identify and extract **all product-related rows** including:
  - Main product lines with all information about Product in line: Code, Name, Pack, Price, Quantity, Total, TAX, etc,
  - Additional rows without prices that provide additional info:
    - Descriptions,
    - Supplemental rows (e.g., Lote, reference).

- Continue extracting **all products** until explicitly unrelated content begins (e.g., totals, taxes, company information, notes) or page will be finished).
- Place all detected product rows found on the page in the output `ProductRows` array, **strictly in their order** on the page. **Do not mix or rearrange** rows.
- For each row preserve original sequence of **all words, symbols, numbers, etc., and raw coordinates without changes** in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
- ⚠️ **Do not skip lines and line words that represent products**.
- ⚠️ Once product rows are detected, **check that quantities and prices are correctly aligned** and not mistakenly linked to neighboring products, especially if the Invoice is garbled.

##### ⚠️ Quantity + Unit Handling:
- If a **quantity value** (e.g., `3,12`) is **next to** or **preceded/followed** by a unit (e.g., `KG`, `UNI`, `LT`, `UN`, etc.), treat them as a **logically linked pair**.
- Ensure they are both included in the **same product row**, even if slightly misaligned by coordinates.
- Units must **not be confused with headers** or discarded as noise.
**Examples of valid units (case-insensitive):** `UNI`, `UN`, `KG`, `G`, `L`, `LT`, `PC`, `PACK`, `EMB`, `CX`, `DZ`.

- Be careful. If even **one product line or word from product line is lost** from page, the whole recognition **task will be thwarted**.

---

#### 4. **Extract Tax Table Headers:**

- Identify and extract the header row(s) for the tax section (e.g., `Incidência`, `Taxa`, `IVA`, `Valor`, `Total`).
- Store all Tax Table Header words found on the Invoice page **as one row** in the output `TaxCategoriesHeaders` array, preserving:
  - Exact text (with all symbols, accents, etc.),
  - Coordinates unmodified in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`..

---

#### 5. **Extract Tax Table Rows:**

- Identify and extract all rows that belongs to the tax breakdown table.
- Use the header layout to guide column matching.
- Store all detected rows found on the Page in the output `TaxCategoriesRows` array, preserving full word details and unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

#### 6. **Correct OCR Errors:**

- Actively detect and correct common OCR mistakes (e.g., `L` instead of `1`, missing accents, fragmented words, misaligned lines).
- Use visual proximity and contextual clues to reassemble broken rows or fix incorrect groupings.

---

#### 7. **Output JSON Format:**

- Return the result strictly as **JSON**, following the `response_format` schema.
- **Do not return** the schema or any descriptive text — only the structured data.
- Check carefully that **all data** have been processed and saved correctly. **Be very careful**. If **any product or tax line is missing**, the recognition **task has failed**.

";


        private const string _systemPromptForParsingLiteralSpecialRows = @"
You are an AI assistant specialized in extracting structured product and tax data from OCR-recognized invoices.  
Your task is to analyze the OCR output and extract **product tables** and **tax breakdowns**, returning the result as structured JSON according to the provided `response_format` schema.

---

### 📄 **Input Format:**

- An Invoice may consist of several pages. But each time processing is done on a separate page.
- A page contains a flat list of rows combining recognized `Words` presented on the same line in the invoice.
- Each `Word` in a line has:
  - `WordText`
  - `WordCoordinates` in the format:  
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`

- Using this list of rows, words, and their coordinates on the page, **define the product table** and **tax section**, including headings and rows for each.
---

### 🛠️ **Extraction Tasks: Do it for each page**

#### 1. **Detect Sections:**

- Locate and retrieve the product table and tax section on the page. Products or taxes may not be present on individual pages, but make sure to double check.
- It is important to extract all rows of products and taxes. If even a single product or tax is missing, then the whole task of Invoice recognizing is not complete.
- It is **better to add extra rows** when in doubt **than to lose the right** ones.
- **Don't mix up the rows**, it is important to keep the sequence that is given in the coordinates.
- Ignore unrelated parts such as company details, addresses, notes, footers, etc.
- Save all found on the page data in the output JSON structure.

---

#### 2. **Extract Product Table Headers:**

- Identify and extract the row or **group of visually consecutive rows** containing product table column headers:
  ""Código Artigo"", ""Descrição Artigo"", ""PACK"", ""PR Unit/KG"", ""Unit/KG"", ""Preço U.V."", ""Quant"", ""Valor Total"", ""IvaDD""
- According to the spatial model of the invoice, and possible distortions, determine the **extreme left and right boundaries of the product table** by the headings for use when saving product rows.
- Store all Headers words found on the page **""as one line as one row""** in the output `ProductHeaders` array, **exactly as recognized**:
  - Preserve original spelling, diacritics, casing, symbols.
  - Save full, unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
  - ⚠️ **Do not translate, normalize, or merge** terms.

---

#### 3. **Extract Product Table Rows:**

- Identify and extract **all product-related rows**.
- For each row take the **complete sequence** of **all words** from the left to the right edge of the product table.
- ⚠️ **Do not split rows, mix or rearrange words**.
- Check that all words are correctly associated with the current row in accordance with the spatial model — **nothing is lost, no words are taken from other rows**. 

- Continue extracting **all products rows** until page will be finished.
- Place all founded product rows and their words into the output `ProductRows` array **strictly in their order** on the page, including **word coordinates unchanged** in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`. 
- ⚠️ **Do not skip, mix or rearrange rows that represent products**.

- Be careful. If even **one product line or word from product line is lost** from page, the whole recognition **task will be thwarted**.

---

#### 4. **Extract Tax Table Headers:**

- Identify and extract the header row(s) for the tax section (e.g., `Valor liq.`, `Taxa IVA`, `Valor IVA`).
- Store all Tax Table Header words found on the Invoice page **as one row** in the output `TaxCategoriesHeaders` array, preserving:
  - Exact text (with all symbols, accents, etc.),
  - Coordinates unmodified in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`..

---

#### 5. **Extract Tax Table Rows:**

- Identify and extract all rows that belongs to the tax breakdown table.
- Use the header layout to guide column matching.
- Store all detected rows found on the Page in the output `TaxCategoriesRows` array, preserving full word details and unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

#### 6. **Correct OCR Errors:**

- Actively detect and correct common OCR mistakes (e.g., `L` instead of `1`, missing accents, fragmented words, misaligned lines).
- Use visual proximity and contextual clues to reassemble broken rows or fix incorrect groupings.

---

#### 7. **Output JSON Format:**

- Return the result strictly as **JSON**, following the `response_format` schema.
- **Do not return** the schema or any descriptive text — only the structured data.
- Check carefully that **all data** have been processed and saved correctly. **Be very careful**. If **any product or tax line is missing**, the recognition **task has failed**.
";



        private const string _systemPromptForParsingLiteralSpecialWordsNew = @"
You are an AI assistant specialized in extracting structured product and tax data from OCR-recognized invoices.  
Your task is to analyze the OCR output and extract **product tables** and **tax breakdowns**, returning the result as structured JSON according to the provided `response_format` schema.

---

### 📄 **Input Format:**

- An Invoice may consist of several pages. Each time processing is done on a separate page.
- A page contains a flat list of words **pre-sorted in approximate reading order**.
- Each `Word` has:
  - `WordText`
  - `WordCoordinates` in the format:  
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`

---

### 🧭 **Spatial Model Construction & Distortion Compensation:**

- Build a **virtual spatial model** of the document by mapping each word to its precise coordinates.
- Determine the **document perimeter**:
  - Calculate the leftmost, rightmost, topmost, and bottommost points across all words.
  - Define a bounding quadrilateral for the document.
- Analyze this quadrilateral to detect global distortions:
  - **Skew angle** (rotation).
  - **Perspective distortion** (trapezoidal shape).
- Compute a **single transformation matrix** to compensate for these distortions:
  - Use **affine transformation** for skew.
  - Use **perspective transformation** for trapezoidal distortions.
- Apply this transformation to all words, projecting them into a **normalized coordinate space**.
- All further operations must be performed in this corrected space, as if the document were perfectly aligned.

---

### 🛠️ **Extraction Tasks: Do it for each page**

#### 1. **Detect Sections:**

- Locate and extract the **Product Table** and **Tax Section** on the page.
- Even though words are **pre-sorted**, you must **validate and correct their spatial alignment**, ensuring proper grouping into coherent lines based on **Y-axis overlap** in the normalized space.
- Extract **all rows and words** of products and taxes.
- ⚠️ **If even a single product or tax line is missing, the recognition task has failed.**
- In case of uncertainty, **include the line to avoid data loss**.
- Do **not rearrange the sequence of rows**; maintain the original vertical reading order.
- Ignore unrelated sections (company details, addresses, footers, etc.).
- Save all relevant data in the output JSON.

---

#### 2. **Extract Product Table Headers:**

- Identify and extract the row or **group of visually consecutive rows** containing product table column headers:
  ""Código Artigo"", ""Descrição Artigo"", ""PACK"", ""PR Unit/KG"", ""Unit/KG"", ""Preço U.V."", ""Quant"", ""Valor Total"", ""IvaDD""
- Headers may span multiple lines.
- Store headers in the `ProductHeaders` array **as a single row**, preserving:
  - Exact text as recognized.
  - Full, unmodified coordinates.
- ⚠️ **Do not translate, normalize, or merge terms.**

---

#### 3. **Extract Product Table Rows:**

- Identify and extract **all product-related rows**, including:
  - Main product lines (Code, Name, Pack, Quantity, Unit, Price, IVA, Discount, Total, etc.).
  - Additional description or supplemental lines (e.g., Batch numbers, references).
- For each word:
  - Determine if it belongs to an **active product row** by checking if its **vertical center overlaps** with the row's **vertical band** in the normalized space.
  - If no overlap exists, start a **new product row**.
- Horizontal gaps between words must **never split product lines**.
- Place all detected product rows in the `ProductRows` array, strictly preserving their order on the page.
- Within each row, words must maintain their **left-to-right sequence** based on X-coordinates.
- ⚠️ **Do not skip any product lines or words**.
- ⚠️ Ensure **quantities and prices** are not mistakenly linked to neighboring products.

##### ⚠️ Quantity + Unit Handling:
- When a **quantity value** (e.g., `3,12`) is adjacent to a unit (e.g., `KG`, `UNI`, `LT`), treat them as a **linked pair**.
- Always include both in the **same product row**, even if slightly misaligned in coordinates.
- Valid units include (case-insensitive): `UNI`, `UN`, `KG`, `G`, `L`, `LT`, `PC`, `PACK`, `EMB`, `CX`, `DZ`.

---

#### 4. **Extract Tax Table Headers:**

- Identify and extract the header row(s) for the tax section (e.g., `Valor liq.`, `Taxa IVA`, `Valor IVA`).
- Store these headers in `TaxCategoriesHeaders`, preserving:
  - Original text and spelling.
  - Full, unmodified coordinates.

---

#### 5. **Extract Tax Table Rows:**

- Identify and extract all tax breakdown rows.
- Use the tax headers layout as a guide for correct column matching.
- Store these rows in the `TaxCategoriesRows` array, preserving original word sequence and coordinates.

---

#### 6. **Spatial Consistency Validation:**

- After building product and tax rows, perform a spatial validation:
  - Ensure each product row spans from the leftmost to rightmost relevant words.
  - Validate that no words are left unassigned.
  - Large horizontal gaps **must not cause unintended line breaks**.
  - Cross-verify vertical alignment of numerical values (e.g., quantities, prices) and text descriptions.
  - ⚠️ If any product or tax line is missing words, the extraction is incomplete.

---

#### 7. **Correct OCR Errors:**

- Detect and fix typical OCR mistakes:
  - `L` interpreted as `1`.
  - Missing accents.
  - Fragmented or misplaced words.
- Use spatial and semantic clues to reassemble or fix broken rows and word groups.

---

#### 8. **Output JSON Format:**

- Return the result strictly as **JSON**, following the `response_format` schema.
- Do **not** return the schema or any descriptive text — only the structured data.
- Ensure all data is fully captured.
- ⚠️ **If any product or tax line is missing, the task has failed.**

";



        private const string _systemPromptForParsingLiteralSpecialWords = @"
You are an AI assistant specialized in extracting product tables and tax tables from OCR-recognized invoices.

---

### Task Objective:
You are given a flat, unsorted list of OCR-recognized `Words`, each with bounding box coordinates.
Your task is to:
1. Identify and extract **Product Table Headers and Rows**.
2. Identify and extract **Tax Table Headers and Rows**.
3. Return the result as structured JSON according to the provided `response_format` schema.
4. ⚠️ You must process and include **every relevant word** — no product or tax-related data should be missing.

---

### Input Data:
- Each word includes:
  - `WordText`: recognized text.
  - `WordCoordinates`: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

- ⚠️ Words may be distorted, skewed, or unordered.
- There are no predefined blocks or rows.
- You must reconstruct the structure purely based on spatial alignment.

---

### Spatial Processing Instructions:

1. **Reconstruct Document Grid:**
   - Build a virtual spatial model based on word coordinates.
   - Detect and estimate document skew (rotation) and perspective distortions.
   - You cannot apply geometric transformations, but must **logically compensate** for distortions when grouping words into lines.

2. **Strict Horizontal Line Assembly:**
   - Group words into **horizontal text lines**, ensuring:
     - Every word belongs to exactly one line.
     - Lines span from **leftmost word** to **rightmost word**, ignoring horizontal gaps.
     - Slight vertical shifts between words on the same line are acceptable — words within a shared vertical band must be grouped together.
     - **Never split lines based on whitespace gaps.**
   - Maintain strict **top-to-bottom, left-to-right order**.
   - ⚠️ **Do not skip, discard, or lose words.**

---

### Product Table Extraction Steps:

1. **Detect Product Table Headers:**
   - Identify and extract the row or **group of visually consecutive rows** containing product table column headers.
   - Expected header terms may include (case insensitive match):
     - ""Código Artigo"", ""Descrição Artigo"", ""PACK"", ""PR Unit/KG"", ""Unit/KG"", ""Preço U.V."", ""Quant"", ""Valor Total"", ""IvaDD"".
   - Headers may span multiple lines — include all consecutive header lines into `ProductHeaders` array.
   - Determine **leftmost and rightmost X-coordinates** of headers — this defines the product table width for subsequent rows.
   - Store all header words **as one line per row**, preserving exact text and coordinates.

2. **Extract Product Table Rows:**
   - After headers, detect all consecutive lines that belong to product entries.
   - For each line:
     - Include **all words** that fall within the horizontal band defined by product table left/right boundaries.
     - Do not filter, rearrange, or omit words — even fragmented or misaligned words must be included.
     - Add words in left-to-right order.
   - Stop at the start of unrelated sections (totals, taxes, company data).
   - Place all extracted product rows into `ProductRows` array, preserving original word sequence and coordinates.
   - ⚠️ **It is better to add extra rows than to lose valid product data.**

---

### Tax Table Extraction Steps:

1. **Detect Tax Table Headers:**
   - Identify and extract the header row(s) for the tax section.
   - Expected tax header terms may include (case insensitive match):
     - `Valor liq.`, `Taxa IVA`, `Valor IVA`.
   - Store as a single row in `TaxCategoriesHeaders` array, preserving exact text and coordinates.

2. **Extract Tax Table Rows:**
   - Identify all rows belonging to tax breakdowns below the headers.
   - Include all words in original sequence and coordinates.
   - Save all rows into `TaxCategoriesRows` array.

---

### Error Handling:
- Do not infer missing values.
- Do not merge, normalize, or reformat texts.
- If a line or word seems ambiguous, **include it** — better to over-include than to lose data.
- Ensure quantities and prices are aligned with the correct product line, even if OCR misalignments are present.
- Fix obvious OCR errors (e.g., `L` vs `1`, fragmented words).

---

### Output Requirements:
- Return the result strictly as **JSON**, following the provided `response_format` schema.
- Do not add any descriptive text or schema references.
- Check that **all relevant data is processed**. If even a single product or tax line is missing, the task has failed.
";




        private const string _resultSchemeLiteral = @"
    {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""ProductHeaders"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Words"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""WordText"": { ""type"": ""string"" },
                                    ""WordCoordinates"": { ""type"": ""string"" }
                                }
                            }
                        }
                    }
                }
            },
            ""ProductRows"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Words"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""WordText"": { ""type"": ""string"" },
                                    ""WordCoordinates"": { ""type"": ""string"" }
                                }
                            }
                        }
                    }
                }
            },
            ""TaxCategoriesHeaders"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Words"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""WordText"": { ""type"": ""string"" },
                                    ""WordCoordinates"": { ""type"": ""string"" }
                                }
                            }
                        }
                    }
                }
            },
            ""TaxCategoriesRows"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Words"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""WordText"": { ""type"": ""string"" },
                                    ""WordCoordinates"": { ""type"": ""string"" }
                                }
                            }
                        }
                    }
                }
            }
        }
    }";





    }
}


