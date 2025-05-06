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

        public static IInvoiceValidator CreateValidator(InvoiceStageEnum status)
        {
            return status switch
            {
                InvoiceStageEnum.New => new NewInvoiceValidatorBase(),
                InvoiceStageEnum.QRCodeRecognition => new QRCodeProcessedValidator(),
                InvoiceStageEnum.TextRecognition => new TextProcessedValidator(),
                InvoiceStageEnum.ProductsMapping => new ProductsMappedValidator(),

                _ => throw new NotSupportedException($"Status {status} is not supported for validation")
            };
        }
    }
    public class NewInvoiceValidatorBase : InvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            invoice.StageStatus = InvoiceStatusEnum.Ok;
            invoice.Comments = string.Empty;

            if (string.IsNullOrEmpty(invoice.FilePath))
            {
                invoice.StageStatus = InvoiceStatusEnum.Error;
                invoice.Comments += "Missing file path for invoice.";
            }
           
        }
    }

    public class QRCodeProcessedValidator : NewInvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            base.Validate(invoice, customerTaxId);

            if (string.IsNullOrEmpty(invoice.InvoiceNumber) ||
                invoice.InvoiceDate == default ||
                string.IsNullOrEmpty(invoice.Consumer?.TaxNumber) ||
                string.IsNullOrEmpty(invoice.Supplier?.TaxNumber) ||
                invoice.TotalAmount <= 0 ||
                invoice.TotalIVA < 0 ||
                !invoice.TaxCategories.Any())
            {
                invoice.StageStatus = InvoiceStatusEnum.Error;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";

                invoice.Comments += "Missing or invalid QR code data";
            }

            if (invoice.Consumer.TaxNumber != customerTaxId)
            {
                invoice.StageStatus = InvoiceStatusEnum.Error;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";

                invoice.Comments += "Consumer QR code tax number mismatch.=";
            }

            foreach (var tax in invoice.TaxCategories)
            {
                if (tax.Base <= 0 || tax.IVA < 0 || tax.Total <= 0)
                {
                    invoice.StageStatus = InvoiceStatusEnum.Error;
                    if (!string.IsNullOrEmpty(invoice.Comments))
                        invoice.Comments += "; ";
                    invoice.Comments += "Invalid QR code tax category data.";
                }
            }
        }
    }

    public class TextProcessedValidator : QRCodeProcessedValidator
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            base.Validate(invoice, customerTaxId);

            if (invoice.Products.Any(p => string.IsNullOrEmpty(p.ProductName) ||
                                           p.ProductTotalValue <= 0 ||
                                           p.TaxCategory == null ||
                                           string.IsNullOrEmpty(p.Unit) ||
                                           p.Quantity <= 0
                                           //|| (!string.IsNullOrEmpty(p.Container) && p.Count == null)
                                           ))

            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if(!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                
                invoice.Comments += "Missing or invalid product data.";
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
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Total product price mismatch.";
            }

            var productTaxCategories = invoice.Products.Select(p => p.TaxCategory).Distinct();
            if (!productTaxCategories.All(tc => invoice.TaxCategories.Any(t => t.TaxCategory == tc)))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Tax category mismatch.";
            }
        }
    }


    public class ProductsMappedValidator : TextProcessedValidator
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId)
        {
            base.Validate(invoice, customerTaxId);

            if (invoice.Products.Any(p => p.RMSProduct == null ||
                                           (!string.IsNullOrEmpty(p.Container) && p.RMSContainer == null) ||
                                           p.RMSStorage == null))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Missing RMS data for products.";
            }

            if (invoice.Products.Any(p => p.RMSProduct != null && p.RMSProduct.Status != RMSProductStatusEnum.FromRMS))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "There are some new RMSProducts in the Invoice.";
            }



        }
    }

}
