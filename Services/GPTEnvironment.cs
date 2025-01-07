using UpRestEye3.Models;


namespace UpRestEye3.Services
{
    public class GPTEnvironment 
    {
        private readonly string _responseFormat;

        public GPTEnvironment() 
        {
            _responseFormat = InvoiceJsonHelper.GetSchema();
        }

        public string GetSystemPrompt(Invoice currentInvoice)
        {

            // Дополнительные переменные для формирования запроса
            string _consumerPrompt = string.Empty, _supplierPrompt = string.Empty;
            string _invoicePrompt = string.Empty, _amountPrompt = string.Empty;


            if (currentInvoice.Consumer != null)
            {
                if(!string.IsNullOrWhiteSpace(currentInvoice.Consumer.TaxNumber))
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


            if (!string.IsNullOrWhiteSpace(currentInvoice.Info.InvoiceNumber))
            {
                _invoicePrompt = string.Format(_invoicePromptWithNumber, currentInvoice.Info.InvoiceNumber);
                _invoicePrompt += string.Format(_invoicePromptWithDate, currentInvoice.Info.InvoiceDate.ToShortDateString());
            }

            if (currentInvoice.Info.TotalAmount > 0.0m)
            {
                _amountPrompt = string.Format(_totalAmountPrompt, currentInvoice.Info.TotalAmount);
            }

            if (currentInvoice.Info.TotalIVA > 0.0m)
            {
                _amountPrompt += string.Format(_totalIVAPrompt, currentInvoice.Info.TotalIVA);
            }

            
            return  String.Format(_systemPromptLiteral, _consumerPrompt, _supplierPrompt, _invoicePrompt, _amountPrompt);
        }

        public string GetResponseFormat()
        {
            return _responseFormat;
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


        private const string _systemPromptLiteral = $@"
You are a helpful assistant that structures OCR data into JSON.
Extract structured data from given receipts. 
01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
04. Define the Name and Tax-ID (or NIF) of the Supplier in the document, they are placed next to each other. {{1}} Also try to find the Supplier bank account - IBAN. It could be located next to the Supplier Name or at the end of the document, but it may not be there in the document. Take the Supplier Tax-ID, Name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
05. Try to define the Consumer, sometimes it is only a Tax-ID (or NIF). {{0}} Sometimes there can also be a Name and you should try to identify it. It's located next to the Tax-ID. You need to take the Consumer Name from the processed document and the Consumer Tax-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
06. Determine the invoice number and date. {{2}} Put the invoice number and date in to the JSON in to the Info section.
07. Define the list of items: their Codes (if present), Names, Units, Quantity and Price. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
08. Determine the Tax Amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
09. Determine the Total Amount with Tax and the Total Amount of Tax (or IVA). {{3}}  Put them in JSON in the Info section. 
10. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
11. Add your comments about recognized data in the Comments element of the response JSON.
";


        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;

    }

}
