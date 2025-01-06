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

            string _consumerNIF=, _consumerName, _supplierNIF, _supplierName, _invoiceNumber, _invoiceDate, _totalAmount, _totalIVA;

            // TODO перенести в Helper
            if (currentInvoice.Consumer != null)
            {
                _consumerNIF = currentInvoice.Consumer.TaxNumber;
                _consumerName = currentInvoice.Consumer.Name;
            }

            if (currentInvoice.Supplier != null)
            {
                _supplierNIF = currentInvoice.Supplier.TaxNumber;
                _supplierName = currentInvoice.Supplier.Name;
            }
            

            string _invoiceNumber = currentInvoice.Info.InvoiceNumber != null ? currentInvoice.Info.InvoiceNumber : string.Empty;
            string _invoiceDate = currentInvoice.Info.InvoiceDate != null ? currentInvoice.Info.InvoiceDate.ToShortDateString() : string.Empty;

            string _totalAmount = (currentInvoice.Info.TotalAmount > 0.0m)
                                    ? currentInvoice.Info.TotalAmount.ToString() : string.Empty;

            string _totalIVA = currentInvoice.Info.TotalIVA > 0.0m
                                    ? currentInvoice.Info.TotalIVA.ToString() : string.Empty;


            string _consumerPrompt = (!string.IsNullOrWhiteSpace(_consumerNIF) 
                                        ? string.Format(_consumerPromptWithNIF, _consumerNIF) 
                                        : string.Empty) +
                                      (!string.IsNullOrWhiteSpace(_consumerName)
                                        ? string.Format(_consumerPromptWithName, _consumerName)
                                        : string.Empty);

            string _supplierPrompt = !string.IsNullOrWhiteSpace(SupplierNIF)
                                        ? string.Format(_supplierPromptWithNIF, SupplierNIF)
                                        : string.Empty;

            
            return  String.Format(_systemPromptLiteral, _consumerPrompt, _supplierPrompt);
        }

        public string GetResponseFormat()
        {
            return _responseFormat;
        }

        private const string _consumerPromptWithNIF = $@" The TAX-ID of the Consumer should match exactly this TAX-ID: ""{{0}}"".";
        private const string _consumerPromptWithName = $@" The Name of the Consumer should be similar to ""{{0}}"".";

        private const string _supplierPromptWithNIF = $@" The TAX-ID of the supplier should match exactly this TAX-ID: ""{{0}}"".";
        private const string _supplierPromptWithName = $@" The Name of the Supplier should be similar to ""{{0}}"".";

        private const string _invoicePromptWithNumber = $@" The Invoice Number should match exactly this number: ""{{0}}"".";
        private const string _invoicePromptWithDate = $@" The Invoice Date should match this date: ""{{0}}"".";

        private const string _totalAmountPrompt = $@" The Total Amount should be equal to ""{{0}}"".";
        private const string _totalIVAPrompt = $@" The Total IVA should be equal to ""{{0}}"".";


        private const string _systemPromptLiteral = $@"
You are a helpful assistant that structures OCR data into JSON.
Extract structured data from given receipts. 
01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
04. Define the name and TAX-ID of the vendor (or supplier) in the document, they are placed next to each other. {{1}} Also try to find the supplier bank account - IBAN. It could be located next to the supplier's name or at the end of the document, but it may not be there in the document. Take the supplier TAX-ID, name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
05. Try to define the consumer, sometimes it is only a TAX-ID (NIF). {{0}} Sometimes there can also be a Name and you should try to identify it. It's located next to the TAX-ID. You need to take the consumer Name from the processed document and the consumer TAX-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
06. Determine the invoice number and date. {{}} Put the invoice number and date in to the JSON in to the Info section.
07. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
08. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
09. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
10. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
11. Add your comments about recognized data in the Comments element of the response JSON.
";


    }

}
