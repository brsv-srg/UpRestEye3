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
                model = "gpt-4o", 
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



        private const string _systemPromptForParsingLiteral = $@"
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
   - Identify row (or rows) with product column headers (e.g., Product Code, Description, Quantity, Price, etc.). These may be such or similar words in Portuguese or abbreviations in Portuguese or English.
   - Put this row (or rows) into the ProductHeaders section in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also coordinates exactly without transformations**.

3. **Extract product table rows:**
   - Identify **all rows** which looks like product items. **MOST IMPORTANT THING!!!** Identify **all rows** which looks like product items.
   - Put each row into the ProductRows array in the response JSON as is - **all words in full, all symbols exactly including diacritics, without any transformations and Unicode shielding, and also coordinates exactly without transformations**.

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


