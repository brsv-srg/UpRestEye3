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

1. **Global Distortion Compensation:**
   - Analyze the coordinates of all words to detect overall document distortions, such as:
     - Skew (rotation around Z-axis).
     - Perspective shifts (non-parallel lines).
     - Curvatures or ripples in the scanned paper.
   - Virtually normalize the document plane, so that horizontal lines can be accurately reconstructed, even if the original coordinates contain angular distortions.

2. **Adaptive Line Grouping Model:**
   - Group words into horizontal text lines by dynamically determining a **vertical grouping threshold** based on the document's distortion level:
     - For well-aligned documents, apply a minimal vertical tolerance (few pixels) to avoid merging distinct lines.
     - For distorted documents (with visible skew, curve, or inconsistent baselines), automatically widen the vertical grouping tolerance to ensure fragmented words are still grouped into their logical line.
   - Compute the **estimated line height** by analyzing the typical height of word boxes and inter-line spacing across the document.
     - Use this to define a vertical grouping band for each line.
   - A word belongs to a line if its vertical center (Y) overlaps with the line's vertical band, considering this adaptive tolerance.

3. **Full-Width Line Assembly:**
   - For each detected line, include all words that fall within its vertical band, regardless of horizontal gaps.
   - Never split a line into fragments or sub-columns, even if large horizontal spaces exist.
   - Do not assume multiple columns unless explicitly indicated by structural separators (e.g., visible lines, repeated headers).
   - Ensure all words within a line are ordered from left to right in natural reading sequence.
   - Every word must be assigned to exactly one line — no omissions or duplications.

4. **Ambiguity Resolution in Overlaps:**
   - If a word's vertical position could belong to two adjacent lines, assign it to the line whose vertical center is closer.
   - For isolated words surrounded by whitespace, prefer merging them with the closest line within the adaptive vertical tolerance band.

5. **Sequence Integrity:**
   - Ensure that the final list of lines follows the natural reading order — from top to bottom.
   - Within each line, words must follow left-to-right sequence based on their X-coordinates.

6. **Output JSON Format:**
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


