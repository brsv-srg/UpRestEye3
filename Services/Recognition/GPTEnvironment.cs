using System.Text.Json;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;


namespace UpRestEye3.Services.Recognition
{
    public class GPTEnvironment
    {
        private readonly string _invoiceSchema;
        private readonly string _rmsProductsSchema;
            // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTEnvironment()
        {
            _invoiceSchema = JsonHelper.GetInvoiceSchema();
            _rmsProductsSchema = JsonHelper.GetRMSProductsSchema();
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
       
        public string GetRMSProductSchema() => _rmsProductsSchema;

        public string GetURL() => _url;
        
        public string GetApiKey() => _apiKey;
        
        public string GetReceiptParsingRequestBody(RecognizedDocument invoiceText, InvoiceDTO currentInvoice)
        {
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", // "o1 -preview-2024-09-12",
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPrompt(currentInvoice) },
                        new { role = "user", content = $@"Extract structured data from this Invoice: {JsonSerializer.Serialize(invoiceText)}"} // env.GetTestRequestPrompt()}" }
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
            return JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
        }


        public string GetReceiptMappingRequestBody(InvoiceDTO currentInvoice, List<RMSProductDTO> RMSProduct)
        {
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", 
                messages = new object[]
                {
                        new { role = "system", content = _systemPromptForMappingLiteral },
                        new { role = "user", content = $@"
Match each Product from the list of Products from this Invoice: {JsonSerializer.Serialize(currentInvoice)}  
with the most appropriate RMSProduct and Container from this list of RMSProducts: {JsonSerializer.Serialize(RMSProduct)}"
                            } 
            },
                response_format = new
                {
                    type = "json_schema",
                    json_schemas = new
                    {
                        Invoice = new
                        {
                            name = "Invoice",
                            schema = JsonDocument.Parse(GetInvoiceSchema()).RootElement
                        },
                        RMSProduct = new
                        {
                            name = "RMSProduct",
                            schema = JsonDocument.Parse(GetRMSProductSchema()).RootElement
                        }
                    }
                },

                temperature = 0.1
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
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
You are a helpful restaurant assistant that structures OCR recognized Invoice data into JSON.
Extract structured data from given recognized Invoice. 
01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. The OCR text of the Invoice is input to you to get the result. The Invoice contains Number and Data of Invoice; Supplier data; Consumer (buyer) data; a list of Grocery Products with Code, Name, Unit or Container, Quantity, Tax category and Price for each Product; Total Amount and Tax Amount; and a summary list of tax categories with amounts for each category.
04. The invoice can be on A4 size paper and then it contains detailed data. Or it can be a narrow cashier's cheque from a cash register printer, in which case it contains abbreviated data.
05. The recognized data is delivered as blocks of text that are combined according to their position on the Invoice. 
06. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
07. Define the Name and Tax-ID (or NIF) of the Supplier in the document, they are placed next to each other. {{1}} Also try to find the Supplier bank account - IBAN. It could be located next to the Supplier Name or at the end of the document, but it may not be there in the document. Take the Supplier Tax-ID, Name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
08. Try to define the Consumer, sometimes it is only a Tax-ID (or NIF). {{0}} Sometimes there can also be a Name and you should try to identify it. It's located next to the Tax-ID. You need to take the Consumer Name from the processed document and the Consumer Tax-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
09. Determine the invoice number and date. {{2}} Put the invoice number and date in to the JSON in to the Info section.
10. Define the list of Grocery Products: their Codes (if present), Names, Units, Quantity, Tax Category and Price. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
11. Determine the Tax Amounts by category. They are usually placed after the list of products in a table with column headings. The headings usually contain words or abbreviations such as ‘IVA’ or ‘Tax’, ‘Base’, ‘Quantity’, ‘Amount’ and so on. Column titles do not need to be copied into the final document. Find directly the categories and the amount values by category. The category names contain the percentages by category ‘13’, ‘23’, ‘6’ or the names ‘Normal’, ‘Intermedia’, ‘Redusido’, or abbreviations of these words. Find and put the list of them in JSON in the TaxCategories section.
12. Determine the Total Amount with Tax and the Total Amount of Tax (or IVA). {{3}}  Put them in JSON in the Info section. 
13. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
14. Add your comments about recognized data in the Comments element of the response JSON.
";

        // todo добавить описание всех полей в таблице товаров, какие могут быть сокращения
        // todo подавать на вход все, что распознали в QR - суммы и налоговые категории. в промпте указать, чтобы сверил с тем что распознает
        // todo разобрать пример промпта в телеге, взять ключевые моменты оттуда

        private const string _systemPromptForMappingLiteral = $@"
You are a helpful restaurant assistant that mapping OCR recognized invoice data to data from the restaurant management system and put it into JSON.
Map products from given Invoice (from Invoice Product List) to Products from given RMSProducts list from the restaurant management system. 
01. Discard the unimportant and unnecessary information, leaving only the important data. 
02. Don't change the original Invoice, transpose it as is into the Invoice section of response message strictly according to the JSON structure specified in response_format. Don't add any additional data such as schema descriptions or additional data. Only add to each product in the Invoice Products list the matching RMSProduct IDs into the RMSProductId field. 
03. Take each Product from the Invoice Products list and find a suitable matching product in the RMSProducts list. Note that there may be abbreviated or branded product names in the Invoice Products list. And in the RMSProducts list there could be common names of products used to prepare dishes served in a restaurant. 
04. Using your logic and knowledge of the restaurant business and the business of selling goods, find the most appropriate product match and store the RMSProduct ID and Name in Invoice Product in the RMSProductId and RMSProductName fields.  
05. Also find the most appropriate Container or Unit of measure for the Invoice Product being processed in the RMSContainers list of the selected RMSProduct and store the RMSContainer ID and Name in the RMSContainerId and RMSContainerName fields.
06. If you don't find an appropriate RMSProduct in the RMSProducts list, create a new RMSProduct with the value New in field Status. Create a common Name of product without brand or abbreviation, if it is typical grocery product to prepare dishes. If it is a product with an important brand name or familiar product name, for example some wine, or Coca-Cola, save this brand name to RMSProduct. Put new RMSProduct with all necessary fields and unit/container also into RMSProducts section of response message strictly according to the JSON structure specified in response_format. And also keep the name of new RMSProduct in the RMSProductName field of the Invoice Product.
07. If you find appropriate RMSProduct but don't find appropriate RMSContainer, put existing RMSProduct with Id and the new RMSContainer into RMSProducts section of response message strictly according to the JSON structure specified in response_format. . 

08. Add your comments about recognized data in the Comments element of the response JSON.
";
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;

    }

}
