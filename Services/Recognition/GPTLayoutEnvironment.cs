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
    public class GPTLayoutEnvironment
    {
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTLayoutEnvironment()
        {
        }

        public string GetSystemPrompt(InvoiceDTO currentInvoice)
        {
            if (currentInvoice.Supplier.TaxNumber == "502030712")
                return _systemPromptForParsingLiteralSpecial01;
            else
                return _systemPromptForParsingLiteral;
        }


        public string GetUserPrompt(string invoiceText, InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"\n
Extract the table of products/services and list of TAXes from this provided OCR invoice text: {invoiceText}.

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

        public string GetProductsSchema() => _resultSchemeLiteral;
       
        public string GetURL() => _url;
        
        public string GetApiKey() => _apiKey;
        
        public string GetLayoutRequestBody(ResortedSimplifiedDocument invoiceText, InvoiceDTO currentInvoice)
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
                        new { role = "system", content = GetSystemPrompt(currentInvoice) }, 
                        new { role = "user", content = GetUserPrompt(JsonSerializer.Serialize(invoiceText, options), currentInvoice)}  
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



        private const string _systemPromptForParsingLiteralOld = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized invoices.
Your task is to identify and extract the table of products/services and list of TAXes from the provided OCR invoice text and return a well-structured JSON according to the response_format schema.

### **Input Format:**
   - The OCR-recognized text is provided in a hierarchical structure: blocks, rows, words.
   - Words are grouped into rows according to the coordinates of their location in the texture.
   - Each element has the format coordinates: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
   - In the rows, the words that are maximally similar to each other along the Y coordinate are already stacked, taking into account the OCR error. 

### **Task Requirements:**

1. **Identify the product table and tax list:**
   - Locate rows containing product table data and tax list items.

2. **Extract product table headers:**

   - Identify the row or **group of consecutive rows** containing product column headers (e.g., 'Code', 'Description', 'Quantity', 'Price', etc.).  
      Headers may appear in Portuguese, English, or as abbreviations (e.g., Cod, Qtd, IVA).

   - **Headers may span multiple lines**. If a column name is split across several rows (e.g., 'Desconto' / 'promocional'), include **all relevant rows**.

   - Add all header words to the `ProductHeaders` section **exactly as recognized** — with original spelling, diacritics, symbols, and full bounding box coordinates.  
      **Do not modify, merge, normalize, or translate** the text.

3. **Extract product table rows:**
   - Identify **all rows that represent or relate to products** — including main product lines and any additional rows with comments, variants, or extended descriptions.
   - **Do not stop** after the first group of product-like rows. Continue extracting until a clearly unrelated block begins (e.g., totals, taxes, payment info).
   - Add each row to the `ProductRows` array in the response JSON **exactly as recognized** — preserving:
      - Full original text (including all diacritics and symbols),
      - Original order of words,
      - Exact coordinates (no modifications, shifts, or normalization).

   - Some quantity/unit combinations may be split across multiple rows or columns (e.g., ""3,12"" in one column, ""KG"" in another). You must keep these together, even if they appear slightly offset in Y-coordinate.
   - If possible, merge adjacent rows if they clearly form a logical continuation (e.g., description in one row, quantity/unit in another).

   - Known units: ""UNI"", ""UN"", ""KG"", ""G"", ""L"", ""LT"", ""PC"", ""PACK"", ""EMB"", ""CX"", ""DZ"", etc.
   - These may appear in uppercase or lowercase, or with OCR distortions like ""L"" instead of ""1"".


4. **Extract tax list headers:**
   - Identify row with tax list column headers (e.g., IVA, Base, Value, Total, etc.). These may be such or similar words in Portuguese or abbreviations in Portuguese or English.
   - Put them into the TaxCategoriesHeaders array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also coordinates exactly without transformations**.

5. **Extract tax list rows:**
   - Determine all values of the rows and columns in number and order according to the header list.
   - Put each row into the TaxCategoriesRows array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also coordinates exactly without transformations**.

7. **Correct OCR errors:**
   - Please note and take into account when analyzing that there may be OCR errors and recognition errors, scanning defects, paper breaks and shifts. 
   - Correct the data if you see that the OCR has made a mistake.

8. **Return structured data in JSON format:**
   - Output the extracted product table into the JSON format strictly following the response_format schema.
   - Do not return JSON schema, only the data.

9. For each product row, if a **quantity** value is followed or preceded by a **unit of measure** (e.g., ""UNI"", ""KG"", ""LT"", ""UN"", ""g"", etc.), treat them as logically **linked** and include both words in the same row object. These pairs must not be separated or lost.

10. Pay special attention to units of measure: do **not confuse** them with headers or discard them as noise. Always keep them alongside their quantities.

";




        private const string _systemPromptForParsingLiteral = $@"
You are an AI assistant specialized in extracting structured product and tax data from OCR-recognized invoices.  
Your task is to analyze the OCR output and extract **product tables** and **tax breakdowns**, returning the result as structured JSON according to the provided `response_format` schema.

---

### 📄 **Input Format:**

- The invoice text is provided in a **hierarchical OCR structure**: `Pages → Blocks → Rows → Words`.  
- Each `Row` consists of `Words`, each with exact `WordText` and `WordCoordinates`.  
- Coordinates follow the format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.  
- Rows are grouped by visual alignment along the Y-axis — errors from OCR or scanning are partially corrected.
- ⚠️ **There may be multiple pages. All analysis must include all `Pages[]`, processing them in logical order. Tables may span across pages. Pages might appear out of order — you must infer the correct page sequence based on content and structure (e.g., headers, section titles, continuation of tables).**

---

### 🛠️ **Extraction Tasks:**

#### 1. **Detect Sections:**
- Locate and isolate the product table and the tax section **across all pages**.
- The product and tax tables may be split across multiple pages, or appear in unexpected positions.
- Even if tables are interrupted, repeated, or scattered, identify and reconstruct them correctly.
- Ignore unrelated parts like addresses, notes, footer, etc.

---

#### 2. **Extract Product Table Headers:**

- Identify the row or **group of visually consecutive rows** containing product table column headers (e.g., `Code`, `Description`, `Qty`, `Unit`, `Price`, `IVA`, `Discount`, `Total`, etc.).
- Search for headers **on all pages**, especially near the start of product sections.
- Headers may be in **Portuguese, English**, or use common abbreviations (`Cod`, `Qtd`, `UNI`, `IVA`, etc.).
- **Headers may span multiple lines**. If a column name is split across several rows (e.g., 'Desconto' / 'promocional'), include **all relevant rows**.
- Store all words in the `ProductHeaders` array **exactly as recognized**:
  - Preserve original spelling, diacritics, casing, symbols.
  - Save full, unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
  - **Do not translate, normalize, or merge** terms.

---

#### 3. **Extract Product Table Rows:**

- Capture **all product-related rows**, including:
  - Main product lines,
  - Variants (e.g., flavor, cut),
  - Supplemental rows (e.g., batch number, Lote, descriptions).
- Tables may **continue from one page to the next**, or even start mid-page.
- Continue extraction until clearly unrelated content begins (e.g., totals, taxes, notes).
- Include rows from **all pages** as needed.
- Merge rows from multiple pages into a single logical table in `ProductRows`, even if they are visually separated.

For each row:
- Preserve:
  - Exact sequence of words as recognized,
  - Original text with symbols and casing,
  - Raw coordinates without changes in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
- Add each row to `ProductRows`.

##### ⚠️ Quantity + Unit Handling:
- If a **quantity value** (e.g., `3,12`) is **next to** or **preceded/followed** by a unit (e.g., `KG`, `UNI`, `LT`, `UN`, etc.), treat them as a **logically linked pair**.
- Ensure they are both included in the **same product row**, even if slightly misaligned by coordinates.
- Units must **not be confused with headers** or discarded as noise.

**Examples of valid units (case-insensitive):** `UNI`, `UN`, `KG`, `G`, `L`, `LT`, `PC`, `PACK`, `EMB`, `CX`, `DZ`.

---

#### 4. **Extract Tax Table Headers:**

- Identify the header row(s) for the tax section (e.g., `Incidência`, `Taxa`, `IVA`, `Valor`, `Total`).
- Search **across all pages**, especially near the end of the invoice.
- Store all header words in `TaxCategoriesHeaders`, preserving:
  - Exact text (with all symbols, accents, etc.),
  - Coordinates unmodified in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`..

---

#### 5. **Extract Tax Table Rows:**

- Extract each row that belongs to the tax breakdown table.
- The table may be split or start in the middle of a page.
- Use the header layout to guide column matching.
- Store each row in `TaxCategoriesRows`, preserving full word details and unmodified coordinates in their original format: `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`..

---

#### 6. **Correct OCR Errors:**

- Actively detect and correct common OCR mistakes (e.g., `L` instead of `1`, missing accents, fragmented words, misaligned lines).
- Use visual proximity and contextual clues to reassemble broken rows or fix incorrect groupings.

---

#### 7. **Output JSON Format:**

- Return the result strictly as **JSON**, following the `response_format` schema.
- **Do not return** the schema or any descriptive text — only the structured data.

";




        private const string _systemPromptForParsingLiteralSpecial01 = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized invoices.
Your task is to identify and extract the table of products/services and list of TAXes from the provided OCR invoice text and return a well-structured JSON according to the response_format schema.

### **Input Format:**
   - The OCR-recognized text is provided in a hierarchical structure: blocks, rows, words.
   - Words are grouped into rows according to the coordinates of their location in the texture.
   - Each element has the format coordinates: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
   - In the rows, the words that are maximally similar to each other along the Y coordinate are already stacked, taking into account the OCR error. 

### **Task Requirements:**
1. **Identify the product table and tax list:**
   - Locate rows containing product table data and tax list items.

2. **Extract product table headers:**
   - Identify row (or rows) with product column headers: ""Código Artigo"", ""Descrição Artigo"", ""PACK"", ""PR Unit/KG"", ""Unit/KG"", ""Preço U.V."", ""Quant"", ""Valor Total"", ""IvaDD"" 
   - Put this row (or rows) into the ProductHeaders section in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also with all coordinates exactly without transformations**.

3. **Extract product table rows:**
   - Identify **all rows** which looks like product items. 
   **MOST IMPORTANT THING!!!** Identify **all rows** which looks like product items.
   - Put each row into the ProductRows array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also with all coordinates exactly without transformations**.

4. **Check all rows of product table again:**
    - Check everything again. Make sure all the rows with product items are copied, nothing is lost. 
    - The product list table usually ends close to tax list headers.
    - Don't stop until you get to the tax categories. Copy all rows that are similar to the product row, but discard the rows without product items. 

5. **Extract tax list headers:**
   - Identify row with tax list column headers: ""Valor liq."", ""Taxa IVA"", ""Valor IVA"".
   - Put them into the TaxCategoriesHeaders array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also with all coordinates exactly without transformations**.

6. **Extract tax list rows:**
   - Determine all values of the rows and columns in number and order according to the header list.
   - Put each row into the TaxCategoriesRows array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also with all coordinates exactly without transformations**.

7. **Correct OCR errors:**
   - Please note and take into account when analyzing that there may be OCR errors and recognition errors, scanning defects, paper breaks and shifts. 
   - Correct the data if you see that the OCR has made a mistake.

8. **Return structured data in JSON format:**
   - Output the extracted product table into the JSON format strictly following the response_format schema.
   - Do not return JSON schema, only the data.
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
                        ""RowCoordinates"": { ""type"": ""string"" },
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
                        ""RowCoordinates"": { ""type"": ""string"" },
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
                        ""RowCoordinates"": { ""type"": ""string"" },
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
                        ""RowCoordinates"": { ""type"": ""string"" },
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
        



        private const string _resultSchemeLiteralOld = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""title"": ""InvoiceProductsAndTaxTable"",
  ""type"": ""object"",
  ""properties"": {
    ""productHeaders"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""string""
      },
      ""description"": ""List of column headers in the product table""
    },
    ""productRows"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""array"",
        ""items"": {
          ""type"": ""string""
        }
      },
      ""description"": ""List of rows, each row is an array of string values corresponding to the headers""
    },
    ""taxCategoryHeaders"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""string""
      },
      ""description"": ""List of column headers in the tax categories table""
    },
    ""taxCategoryRows"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""array"",
        ""items"": {
          ""type"": ""string""
        }
      },
      ""description"": ""List of rows for tax categories, each row corresponds to taxCategoryHeaders""
    }
  }
}";





        //        private const string _resultSchemeLiteral = @"
        //{
        //  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        //  ""title"": ""InvoiceProductsTable"",
        //  ""type"": ""object"",
        //  ""properties"": {
        //    ""headers"": {
        //      ""type"": ""array"",
        //      ""items"": {
        //        ""type"": ""string""
        //      },
        //      ""description"": ""List of column headers in the product table""
        //    },
        //    ""rows"": {
        //      ""type"": ""array"",
        //      ""items"": {
        //        ""type"": ""array"",
        //        ""items"": {
        //          ""type"": ""string""
        //        }
        //      },
        //      ""description"": ""List of rows, each row is an array of string values corresponding to the headers""
        //    }
        //  },
        //  ""required"": [""headers"", ""rows""]
        //}";


    }
}


