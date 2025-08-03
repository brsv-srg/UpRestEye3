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
    public class GPTRowsLayoutEnvironment
    {
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTRowsLayoutEnvironment()
        {
        }

        public string GetSystemPrompt(InvoiceDTO currentInvoice)
        {
            return _systemPromptForParsingLiteral;
        }


        private string GetUserPrompt(string invoiceText)
        {
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"\n
Extract the rows of words from this provided OCR invoice text: {invoiceText}.
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
        
        public string GetLayoutRequestBody(SimplePageOfWords invoiceText, InvoiceDTO currentInvoice)
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



        private const string _systemPromptForParsingLiteral = @"
You are an AI assistant specialized in reconstructing structured text lines from OCR-recognized invoice words.

Your task is to analyze the provided flat list of recognized `Words`, each with exact coordinates, and build a structured model of text lines.

---

### Input Data:
- You are given a list of OCR words. Each word includes:
  - `WordText`: the recognized text.
  - `WordCoordinates`: coordinates in the format `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

### Task Instructions:

1. **Spatial Model Construction:**
   - Place all words into a virtual 2D spatial model according to their provided coordinates.
   - Determine the **document perimeter**:
     - Calculate the leftmost, rightmost, topmost, and bottommost points across all words.
     - Use these points to define the document’s bounding quadrilateral.
   - Analyze this quadrilateral to detect global distortions:
     - **Skew angle** (rotation of the document).
     - **Perspective distortion** (trapezoidal deformation).
   - Compute a **single transformation matrix** that compensates for these distortions:
     - Use **affine transformation** if the document is skewed but rectangular.
     - Use **perspective transformation** if the document has trapezoidal distortions.
   - This transformation matrix must be applied to all word coordinates to project them into a normalized, distortion-free coordinate space.
     - After this step, all words will be virtually aligned as if the document were scanned perfectly flat and straight.

2. **Line Grouping Algorithm:**
   - Iterate through all transformed words, placing them into horizontal text lines based on the following logic:
     - For the **first word**, create a new line.
     - For each subsequent word:
       - Compare its Y-coordinate range with the current line’s Y-range.
       - If the word’s vertical center overlaps with the current line’s vertical band (considering estimated line height and inter-line spacing), assign it to the current line.
       - Otherwise, start a new line.
   - Ensure **horizontal (X-axis) order** within each line — words must be sequenced from left to right based on their X-coordinates.
   - Do **not** split lines based on horizontal gaps or whitespace. Always group all words within the same horizontal band into a continuous line.

3. **Per-Word Line Assignment Rule:**
   - Every word must be assigned to exactly one line.
   - Line assignment is determined solely by:
     - Word's transformed Y-coordinate range.
     - Intersection with the current line's vertical band (top/bottom range).
     - Consideration of average line height and inter-line spacing (to prevent merging distinct lines).

4. **Sequence Integrity:**
   - Final output must preserve the natural reading order:
     - Lines ordered from top to bottom based on their corrected vertical positions.
     - Words within each line ordered from left to right.

5. **Output JSON Format:**
   - Place all rows found on the page in the output `Rows` array, **strictly in their order** on the page. 
   - Save full, unmodified coordinates of words in their original format: 
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.
   - Return the result strictly as **JSON**, following the `response_format` schema.
   - **Do not return** the schema or any descriptive text — only the structured data.
   - Check carefully that **all data** have been processed and saved correctly. 
   - **Be very careful**. If **any product or tax line is missing**, the recognition **task has failed**.
";


    private const string _resultSchemeLiteral = @"
    {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""Rows"": {
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


