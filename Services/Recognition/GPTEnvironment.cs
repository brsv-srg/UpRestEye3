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


namespace UpRestEye3.Services.Recognition
{
    public class GPTEnvironment
    {
        private readonly string _invoiceSchema;
        private readonly string _invoiceAndRmsProductsSchema;
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTEnvironment()
        {
            _invoiceSchema = JsonHelper.GetInvoiceSchema();
            _invoiceAndRmsProductsSchema = JsonHelper.GetInvoiceAndRmsProductsSchema();
        }

        public string GetSystemPrompt(InvoiceDTO currentInvoice)
        {

            // Дополнительные переменные для формирования запроса
            string _consumerPrompt = string.Empty, _supplierPrompt = string.Empty;
            string _invoicePrompt = string.Empty, _amountPrompt = string.Empty;


            if (currentInvoice.Consumer != null)
            {
                if (!string.IsNullOrWhiteSpace(currentInvoice.Consumer.TaxNumber))
                {
                    _consumerPrompt = string.Format(_consumerPromptWithNIF, currentInvoice.Consumer.TaxNumber);
                }
                if (!string.IsNullOrWhiteSpace(currentInvoice.Consumer.Name))
                {
                    _consumerPrompt += string.Format(_consumerPromptWithName, currentInvoice.Consumer.Name);
                }
                else
                {
                    _consumerPrompt += _consumerPromptNoName;
                }
            }


            if (currentInvoice.Supplier != null)
            {
                if (!string.IsNullOrWhiteSpace(currentInvoice.Supplier.TaxNumber))
                {
                    _supplierPrompt = string.Format(_supplierPromptWithNIF, currentInvoice.Supplier.TaxNumber);
                }
                if (!string.IsNullOrWhiteSpace(currentInvoice.Supplier.Name))
                {
                    _supplierPrompt += string.Format(_supplierPromptWithName, currentInvoice.Supplier.Name);
                }
                else
                {
                    _supplierPrompt += _supplierPromptNoName;
                }
            }


            if (!string.IsNullOrWhiteSpace(currentInvoice.InvoiceNumber))
            {
                _invoicePrompt = string.Format(_invoicePromptWithNumber, currentInvoice.InvoiceNumber);
                _invoicePrompt += string.Format(_invoicePromptWithDate, currentInvoice.InvoiceDate.ToShortDateString());
            }

            if (currentInvoice.TotalAmount > 0.0m)
            {
                _amountPrompt = string.Format(_totalAmountPrompt, currentInvoice.TotalAmount);
            }

            if (currentInvoice.TotalIVA > 0.0m)
            {
                _amountPrompt += string.Format(_totalIVAPrompt, currentInvoice.TotalIVA);
            }


            return string.Format(_systemPromptForParsingLiteral, _consumerPrompt, _supplierPrompt, _invoicePrompt, _amountPrompt);
        }

        public string GetInvoiceSchema() => _invoiceSchema;
       
        public string GetInvoiceAndRmsProductsSchema() => _invoiceAndRmsProductsSchema;

        public string GetURL() => _url;
        
        public string GetApiKey() => _apiKey;
        
        public string GetReceiptParsingRequestBody(RecognizedDocument invoiceText, InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", // "o1 -preview-2024-09-12",
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPrompt(currentInvoice) },
                        new { role = "user", content = $@"Extract structured data from this Invoice: {JsonSerializer.Serialize(invoiceText,options)}"} // env.GetTestRequestPrompt()}" }
            },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(GetInvoiceSchema()).RootElement
                    }
                },
                temperature = 0.1
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }


        public string GetReceiptMappingRequestBody(InvoiceDTO currentInvoice, List<RMSProductDTO> RMSProduct)
        {
            var options = JsonHelper.GetSerializerOptions();

            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", 
                messages = new object[]
                {
                        new { role = "system", content = _systemPromptForMappingLiteral },
                        new { role = "user", content = $@"
Match each Product from the Product List from this Invoice: {JsonSerializer.Serialize(currentInvoice, options)}  
 with the most appropriate RMSProduct and RMSContainer from this list of RMSProducts: {JsonSerializer.Serialize(RMSProduct, options)}"
                            } 
            },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "mappedInvoiceDTO",
                        schema = JsonDocument.Parse(GetInvoiceAndRmsProductsSchema()).RootElement
                    }
                },

                temperature = 0.1
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }



        private const string _consumerPromptWithNIF = $@" The Tax-ID of the Consumer should match exactly this Tax-ID: ""{{0}}"".";
        private const string _consumerPromptWithName = $@" The Name of the Consumer should be similar to ""{{0}}"".";
        private const string _consumerPromptNoName = $@" The Consumer Name should be the regular company name, not a string of numbers or other service stub name.";

        private const string _supplierPromptWithNIF = $@" The TAX-ID of the Supplier should match exactly this Tax-ID: ""{{0}}"".";
        private const string _supplierPromptWithName = $@" The Name of the Supplier should be similar to ""{{0}}"".";
        private const string _supplierPromptNoName = $@" The Supplier Name should be the regular company name, not a string of numbers or other service stub name.";

        private const string _invoicePromptWithNumber = $@" The Invoice Number should match exactly this number: ""{{0}}"".";
        private const string _invoicePromptWithDate = $@" The Invoice Date should match this date: ""{{0}}"".";

        private const string _totalAmountPrompt = $@" The Total Amount should be equal to ""{{0}}"".";
        private const string _totalIVAPrompt = $@" The Total Amount of Tax (IVA) should be equal to ""{{0}}"".";


        private const string _systemPromptForParsingLiteral = $@"
You are a helpful restaurant assistant for extracting structured data from OCR-recognized Invoice.
Your task is to process the OCR recognized text of an Invoice and convert it into a predefined JSON in response_format section, taking into account the following information:

01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. The OCR text of the Invoice is input to you to get the result. The Invoice contains Number and Date of Invoice; Supplier data; Consumer (buyer) data; a list of Grocery Products with Code, Name, Unit or Container, Quantity, Tax Category and Price for each Product; Total Amount and Tax Amount; and a summary List of Tax Categories with amounts for each category.
04. The invoice can be on A4 size paper and then it contains detailed data. Or it can be a narrow cashier's cheque from a cash register printer, in which case it contains abbreviated data.
05. The recognized data is delivered in User Request as blocks and paragraphs of text that are combined according to their position on the Invoice. 
06. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
07. Define the Name and Tax-ID (or NIF) of the Supplier in the document, they are placed next to each other. {{1}} Also try to find the Supplier bank account - IBAN. It could be located next to the Supplier Name or at the end of the document, and it may not be in the document at all. Take the Supplier Tax-ID, Name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
08. Try to define the Consumer, sometimes it is only a Tax-ID (or NIF). {{0}} Sometimes there can also be a Name and you should try to identify it. It's located next to the Tax-ID. You need to take the Consumer Name from the processed document and the Consumer Tax-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
09. Determine the invoice number and date. {{2}} Put the invoice number and date into the output JSON into appropriate fields InvoiceNumber and InvoiceDate .
10. Define the list of Grocery Products. As a rule, the list in the invoice has a tabular form. The fields of this table may be named as Code (if present), Name, Unit, Quantity, Tax Category, Price, etc., or may be or abbreviated.  Determine this on the basis of the coordinates. Put the list in JSON in the Products section. 
11. Determine the Final Tax Amounts by category. They are usually placed after the list of products in a table with column headings. The headings usually contain words or abbreviations such as ‘IVA’ or ‘Tax’, ‘Base’, ‘Quantity’, ‘Amount’ and so on. Column titles do not need to be copied into the final document. Find directly the categories and the amount values by category and put the list of them in response JSON in the TaxCategories section.
12. For Tax Categories in Invoice list of Grocery Products and in Final Tax Amount table it may be used the percentages: '23', '13', '6', or corresponding names:  'Normal', 'Intermedia', 'Redusido', or various abbreviations of names (such as 'Nor', 'Int', 'Red', etc.). Keep the same value which you could find, do not change anything.
13. Determine the Total Amount with Tax and the Total Amount of Tax (or IVA). {{3}}  Put them into the output JSON into appropriate fields TotalAmount and TotalIVA. 
14. Verify that the sum of the items in the Products list is equal to the sum in the Total section. If you find any discrepancies, double-check everything.
15. Put any observations or potential issues into the Comments element of the response JSON.

        ";



        // todo подавать на вход все, что распознали в QR - суммы и налоговые категории. в промпте указать, чтобы сверил с тем что распознает
        // todo разобрать пример промпта в телеге, взять ключевые моменты оттуда

        private const string _systemPromptForMappingLiteral = $@"
You are a helpful restaurant assistant for mapping OCR-recognized invoice data to data from the restaurant management system. 
Your task is to map the Invoice Products with the RMSProducts List from the restaurant management system and put mapped products into the predefined JSON in the response_format section.
Be careful and use the following information:

01. Take each product from the Invoice Product List from first to last and find a suitable matching product in the RMSProducts List: 
- Note that there may be abbreviated or branded product names in the Invoice Products list. And in the RMSProducts list there could be common names of products used to prepare dishes served in a restaurant. 
- Product names in the Invoice Product List and in the RMSProducts list can be in Portuguese or English. If needed, interpret the names and make sure to consider possible translations (e.g., ""Tomate"" in Portuguese vs. ""Tomato"" in English).
- Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
        - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name but differ by color/type, so they are different products. Do not match them to the same RMS product if one is red and the other is white.
        - If the RMSProducts list has ""Boina Branco"" (white) but the Invoice has ""Boina Tinto"" (red), they must not be matched directly. Look for the correct color variant or treat it as a new product if none exists.
        - This should be taken into account for other brands of wine, other types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
- Use your knowledge of languages, typical foods and products used in restaurants to make the best match.
- Also find the most appropriate Container or Unit of measure for the Invoice Product being processed in the RMSContainers list of the selected RMSProduct. 
- Save the ID and Name of the existing suitable RMSContainer in the RMSContainerId and RMSContainerName fields of the Invoice Product. 

- Before finalizing the match, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features.
- Once confirmed save the pair of matched products into corresponding elements of mappedProductsListDTO:
    - Put Invoice Product into InvoiceProduct element.
    - Put RMSProduct into RMSProduct element.


03. If you don't find an appropriate RMSProduct in the RMSProducts list, create a new RMSProduct based on the Invoice Product: 
- Use the value ""NewProduct"" for the Status field and value ""GOODS"" for the Type field.
- Choose a common Name for the new RMSProduct without brand or abbreviation, if it is a typical grocery product to prepare dishes.
- If it is a product with an important brand name or a familiar product name (for example, some wine, or Coca-Cola), save this brand name to RMSProduct.
- Save the reason for adding a new RMSProduct and any observations or potential issues in the Comments element of the new RMSProduct. 
- Also create a new suitable RMSContainer in the new RMSProduct. 
- After creating them, do two important things:
    - First: **save the matching** of the new RMSProduct and the Invoice Product:
        - Put the **Name** of the new RMSProduct into the **RMSProductName** field of the Invoice Product.
        - Put the **Name** of the new RMSContainer into the **RMSContainerName** field of the Invoice Product. 
    - Second: **save** the **new** RMSProduct into the **NewRMSProducts** section of the response message

04. If you find a matching existing RMSProduct but don't find any appropriate RMSContainer, create a new RMSContainer based on the container or unit listed in the Invoice:
- Save the new RMSContainer into the corresponding existing RMSProduct.
- Change the value of the Status field of the RMSProduct to ""NewContainer"".
- Save the reason for adding a new RMSContainer and any observations or potential issues in the Comments element of the existing RMSProduct. 
- After creating RMSContainer, do two important things:
    - First: **save the matching** of the existing RMSProduct with the new RMSContainer and the Invoice Product:
        - Put the **Id** and **Name** of the matching RMSProduct into the **RMSProductId**  and **RMSProductName** fields of the Invoice Product.
        - Put the **Name** of the new RMSContainer into the **RMSContainerName** field of the Invoice Product. 
    - Second: **save** the RMSProduct with the new RMSContainer into the **NewRMSProducts** section of the response message

05. Put the Invoice with added mapping information into the Invoice section of the response message:
- Do it strictly according to the JSON structure specified in response_format. 
- Do not add any additional data such as schema descriptions or extra data.
- Include any observations or potential issues in the Comments element of Invoice in the response JSON. 

06. Check everything again. 
- Make sure that nothing has changed in the Invoice, everything is the same except the identifiers RMSProduct and RMSContainer added to each Invoice Product. 
- Make sure that the Invoice tax category, supplier, consumer, all amounts, etc., remain exactly as they were.
- Make sure that all products in the NewRMSProducts list are indeed new products or new RMSContainers.

";

        private const string _oldsystemPromptForMappingLiteral = $@"
You are a helpful restaurant assistant for mapping OCR-recognized invoice data to data from the restaurant management system. 
Your task is to map products in the Invoice Product List in the given Invoice to products in the RMSProducts List from the restaurant management system and put changes into the predefined JSON in the response_format section.
Be careful and use the following information:

01. For each Product from the Invoice Products List find a suitable matching product in the RMSProducts List: 
- Note that there may be abbreviated or branded product names in the Invoice Products list. And in the RMSProducts list there could be common names of products used to prepare dishes served in a restaurant. 
- Product names in the Invoice Product List and in the RMSProducts list can be in Portuguese or English. If needed, interpret the names and make sure to consider possible translations (e.g., ""Tomate"" in Portuguese vs. ""Tomato"" in English).
- Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
        - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name but differ by color/type, so they are different products. Do not match them to the same RMS product if one is red and the other is white.
        - If the RMSProducts list has ""Boina Branco"" (white) but the Invoice has ""Boina Tinto"" (red), they must not be matched directly. Look for the correct color variant or treat it as a new product if none exists.
        - This should be taken into account for other brands of wine, other types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
- Use your knowledge of languages, typical foods and products used in restaurants to make the best match.
- Before finalizing the match, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features.
- Once confirmed, save the **Id** and **Name** of the **matching** RMSProduct in the **RMSProductId** and **RMSProductName** fields of the Invoice Product.

02. Also find the most appropriate Container or Unit of measure for the Invoice Product being processed in the RMSContainers list of the selected RMSProduct. 
- Save the ID and Name of the existing suitable RMSContainer in the RMSContainerId and RMSContainerName fields of the Invoice Product. 

03. If you don't find an appropriate RMSProduct in the RMSProducts list, create a new RMSProduct based on the Invoice Product: 
- Use the value ""NewProduct"" for the Status field and value ""GOODS"" for the Type field.
- Choose a common Name for the new RMSProduct without brand or abbreviation, if it is a typical grocery product to prepare dishes.
- If it is a product with an important brand name or a familiar product name (for example, some wine, or Coca-Cola), save this brand name to RMSProduct.
- Save the reason for adding a new RMSProduct and any observations or potential issues in the Comments element of the new RMSProduct. 
- Also create a new suitable RMSContainer in the new RMSProduct. 
- After creating them, do two important things:
    - First: **save the matching** of the new RMSProduct and the Invoice Product:
        - Put the **Name** of the new RMSProduct into the **RMSProductName** field of the Invoice Product.
        - Put the **Name** of the new RMSContainer into the **RMSContainerName** field of the Invoice Product. 
    - Second: **save** the **new** RMSProduct into the **NewRMSProducts** section of the response message

04. If you find a matching existing RMSProduct but don't find any appropriate RMSContainer, create a new RMSContainer based on the container or unit listed in the Invoice:
- Save the new RMSContainer into the corresponding existing RMSProduct.
- Change the value of the Status field of the RMSProduct to ""NewContainer"".
- Save the reason for adding a new RMSContainer and any observations or potential issues in the Comments element of the existing RMSProduct. 
- After creating RMSContainer, do two important things:
    - First: **save the matching** of the existing RMSProduct with the new RMSContainer and the Invoice Product:
        - Put the **Id** and **Name** of the matching RMSProduct into the **RMSProductId**  and **RMSProductName** fields of the Invoice Product.
        - Put the **Name** of the new RMSContainer into the **RMSContainerName** field of the Invoice Product. 
    - Second: **save** the RMSProduct with the new RMSContainer into the **NewRMSProducts** section of the response message

05. Put the Invoice with added mapping information into the Invoice section of the response message:
- Do it strictly according to the JSON structure specified in response_format. 
- Do not add any additional data such as schema descriptions or extra data.
- Include any observations or potential issues in the Comments element of Invoice in the response JSON. 

06. Check everything again. 
- Make sure that nothing has changed in the Invoice, everything is the same except the identifiers RMSProduct and RMSContainer added to each Invoice Product. 
- Make sure that the Invoice tax category, supplier, consumer, all amounts, etc., remain exactly as they were.
- Make sure that all products in the NewRMSProducts list are indeed new products or new RMSContainers.

";
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;

    }

}
