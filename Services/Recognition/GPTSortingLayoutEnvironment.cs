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
You are an AI assistant specialized in processing OCR word lists into a natural visual reading order.

---

### Task Objective:
You are given a flat list of OCR-recognized `Words`, each with exact bounding box coordinates.

Your task is to **sort the words into a strict linear reading sequence**, following their visual appearance on the document from **top to bottom, left to right**.

---

### Input Data:
- A list of `Words`. Each word includes:
  - `WordText`: the recognized text.
  - `WordCoordinates`: the bounding box in the format `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`.

---

### Spatial Corrections:
- Analyze the coordinates of all words to detect any document distortions:
  - Skewed rotation (tilt of text lines at an angle).
  - Perspective distortion (trapezoidal shape caused by angled scanning or photo).
- Calculate a global **Y-axis alignment tolerance** that compensates for these distortions.
- Assume minor distortions can be corrected by using a **vertical merging tolerance threshold** that groups words into horizontal bands, even if their Y-coordinates slightly differ.

---

### Sorting Algorithm:
1. Build a spatial model of the document using word coordinates.
2. Determine an appropriate **line band height** that accounts for font size and inter-line spacing.
3. Group words into **horizontal bands** by checking if their vertical (Y-axis) center points fall within the same line band (using the calculated tolerance).
4. For each horizontal band:
   - Sort all words from **left to right** based on their **TopLeftX** coordinate.
5. Maintain the sequence of horizontal bands from **top to bottom** based on their **TopLeftY** position.
6. Do not perform any line reconstruction or grouping — only produce a linear ordered list of words, reflecting the document's visual reading flow.
7. ⚠️ Do not skip or miss any words.

### Output JSON Format:
   - Place ordered words into the output `Words` array. 
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


