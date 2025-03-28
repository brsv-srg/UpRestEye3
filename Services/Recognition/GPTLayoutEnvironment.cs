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
Extract structured data from this OCR-recognized invoice: {invoiceText}. 

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
                //model = "gpt-4o", 
                model = "gpt-4o-mini",
                temperature = 0.2,
                top_p = 0.3,
                max_tokens = 2048,
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



        private const string _systemPromptForParsingLiteral = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized invoices.
Your task is to identify and extract the table of products/services and list of TAXes from the provided OCR invoice text and return a well-structured JSON according to the response_format schema.

### **Input Format:**
   - The OCR-recognized text is provided in a hierarchical structure: blocks, rows, words.
   - Words are grouped into rows according to the coordinates of their location in the texture.
   - Each element has the format coordinates: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
   - In the rows the words that are maximally similar to each other on the Y coordinate are already stacked, taking into account the error. 

### **Task Requirements:**

1. **Identify the product table and tax list:**
   - Use coordinates to logically group data.
   - Locate rows containing product table data.
   - User coordinates to determine headers and columns data.
   - To define data in a column, use the entire width from the beginning of one column to the beginning of the next column.
   - Remember that the number of columns with data must correspond to the number of column headings.    
   - Remember that some columns may be left-aligned relative to the header, some right-aligned, some in the middle. Check all options. 
   - If you can't determine the required columns by the current coordinates, calculate the midpoint between left and right coordinates of the elements and try to position the columns by this point.


2. **Extract product table headers:**
   - Identify product column headers (e.g., Product Code, Description, Quantity, Price, etc.).
   - Put them into the productHeaders array in the response JSON.
   - Use the full width of the column header before the beginning of the next header to define words related to the column. 


3. **Extract product table rows:**
   - Determine the values of the rows and columns in number and order according to the header list and headers coordinates.
   - Put each row into the productRows array in the response JSON.

4. **Extract tax list headers:**
   - Identify tax list column headers (e.g., IVA, Base, Value, Total, etc.).
   - Put them into the taxCategoryHeaders array in the response JSON.

5. **Extract tax list rows:**
   - Determine the values of the rows and columns in number and order according to the header list.
   - Put each row into the taxCategoryRows array in the response JSON.

6. **Check if the data is correct:**.
   - Compare the amounts on the rows of the product table and tax list with the known invoice parameters input.

7. **Correct OCR errors:**
   - Please note and take into account when analysing that there may be OCR errors and recognition errors, scanning defects, paper breaks and shifts. 
   - Merge or move elements in hierarchy that belong to the same row or column but were split incorrectly.
   - Split merged values if OCR incorrectly combined multiple fields.
   - Correct the data if you see that the OCR has made a mistake.

8. **Return structured data in JSON format:**
   - Output the extracted product table into the JSON format strictly following the response_format schema.
   - Do not return JSON schema, only the data.
";


        private const string _systemPromptForParsingLiteralSpecial01 = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized invoices.
Your task is to identify and extract the table of products/services and list of TAXes from the provided OCR invoice text and return a well-structured JSON according to the response_format schema.

### **Input Format:**
   - The OCR-recognized text is provided in a hierarchical structure: blocks, rows, words.
   - Words are grouped into rows according to the coordinates of their location in the texture.
   - Each element has the format coordinates: (TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY).
   - In the rows the words that are maximally similar to each other on the Y coordinate are already stacked, taking into account the error. 

### **Task Requirements:**

1. **Identify the product table and tax list:**
   - Use coordinates to logically group data.
   - Locate all rows containing product table data.
   - User coordinates to determine headers and columns data.        
   - To define data in a column, use the entire width from the beginning of one column to the beginning of the next column.
   - Remember that the number of columns with data must correspond to the number of column headings.    
   - Remember that some columns may be left-aligned relative to the header, some right-aligned, some in the middle. Check all options. 
   - If you can't determine the required columns by the current coordinates, calculate the midpoint between left and right coordinates of the elements and try to position the columns by this point.

2. **Extract product table**
   - **Column headers:**

        - Identify product column headers and extract them in **exactly this sequence**:  

        | # |**Product Table Column**|   **Column Type**        |  **Description** |
        |---|------------------------|--------------------------|------------------|
        | 1 | Código Artigo          | Number and letter string |  Code of the product from the recognised invoice (if specified) |
        | 2 | Descrição Artigo       | Text                     |  Name of the product from the recognised invoice |
        | 3 | PACK                   | 2-3 Symbols              |  Packaging type, container (package, bottle, box, bag, sack, piece, kg, etc.) (also match the appropriate Measure Units by the value of this field in accordance with the provided dictionary **MeasureUnits**)|
        | 4 | PR Unit/KG             | Decimal number           |  Price of one measure unit |
        | 5 | Unit/KG                | Decimal number           |  Number of units inside the container |
        | 6 | Preço U.V.             | Decimal number           |  Price of one container |
        | 7 | Quant                  | Integer number           |  Number of container units purchased |
        | 8 | Valor Total            | Decimal number           |  Total cost of this product (with or without tax, based on invoice type) |
        | 9 | IvaDD                  | Integer number           |  Designation of the Tax category of the product (match using tax summary list) |
        
        - Put found headers into the productHeaders array in the response JSON.
        - Use the full width of the column header before the beginning of the next header to define words related to the column. 


   - **Product table rows:**
        - Find all the rows that look like the rows of the product table and all the words that can belong to the columns according to the headers coordinates.
        - Put each row into the productRows array in the response JSON.


3. **Extract tax list** 
   - **Сolumn headers:**
       - Identify tax list column headers (e.g., IVA, Base, Value, Total, etc.).
       - Put them into the taxCategoryHeaders array in the response JSON.

   - **Tax list rows:**
       - Determine the values of the rows and columns in number and order according to the header list.
       - Put each row into the taxCategoryRows array in the response JSON.

4. **Check if the data is correct:**.
   - Compare the amounts on the rows of the product table and tax list with the known invoice parameters input.

5. **Correct OCR errors:**
   - Please note and take into account when analysing that there may be OCR errors and recognition errors, scanning defects, paper breaks and shifts. 
   - Merge or move elements in hierarchy that belong to the same row or column but were split incorrectly.
   - Split merged values if OCR incorrectly combined multiple fields.
   - Correct the data if you see that the OCR has made a mistake.

6. **Return structured data in JSON format:**
   - Output the extracted product table into the JSON format strictly following the response_format schema.
   - Do not return JSON schema, only the data.
";



        private const string _resultSchemeLiteral = @"
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


