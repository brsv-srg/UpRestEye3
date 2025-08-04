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
    public class GPTSortingLayoutEnvironment
    {
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTSortingLayoutEnvironment()
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
You are an AI assistant specialized in reconstructing a **full reading sequence** from OCR-recognized word lists with coordinates.

---

### Task Objective:
You are given a flat list of OCR-recognized `Words`, each with bounding box coordinates.
Your task is to **sort all words into a strict visual reading order from top to bottom, left to right**, exactly as they appear in the document.
You must process **every word in the input list** without skipping, filtering, or discarding any of them.

---

### Input Data:
- A list of `Words`. Each word includes:
  - `WordText`: the recognized text.
  - `WordCoordinates`: the bounding box in the format:
    `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

### Task Instructions:

1. **Spatial Model Construction:**
   - Place all words into a virtual 2D plane using their coordinates.
   - Analyze global distortions (skewed lines, perspective shift) by observing alignment patterns across all words.
   - You cannot perform precise geometric transformations, but you must **logically estimate the orientation** of text lines and compensate by applying a **dynamic Y-alignment tolerance**:
     - If text lines appear slanted, allow vertical offsets in line grouping.
     - If lines appear trapezoidal, adjust groupings accordingly.
   - Always prefer inclusive grouping — if a word might belong to a line, it must be included.

2. **Line Detection and Sorting:**
   - Iteratively group words into **horizontal line bands**:
     - For each word, check if its vertical center aligns (or overlaps) with an existing line's vertical band.
     - The line band height is dynamic, based on neighboring words' heights and global alignment.
     - Include all words that fall within or slightly near this vertical band.
   - For each detected line band, sort words from **left to right** based on their `TopLeftX` coordinate.
   - After processing all words, order all line bands from **top to bottom** based on their `TopLeftY`.

3. **Mandatory Word Inclusion:**
   - Every word from the input must appear in the output.
   - ⚠️ You are **not allowed to skip, discard, or lose** any words.
   - Words that seem misaligned or floating must still be included in the output at their corresponding vertical position.

4. **No Reconstruction or Group Merging:**
   - Do not try to merge lines into logical paragraphs or tables.
   - Do not assume any multi-column layout.
   - Just produce a **flat, ordered list of words in reading sequence**, respecting their spatial positions.

---

### Output JSON Format:
- Return an array named `Words`, containing all words from the input, sorted in correct reading order.
- For each word, include:
  - `WordText`
  - `WordCoordinates` in the original format.
- Return the result strictly as **JSON**, matching the `response_format` schema.
- Do not include any explanations, comments, or schema definitions.
- Ensure no words are skipped, omitted, or misplaced.

---
⚠️ Important:
- This task is about **sorting**. You are not allowed to filter, interpret, or skip words.
- The output must contain **the exact same number of words as the input list**.
- Be extremely careful — if any word is missing from the output, the recognition task has failed.
";



        private const string _resultSchemeLiteral = @"
    {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
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
    }";





    }
}


