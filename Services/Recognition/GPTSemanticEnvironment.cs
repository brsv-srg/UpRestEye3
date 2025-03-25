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
                //model = "gpt-4o", 
                model = "gpt-4o-mini",
                temperature = 0.2,
                top_p = 0.3,
                max_tokens = 2048,
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
                model = "gpt-4o-mini",
                //model = "gpt-4o", 
                temperature = 0.3,
                top_p = 0.3,
                //max_tokens = 2048,
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


        private const string _systemPromptForParsingLiteral = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.
Your task is to extract only the list of Grocery Products from the provided OCR Invoice text, and return a well-structured JSON according to the given schema.

### **Processing Guidelines:**
1. **General Rules**
    - Remove unnecessary symbols and non-relevant information.
    - Ensure strict adherence to the provided JSON structure.
    - **Do not return JSON schema descriptions**, only the extracted data.

2. **Invoice Information**
    - The given OCR recognized data consist blocks and paragraphs of text accompanied by the coordinates of its location on the receipt.
    - The invoice can be on A4 size paper and then it contains detailed data. Or it can be a narrow cashier's cheque from a cash register printer, in which case it contains abbreviated data.
    - Use the provided invoice structure to determine positions and relationships between the fields.
       
3. **Product List Extraction**
    - As a rule, the list in the invoice has a tabular form. 
    - The fields we need in this table could named as  **Code** (if present), **Name**, **Unit**, **Quantity**, **Tax Category** or **IVA**, and **Total Value**, etc., or may be Portuguese names or abbreviations. 
    - Define the key parameters of the Product list: 
        -- Product Code and Name; 
        -- Measure Units, Container, the Count of Units in the Container and the Quantity of Containers;
        -- Tax category and the Total Cost Value of the entire product (unit price multiplied by quantity).
    - The name of the unit of measure must be in accordance with the provided dictionary **MeasureUnits** . 
    - The extracted **Tax Category** for each product must match one of the given categories and percentages.
        -- Tax Categories in Product List may be: '23', '13', '6', or corresponding names:  'Normal', 'Intermedia', 'Redusido', or various abbreviations of names (such as 'Nor', 'Int', 'Red', etc.). 
        -- Sometimes tax categories may be designated by numbers or letters, and these designations are used in the Product list and deciphered in the Tax Category summary list. For Product List match and use direct names of Tax Categories rather than designations.
    - Verify that the total sum of extracted products matches the provided **TotalAmount** (if ProductTotalValue include taxes) or **TotalAmount** - **TotalIVA** (if ProductTotalValue doesn't include taxes).

4. **Output** 
    - Check every extracted product and ensure that the extracted data **matches** the expected values.
    - Output the extracted products in JSON format** strictly following the response schema.
    - If there is a discrepancy, return a warning in the **Comments** field.
";

        private const string _systemPromptForParsingLiteralSpecial01 = $@"
You are an AI assistant specialized in extracting structured product data from OCR-recognized Invoices.
Your task is to extract only the list of Grocery Products from the provided OCR Invoice text, and return a well-structured JSON according to the given schema.

### **Processing Guidelines:**
1. **General Rules**
    - Remove unnecessary symbols and non-relevant information.
    - Ensure strict adherence to the provided JSON structure.
    - **Do not return JSON schema descriptions**, only the extracted data.

2. **Invoice Information**
    - The given OCR recognized data consist blocks and paragraphs of text accompanied by the coordinates of its location on the receipt.
    - The invoice is on the A4 size paper and contains detailed data.
    - Use the provided invoice structure to determine positions and relationships between the fields.

3. **Product List Extraction**  
   - The invoice contains a tabular product list.  
   - Extract and correctly map **each field** from the tabular product list to the corresponding JSON field **exactly as specified below**. Extract them in **exactly this sequence**:  


    | # | **Invoice Table Column** |   **Column Type**        | **Mapped JSON Field**   | **Description** |
    |---|--------------------------|--------------------------|-------------------------|-----------------|
    | 1 | `Codigo Artigo`          | Number and letter string | `ProductCode`           | Code of the product from the recognised invoice (if specified) |
    | 2 | `Descricao Artigo`       | Text                     | `ProductName`           | Name of the product from the recognised invoice |
    | 3 | `PACK`                   | 2-3 Symbols              | `Container`             | Packaging type, container (package, bottle, box, bag, sack, piece, kg, etc.) (also match the appropriate Measure Units by the value of this field in accordance with the provided dictionary **MeasureUnits**)|
    | 4 | `PR Unit/KG`             | Decimal number           | `PricePerUnitKG`        | Price of one measure unit |
    | 5 | `Unit/KG`                | Decimal number           | `UnitsCountInContainer` | Number of units inside the container |
    | 6 | `Preco U.V.`             | Decimal number           | `PricePerContainer`     | Price of one container |
    | 7 | `Quant`                  | Integer number           | `QuantityOfContainers`  | Number of container units purchased |
    | 8 | `Valor Total`            | Decimal number           | `ProductTotalValue`     | Total cost of this product (with or without tax, based on invoice type) |
    | 9 | `IvaDD`                  | Integer number           | `TaxCategory`           | Designation of the Tax category of the product (match using tax summary list) |

   - **DO NOT swap columns.** Maintain this exact mapping structure.  
   - The product table **must contain exactly 9 columns** as specified above.  
   - If the extracted table has more or fewer columns, check for OCR misalignment or errors.  
   - If a column is missing or has incorrect data, add a warning in `Comments`.
   - If needed, reconstruct the table **by splitting columns based on spacing and alignment**.
   - If column separation is unclear, **log an error instead of making assumptions**.


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

    }
}
