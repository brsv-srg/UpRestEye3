using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IInvoiceValidator
    {
        void Validate(InvoiceDTO invoice, string customerTaxId);
    }

    public abstract class InvoiceValidatorBase : IInvoiceValidator
    {
        public abstract void Validate(InvoiceDTO invoice, string customerTaxId);

        public static IInvoiceValidator CreateValidator(InvoiceStatusEnum status)
        {
            return status switch
            {
                InvoiceStatusEnum.ProcessError => new QRCodeProcessedValidator(),

                InvoiceStatusEnum.QRError => new QRCodeProcessedValidator(),
                InvoiceStatusEnum.QRCodeProcessed => new QRCodeProcessedValidator(),

                InvoiceStatusEnum.TextRecognitionError => new TextProcessedValidator(),
                InvoiceStatusEnum.TextProcessed => new TextProcessedValidator(),
                
                InvoiceStatusEnum.MappingError => new ProductsMappedValidator(),
                InvoiceStatusEnum.ProductsMapped => new ProductsMappedValidator(),
                _ => throw new NotSupportedException($"Status {status} is not supported for validation")
            };
        }
    }

    public class QRCodeProcessedValidator : InvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            if (string.IsNullOrEmpty(invoice.InvoiceNumber) ||
                invoice.InvoiceDate == default ||
                string.IsNullOrEmpty(invoice.Consumer?.TaxNumber) ||
                string.IsNullOrEmpty(invoice.Supplier?.TaxNumber) ||
                invoice.TotalAmount <= 0 ||
                invoice.TotalIVA < 0 ||
                !invoice.TaxCategories.Any())
            {
                invoice.Status = InvoiceStatusEnum.QRError;
                throw new Exception("Invoice QR code processing error: Missing or invalid data.");
            }

            if (invoice.Consumer.TaxNumber != customerTaxId)
            {
                invoice.Status = InvoiceStatusEnum.QRError;
                throw new Exception("Invoice QR code processing error: Consumer tax number mismatch.");
            }

            foreach (var tax in invoice.TaxCategories)
            {
                if (tax.Base <= 0 || tax.IVA < 0 || tax.Total <= 0)
                {
                    invoice.Status = InvoiceStatusEnum.QRError;
                    throw new Exception("Invoice QR code processing error: Invalid tax category data.");
                }
            }
            invoice.Status = InvoiceStatusEnum.QRCodeProcessed;
        }
    }

    public class TextProcessedValidator : InvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            
            if (invoice.Products.Any(p => string.IsNullOrEmpty(p.ProductName) ||
                                           p.ProductTotalValue < 0 ||
                                           p.TaxCategory == null ||
                                           string.IsNullOrEmpty(p.Unit) ||
                                           p.Quantity <= 0
                                           //|| (!string.IsNullOrEmpty(p.Container) && p.Count == null)
                                           ))

            {
                invoice.Status = InvoiceStatusEnum.TextRecognitionError;
                invoice.Comments = "Missing or invalid product data.";
                throw new Exception("Invoice text processing error: Missing or invalid product data.");
            }

            var totalProductPrice = invoice.Products.Sum(p => p.ProductTotalValue); // * (decimal)p.Quantity);
            if (Math.Abs(totalProductPrice - invoice.TotalAmount) <= 0.02m)
            {
                invoice.ProductsTaxIncluded = true;
            }
            else if (Math.Abs(totalProductPrice - (invoice.TotalAmount - invoice.TotalIVA)) <= 0.02m)
            {
                invoice.ProductsTaxIncluded = false;
            }
            else
            {
                invoice.Status = InvoiceStatusEnum.TextRecognitionError;
                invoice.Comments = "Total product price mismatch.";
                throw new Exception("Invoice text processing error: Total product price mismatch.");
            }

            var productTaxCategories = invoice.Products.Select(p => p.TaxCategory).Distinct();
            if (!productTaxCategories.All(tc => invoice.TaxCategories.Any(t => t.TaxCategory == tc)))
            {
                invoice.Status = InvoiceStatusEnum.TextRecognitionError;
                invoice.Comments = "Tax category mismatch.";
                throw new Exception("Invoice text processing error: Tax category mismatch.");
            }
            invoice.Status = InvoiceStatusEnum.TextProcessed;

        }
    }


    public class ProductsMappedValidator : InvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            if (invoice.Products.Any(p => p.RMSProduct == null ||
                                           (!string.IsNullOrEmpty(p.Container) && p.RMSContainer == null) ||
                                           p.RMSStorage == null))
            {
                invoice.Status = InvoiceStatusEnum.MappingError;
                throw new Exception("Invoice products mapping error: Missing RMS data.");
            }
            invoice.Status = InvoiceStatusEnum.ProductsMapped;
        }
    }

}
