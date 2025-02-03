using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;



namespace UpRestEye3.Services.BusinessLogic
{
    public class InvoiceHelper
    {

        public InvoiceHelper()
        {
        }


        // TODO Перенести в сервис
        public static void UpdateInvoiceByQR(InvoiceDTO invoice, QRCodeData qrCode, string? filePath)
        {
            if (qrCode != null)
            {
                if (invoice.Consumer == null)
                {
                    invoice.Consumer = new ConsumerDTO
                    {
                        TaxNumber = qrCode.CustomerTaxNumber
                    };
                }
                else
                if (invoice.Consumer.TaxNumber != qrCode.CustomerTaxNumber)
                {
                    invoice.Consumer.TaxNumber = qrCode.CustomerTaxNumber;
                    invoice.Consumer.Name = string.Empty;
                }

                if (invoice.Supplier == null)
                {
                    invoice.Supplier = new SupplierDTO
                    {
                        TaxNumber = qrCode.SupplierTaxNumber
                    };
                }
                else
                if (invoice.Supplier.TaxNumber != qrCode.SupplierTaxNumber)
                {
                    invoice.Supplier.TaxNumber = qrCode.SupplierTaxNumber;
                    invoice.Supplier.Name = string.Empty;
                    invoice.Supplier.BankAccount = string.Empty;
                }

                invoice.InvoiceNumber = qrCode.DocNumber;
                invoice.InvoiceDate = qrCode.DocDate;
                invoice.TotalIVA = qrCode.TotalIVA;
                invoice.TotalAmount = qrCode.TotalAmount;

                if (invoice.TaxCategories == null)
                    invoice.TaxCategories = [];
                else
                    invoice.TaxCategories.Clear();

                if (qrCode.Base0 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { Category = GetTaxCategory("0%"), Base = qrCode.Base0, IVA = qrCode.Base0, Total = qrCode.Base0 });
                if (qrCode.Base6 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { Category = GetTaxCategory("6%"), Base = qrCode.Base6, IVA = qrCode.IVA6, Total = qrCode.Base6 + qrCode.IVA6 });
                if (qrCode.Base13 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { Category = GetTaxCategory("13%"), Base = qrCode.Base13, IVA = qrCode.IVA13, Total = qrCode.Base13 + qrCode.IVA13 });
                if (qrCode.Base23 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { Category = GetTaxCategory("23%"), Base = qrCode.Base23, IVA = qrCode.IVA23, Total = qrCode.Base23 + qrCode.IVA23 });

                invoice.Status = InvoiceStatus.QRCodeProcessed;
                invoice.FilePath = filePath;
            }
            else
            if (filePath != null)
            {
                invoice.Status = InvoiceStatus.RawFile;
                invoice.FilePath = filePath;
            }
            else
            {
                invoice.Status = InvoiceStatus.Error;
                throw new ArgumentException("Both QRCodeData and filePath are null");
            }
        }

        // TODO Перенести в сервис и запрашивать новые объекты через билдер/фабрику
        public static void CopyInvoice(InvoiceDTO invoiceTarget, InvoiceDTO invoiceSource)
        {
            if (invoiceTarget.Supplier == null)
            {
                invoiceTarget.Supplier = new SupplierDTO();
            }
            if (invoiceSource.Supplier != null)
            {
                invoiceTarget.Supplier.Name = invoiceSource.Supplier.Name;
                invoiceTarget.Supplier.TaxNumber = invoiceSource.Supplier.TaxNumber;
                invoiceTarget.Supplier.BankAccount = invoiceSource.Supplier.BankAccount;
            }

            if (invoiceTarget.Consumer == null)
            {
                invoiceTarget.Consumer = new ConsumerDTO();
            }
            if (invoiceSource.Consumer != null)
            {
                invoiceTarget.Consumer.Name = invoiceSource.Consumer.Name;
                invoiceTarget.Consumer.TaxNumber = invoiceSource.Consumer.TaxNumber;
            }

            invoiceTarget.InvoiceNumber = invoiceSource.InvoiceNumber;
            invoiceTarget.InvoiceDate = invoiceSource.InvoiceDate;
            invoiceTarget.TotalIVA = invoiceSource.TotalIVA;
            invoiceTarget.TotalAmount = invoiceSource.TotalAmount;

            invoiceTarget.Products = new List<InvoiceProductDTO>(invoiceSource.Products);

            invoiceTarget.TaxCategories = new List<TaxesDTO>(invoiceSource.TaxCategories);


            if (invoiceSource.Status != null)
            {
                invoiceTarget.Status = invoiceSource.Status;
            }

            if (!string.IsNullOrWhiteSpace(invoiceSource.FilePath))
            {
                invoiceTarget.FilePath = invoiceSource.FilePath;
            }

        }

        public static void CopyInvoice(InvoiceDAO invoiceTarget, InvoiceDAO invoiceSource)
        {
            if (invoiceTarget.Supplier == null)
            {
                invoiceTarget.Supplier = new SupplierDAO();
            }
            if (invoiceSource.Supplier != null)
            {
                invoiceTarget.Supplier.Id = invoiceSource.Supplier.Id;
                invoiceTarget.Supplier.Name = invoiceSource.Supplier.Name;
                invoiceTarget.Supplier.TaxNumber = invoiceSource.Supplier.TaxNumber;
                invoiceTarget.Supplier.BankAccount = invoiceSource.Supplier.BankAccount;
            }
            invoiceTarget.SupplierId = invoiceSource.SupplierId;


            if (invoiceTarget.Consumer == null)
            {
                invoiceTarget.Consumer = new ConsumerDAO();
            }
            if (invoiceSource.Consumer != null)
            {
                invoiceTarget.Consumer.Id = invoiceSource.Consumer.Id;
                invoiceTarget.Consumer.Name = invoiceSource.Consumer.Name;
                invoiceTarget.Consumer.TaxNumber = invoiceSource.Consumer.TaxNumber;
            }
            invoiceTarget.ConsumerId = invoiceSource.ConsumerId;

            invoiceTarget.Id = invoiceSource.Id;
            invoiceTarget.InvoiceNumber = invoiceSource.InvoiceNumber;
            invoiceTarget.InvoiceDate = invoiceSource.InvoiceDate;
            invoiceTarget.TotalIVA = invoiceSource.TotalIVA;
            invoiceTarget.TotalAmount = invoiceSource.TotalAmount;

            invoiceTarget.Products = new List<InvoiceProductDAO>(invoiceSource.Products);

            invoiceTarget.TaxCategories = new List<TaxesDAO>(invoiceSource.TaxCategories);

            invoiceTarget.Status = invoiceSource.Status;

            invoiceTarget.FilePath = invoiceSource.FilePath;
        }

        public static TaxCategory GetTaxCategory(string stringCategory)
        {

            stringCategory = stringCategory.ToLower();
            if (stringCategory.Contains("23") || stringCategory.ToLower().Contains("nor"))
            {
                return TaxCategory.Normal;
            }
            else if (stringCategory.Contains("13") || stringCategory.ToLower().Contains("int"))
            {
                return TaxCategory.Intermediate;
            }
            else if (stringCategory.Contains("6") || stringCategory.ToLower().Contains("red"))
            {
                return TaxCategory.Reduced;
            }
            else if (stringCategory.Contains("0") || stringCategory.ToLower().Contains("na") || stringCategory.ToLower().Contains("nã"))
            {
                return TaxCategory.Zero;
            }
            else
            {
                throw new ArgumentException("Invalid category string");
            }
        }


        public static InvoiceDTO? BuildInvoiceDTO(InvoiceDAO? invoiceDAO)
        {
            if (invoiceDAO == null)
                return null;

            var invoiceDTO = new InvoiceDTO();

            invoiceDTO.InvoiceNumber = invoiceDAO.InvoiceNumber;
            invoiceDTO.InvoiceDate = invoiceDAO.InvoiceDate;
            invoiceDTO.TotalIVA = invoiceDAO.TotalIVA;
            invoiceDTO.TotalAmount = invoiceDAO.TotalAmount;
            if (invoiceDAO.Consumer != null)
            {
                invoiceDTO.Consumer = new ConsumerDTO
                {
                    Name = invoiceDAO.Consumer.Name,
                    TaxNumber = invoiceDAO.Consumer.TaxNumber
                };
            };

            if (invoiceDAO.Supplier != null)
            {
                invoiceDTO.Supplier = new SupplierDTO
                {
                    Name = invoiceDAO.Supplier.Name,
                    TaxNumber = invoiceDAO.Supplier.TaxNumber,
                    BankAccount = invoiceDAO.Supplier.BankAccount
                };
            };

            invoiceDTO.Products = invoiceDAO.Products.Select(p => new InvoiceProductDTO
            {
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Quantity = p.Quantity,
                Price = p.Price,
                Category = p.Category,
                RMSProductId = p.RMSProductId,
                RMSProductName = p.RMSProduct?.Name,
                RMSContainerId = p.RMSContainerId,
                RMSContainerName = p.RMSContainer?.Name

            }).ToList();

            invoiceDTO.TaxCategories = invoiceDAO.TaxCategories.Select(t => new TaxesDTO
            {
                Category = t.Category,
                Base = t.Base,
                IVA = t.IVA,
                Total = t.Total
            }).ToList();

            invoiceDTO.FilePath = invoiceDAO.FilePath;
            invoiceDTO.UploadTime = invoiceDAO.UploadTime;
            invoiceDTO.Comments = invoiceDAO.Comments;
            invoiceDTO.Status = invoiceDAO.Status;
            return invoiceDTO;
        }

        public static InvoiceDAO? BuildInvoiceDAO(InvoiceDTO? invoiceDTO)
        {
            if (invoiceDTO == null)
                return null;
            var invoiceDAO = new InvoiceDAO();
            invoiceDAO.InvoiceNumber = invoiceDTO.InvoiceNumber;
            invoiceDAO.InvoiceDate = invoiceDTO.InvoiceDate;
            invoiceDAO.TotalIVA = invoiceDTO.TotalIVA;
            invoiceDAO.TotalAmount = invoiceDTO.TotalAmount;
            if (invoiceDTO.Consumer != null)
            {
                invoiceDAO.Consumer = new ConsumerDAO
                {
                    Name = invoiceDTO.Consumer.Name,
                    TaxNumber = invoiceDTO.Consumer.TaxNumber
                };
            };
            if (invoiceDTO.Supplier != null)
            {
                invoiceDAO.Supplier = new SupplierDAO
                {
                    Name = invoiceDTO.Supplier.Name,
                    TaxNumber = invoiceDTO.Supplier.TaxNumber,
                    BankAccount = invoiceDTO.Supplier.BankAccount
                };
            };
            invoiceDAO.Products = invoiceDTO.Products.Select(p => new InvoiceProductDAO
            {
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Quantity = p.Quantity,
                Price = p.Price
            }).ToList();
            invoiceDAO.TaxCategories = invoiceDTO.TaxCategories.Select(t => new TaxesDAO
            {
                Category = t.Category,
                Base = t.Base,
                IVA = t.IVA,
                Total = t.Total
            }).ToList();
            invoiceDAO.FilePath = invoiceDTO.FilePath;
            invoiceDAO.UploadTime = invoiceDTO.UploadTime;
            invoiceDAO.Comments = invoiceDTO.Comments;
            invoiceDAO.Status = invoiceDTO.Status;
            return invoiceDAO;
        }
    }

}
