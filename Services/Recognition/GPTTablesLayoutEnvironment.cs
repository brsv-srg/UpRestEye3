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


        private string GetSystemPromptWords(SimplePageOfWords invoiceText, InvoiceDTO currentInvoice)
        {
            if (currentInvoice?.Supplier?.TaxNumber == "502030712" ||
                invoiceText.Words.Any(w => w.WordText.ToLower().Contains("makro") ) )
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
            string _invoiceInformationPrompt = string.Empty;

            if (currentInvoice.Supplier == null || currentInvoice?.TotalIVA <= 0.0m)
            {
                _invoiceInformationPrompt = "Check everything twice. Losing words is unacceptable. Be careful.";
            }
            else
            {
                _invoiceInformationPrompt = $@"
            Use the following **known invoice details** for validation:
                - **InvoiceNumber**: {currentInvoice?.InvoiceNumber ?? ""},
                - **InvoiceDate**: {currentInvoice?.InvoiceDate.ToString() ?? ""},
                - **SupplierTaxID**: {currentInvoice?.Supplier?.TaxNumber ?? ""},
                - **ConsumerTaxID**: {currentInvoice?.Consumer?.TaxNumber ?? ""},
                - **TotalAmount**: {currentInvoice?.TotalAmount.ToString() ?? ""},
                - **TotalIVA**: {currentInvoice?.TotalIVA.ToString() ?? ""},
                - **Tax Categories**: {JsonSerializer.Serialize(currentInvoice?.TaxCategories ?? new List<TaxesDTO>(), options)}
                ";
            }

            return _invoiceInformationPrompt;
        }

        private string GetProductsSchema() => _resultSchemeLiteral;

        public string GetURL() => _url;

        public string GetApiKey() => _apiKey;
        


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
                        new { role = "system", content = GetSystemPromptWords(invoiceText, currentInvoice) },
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


