using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IInvoiceValidator
    {
        void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false);
    }

    public abstract class InvoiceValidatorBase : IInvoiceValidator
    {
        public abstract void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false);

        public static IInvoiceValidator CreateValidator(InvoiceStageEnum status)
        {
            return status switch
            {
                InvoiceStageEnum.New => new NewInvoiceValidatorBase(),
                InvoiceStageEnum.QRCodeRecognition => new QRCodeProcessedValidator(),
                InvoiceStageEnum.TextRecognition => new TextProcessedValidator(),
                InvoiceStageEnum.ProductsMapping => new ProductsMappedValidator(),
                InvoiceStageEnum.SavingToSystem => new ProductsSavingValidator(),

                _ => throw new NotSupportedException($"Status {status} is not supported for validation")
            };
        }
    }
    public class NewInvoiceValidatorBase : InvoiceValidatorBase
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false)
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
        public override void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false)
        {
            base.Validate(invoice, customerTaxId);

            if (string.IsNullOrEmpty(invoice.Supplier?.TaxNumber) &&
                (invoice.TotalAmount == null || invoice.TotalAmount <= 0.0m) &&
                (invoice.TotalIVA == null || invoice.TotalIVA <= 0.0m) &&
                !invoice.TaxCategories.Any())
            {
                invoice.StageStatus = InvoiceStatusEnum.NA;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";

                invoice.Comments += "There are no QR code in the document";
                return;
            }

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
                if (tax.Base < 0 || tax.IVA < 0 || tax.Total < 0)
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
        public override void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false)
        {
            base.Validate(invoice, customerTaxId);

            if (invoice.Products.Any(p => string.IsNullOrEmpty(p.ProductName) ||
                                           p.ProductTotalValue < 0 ||
                                           p.TaxCategory == null ||
                                           string.IsNullOrEmpty(p.Unit) ||
                                           p.Quantity < 0
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

            // --- Tax validation step ---
            // 1. Group products by tax category and sum their IVA
            //var productTaxSums = invoice.Products
            //    .Where(p => p.TaxCategory != null)
            //    .GroupBy(p => p.TaxCategory)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.Sum(p => p.ProductTotalValue / 100 * InvoiceHelper.GetTaxCategoryPercent(p.TaxCategory))
            //    );

            // Подсчет сумм налогов по продуктам с группировкой по категориям
            // В зависимости от invoice.ProductsTaxIncluded считаем по-разному:
            Dictionary<TaxCategoryEnum, decimal> productTaxSums;
            if (invoice.ProductsTaxIncluded)
            {
                // Налог включен в сумму по продукту: выделяем налог из общей суммы
                productTaxSums = invoice.Products
                    .Where(p => p.TaxCategory != null)
                    .GroupBy(p => p.TaxCategory)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var percent = InvoiceHelper.GetTaxCategoryPercent(g.Key);
                            // Сумма налога = сумма_продуктов * (percent / (100 + percent))
                            return g.Sum(p =>
                            {
                                var taxPercent = InvoiceHelper.GetTaxCategoryPercent(p.TaxCategory);
                                return p.ProductTotalValue * (taxPercent / (100m + taxPercent));
                            });
                        }
                    );
            }
            else
            {
                // Налог НЕ включен в сумму по продукту: считаем налог как процент от суммы
                productTaxSums = invoice.Products
                    .Where(p => p.TaxCategory != null)
                    .GroupBy(p => p.TaxCategory)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var percent = InvoiceHelper.GetTaxCategoryPercent(g.Key);
                            // Сумма налога = сумма_продуктов * (percent / 100)
                            return g.Sum(p =>
                            {
                                var taxPercent = InvoiceHelper.GetTaxCategoryPercent(p.TaxCategory);
                                return p.ProductTotalValue * (taxPercent / 100m);
                            });
                        }
                    );
            }


            // 2. Group invoice tax categories and sum their IVA
            var invoiceTaxSums = invoice.TaxCategories
                .GroupBy(tc => tc.TaxCategory)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(tc => tc.IVA)
                );

            // 3. Compare sums by category
            bool taxMismatch = false;
            foreach (var kvp in productTaxSums)
            {
                if (!invoiceTaxSums.TryGetValue(kvp.Key, out var invoiceSum) ||
                    Math.Abs(invoiceSum - kvp.Value) > 0.03m)
                {
                    taxMismatch = true;
                    break;
                }
            }

            // 4. Compare total tax
            var totalProductTax = productTaxSums.Values.Sum();
            var totalInvoiceTax = invoiceTaxSums.Values.Sum();
            if (Math.Abs(totalProductTax - totalInvoiceTax) > 0.03m)
            {
                taxMismatch = true;
            }

            if (taxMismatch)
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Tax sums by category or total tax mismatch.";
            }
        }
    }

    public class ProductsMappedValidator : TextProcessedValidator
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false)
        {
            base.Validate(invoice, customerTaxId);

            if (invoice.Products.Any(p => p.RMSProduct == null ||
                                           p.RMSStorage == null))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Missing RMS product or storage for products.";
            }

            if (manually == false && invoice.Products.Any(p => p.RMSProduct != null && !string.IsNullOrEmpty(p.Container) && p.RMSContainer == null))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "Missing RMS container for products.";
            }

            if (invoice.Products.Any(p => p.RMSProduct != null && p.RMSProduct.Status != RMSProductStatusEnum.Synchronized))
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if (!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; ";
                invoice.Comments += "There are some new RMSProducts in the Invoice.";
            }
        }
    }

    public class ProductsSavingValidator : TextProcessedValidator
    {
        public override void Validate(InvoiceDTO invoice, string customerTaxId, bool manually = false)
        {
            if (invoice.StageStatus == InvoiceStatusEnum.Ok)
                return; // No need to validate if already OK

            base.Validate(invoice, customerTaxId);

            if (invoice.StageStatus == InvoiceStatusEnum.Ok)
                invoice.StageStatus = InvoiceStatusEnum.Manual;

        }
    }

}
