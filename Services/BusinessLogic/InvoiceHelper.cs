using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using System.Linq.Expressions;
using UpRestEye3.Models.RMSDTO;
using OpenCvSharp.ML;
using System.ComponentModel;



namespace UpRestEye3.Services.BusinessLogic
{
    public static class InvoiceHelper
    {

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
                    invoice.TaxCategories.Add(new TaxesDTO { TaxCategory = GetTaxCategory("0%"), Base = qrCode.Base0, IVA = qrCode.Base0, Total = qrCode.Base0 });
                if (qrCode.Base6 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { TaxCategory = GetTaxCategory("6%"), Base = qrCode.Base6, IVA = qrCode.IVA6, Total = qrCode.Base6 + qrCode.IVA6 });
                if (qrCode.Base13 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { TaxCategory = GetTaxCategory("13%"), Base = qrCode.Base13, IVA = qrCode.IVA13, Total = qrCode.Base13 + qrCode.IVA13 });
                if (qrCode.Base23 > 0)
                    invoice.TaxCategories.Add(new TaxesDTO { TaxCategory = GetTaxCategory("23%"), Base = qrCode.Base23, IVA = qrCode.IVA23, Total = qrCode.Base23 + qrCode.IVA23 });

                invoice.Status = InvoiceStatusEnum.QRCodeProcessed;
                invoice.FilePath = filePath;
            }
            else
            if (filePath != null)
            {
                invoice.Status = InvoiceStatusEnum.RawFile;
                invoice.FilePath = filePath;
            }
            else
            {
                invoice.Status = InvoiceStatusEnum.QRError;
                throw new ArgumentException("Both QRCodeData and filePath are null");
            }
        }

        // TODO Перенести в сервис и запрашивать новые объекты через билдер/фабрику
        public static void CopyInvoice(InvoiceDTO invoiceTarget, InvoiceDTO invoiceSource)
        {
            if (ReferenceEquals(invoiceTarget, invoiceSource))
                return;

            if (invoiceSource.Id != null)
                invoiceTarget.Id = invoiceSource.Id;

            if (invoiceTarget.Supplier == null)
            {
                invoiceTarget.Supplier = new SupplierDTO();
            }
            if (invoiceSource.Supplier != null)
            {
                invoiceTarget.Supplier.Name = invoiceSource.Supplier.Name;
                invoiceTarget.Supplier.TaxNumber = invoiceSource.Supplier.TaxNumber;
                invoiceTarget.Supplier.BankAccount = invoiceSource.Supplier.BankAccount;
                invoiceTarget.Supplier.RMSSupplierId = invoiceSource.Supplier.RMSSupplierId;
            }

            if (invoiceTarget.Consumer == null)
            {
                invoiceTarget.Consumer = new ConsumerDTO();
            }
            if (invoiceSource.Consumer != null)
            {
                invoiceTarget.Consumer.Id = invoiceSource.Consumer.Id;
                invoiceTarget.Consumer.Name = invoiceSource.Consumer.Name;
                invoiceTarget.Consumer.TaxNumber = invoiceSource.Consumer.TaxNumber;
            }

            invoiceTarget.InvoiceNumber = invoiceSource.InvoiceNumber;
            invoiceTarget.InvoiceDate = invoiceSource.InvoiceDate;
            invoiceTarget.TotalIVA = invoiceSource.TotalIVA;
            invoiceTarget.TotalAmount = invoiceSource.TotalAmount;

            invoiceTarget.Products = new List<InvoiceProductDTO>(invoiceSource.Products);

            invoiceTarget.TaxCategories = new List<TaxesDTO>(invoiceSource.TaxCategories);

            invoiceTarget.Products = invoiceSource.Products.Select(p => new InvoiceProductDTO
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Quantity = p.Quantity,
                Container = p.Container,
                Count = p.Count,
                ProductTotalValue = p.ProductTotalValue,
                TaxCategory = p.TaxCategory,
                RMSProduct = RMSProductHelper.CopyRMSProductDTO(p.RMSProduct),
                RMSContainer = RMSProductHelper.CopyRMSContainerDTO(p.RMSContainer),
                RMSStorage = RMSProductHelper.CopyRMSStorageDTO(p.RMSStorage),

                Comments = p.Comments
            }).ToList();

            invoiceTarget.TaxCategories = invoiceSource.TaxCategories.Select(t => new TaxesDTO
            {
                Id = t.Id,
                TaxCategory = t.TaxCategory,
                Base = t.Base,
                IVA = t.IVA,
                Total = t.Total
            }).ToList();

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
                invoiceTarget.Supplier.RMSSupplierId = invoiceSource.Supplier.RMSSupplierId;
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

            invoiceTarget.Products = invoiceSource.Products.Select(p => new InvoiceProductDAO
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Quantity = p.Quantity,
                Container = p.Container,
                Count = p.Count,
                ProductTotalValue = p.ProductTotalValue,
                TaxCategory = p.TaxCategory,
                RMSProduct = RMSProductHelper.CopyRMSProductDAO(p.RMSProduct),
                RMSContainer = RMSProductHelper.CopyRMSContainerDAO(p.RMSContainer),
                RMSStorage = RMSProductHelper.CopyRMSStorageDAO(p.RMSStorage),
                Comments = p.Comments
    }).ToList();

            invoiceTarget.TaxCategories = invoiceSource.TaxCategories.Select(t => new TaxesDAO
            {
                Id = t.Id,
                InvoiceId = t.InvoiceId,
                TaxCategory = t.TaxCategory,
                Base = t.Base,
                IVA = t.IVA,
                Total = t.Total
            }).ToList();

            invoiceTarget.Status = invoiceSource.Status;

            invoiceTarget.FilePath = invoiceSource.FilePath;
        }

        public static TaxCategoryEnum GetTaxCategory(string stringCategory)
        {

            stringCategory = stringCategory.ToLower();
            if (stringCategory.Contains("23") || stringCategory.ToLower().Contains("nor"))
            {
                return TaxCategoryEnum.Normal;
            }
            else if (stringCategory.Contains("13") || stringCategory.ToLower().Contains("int"))
            {
                return TaxCategoryEnum.Intermediate;
            }
            else if (stringCategory.Contains("6") || stringCategory.ToLower().Contains("red"))
            {
                return TaxCategoryEnum.Reduced;
            }
            else if (stringCategory.Contains("0") || stringCategory.ToLower().Contains("na") || stringCategory.ToLower().Contains("nã"))
            {
                return TaxCategoryEnum.Zero;
            }
            else
            {
                throw new ArgumentException("Invalid category string");
            }
        }

        public static decimal GetTaxCategoryPercent(TaxCategoryEnum taxCategory)
        {
            switch (taxCategory)
            {
                case TaxCategoryEnum.Normal:
                    return 23m;
                case TaxCategoryEnum.Intermediate:
                    return 13m;
                case TaxCategoryEnum.Reduced:
                    return 6m;
                case TaxCategoryEnum.Zero:
                    return 0m;
                default:
                    throw new ArgumentException("Invalid category string");
            }
        }




        public static InvoiceDTO? BuildInvoiceDTO(InvoiceDAO? invoiceDAO)
        {
            if (invoiceDAO == null)
                return null;

            var invoiceDTO = new InvoiceDTO();
            invoiceDTO.Id = invoiceDAO.Id;
            invoiceDTO.InvoiceNumber = invoiceDAO.InvoiceNumber;
            invoiceDTO.InvoiceDate = invoiceDAO.InvoiceDate;
            invoiceDTO.TotalIVA = invoiceDAO.TotalIVA;
            invoiceDTO.TotalAmount = invoiceDAO.TotalAmount;
            if (invoiceDAO.Consumer != null)
            {
                invoiceDTO.Consumer = new ConsumerDTO
                {
                    Id = invoiceDAO.Consumer.Id,
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
                    BankAccount = invoiceDAO.Supplier.BankAccount,
                    RMSSupplierId = invoiceDAO.Supplier.RMSSupplierId
                };
            };
            // DTO = DAO
            invoiceDTO.Products = invoiceDAO.Products.Select(p => new InvoiceProductDTO
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                
                Unit = p.Unit,
                Quantity = p.Quantity,
                Container = p.Container,
                Count = p.Count,

                ProductTotalValue = p.ProductTotalValue,

                TaxCategory = p.TaxCategory,
                RMSProduct = RMSProductHelper.BuildRMSProductDTO(p.RMSProduct),
                RMSContainer = RMSProductHelper.BuildRMSContainerDTO(p.RMSContainer),
                RMSStorage = RMSProductHelper.CopyRMSStorageDTO(p.RMSStorage),
                Comments = p.Comments

            }).ToList();

            invoiceDTO.TaxCategories = invoiceDAO.TaxCategories.Select(t => new TaxesDTO
            {
                Id = t.Id,
                TaxCategory = t.TaxCategory,
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
            try
            {
                if (invoiceDTO == null)
                    return null;
                var invoiceDAO = new InvoiceDAO();
                invoiceDAO.Id = invoiceDTO.Id;
                invoiceDAO.InvoiceNumber = invoiceDTO.InvoiceNumber;
                invoiceDAO.InvoiceDate = invoiceDTO.InvoiceDate;
                invoiceDAO.TotalIVA = invoiceDTO.TotalIVA;
                invoiceDAO.TotalAmount = invoiceDTO.TotalAmount;
                if (invoiceDTO.Consumer != null)
                {
                    invoiceDAO.Consumer = new ConsumerDAO
                    {
                        Id = invoiceDTO.Consumer.Id,
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
                        BankAccount = invoiceDTO.Supplier.BankAccount,
                        RMSSupplierId = invoiceDTO.Supplier.RMSSupplierId
                    };
                };

                // DAO = DTO
                invoiceDAO.Products = invoiceDTO.Products.Select(p => new InvoiceProductDAO
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    Container = p.Container,
                    Count = p.Count,

                    ProductTotalValue = p.ProductTotalValue,

                    TaxCategory = p.TaxCategory,
                    RMSProductId = p.RMSProduct != null ? p.RMSProduct.Id : null,
                    RMSProduct = RMSProductHelper.BuildRMSProductDAO(p.RMSProduct),

                    RMSContainerId = p.RMSContainer != null ? p.RMSContainer.Id : null,
                    RMSContainer = RMSProductHelper.BuildRMSContainerDAO(p.RMSContainer),

                    RMSStorageId = p.RMSStorage?.Id,
                    RMSStorage = RMSProductHelper.CopyRMSStorageDAO(p.RMSStorage),


                    Comments = p.Comments
                }).ToList();



                invoiceDAO.TaxCategories = invoiceDTO.TaxCategories.Select(t => new TaxesDAO
                {
                    Id = t.Id,
                    TaxCategory = t.TaxCategory,
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
            catch (Exception ex)
            {
                throw new Exception("Error in BuildInvoiceDAO", ex);
            }
        }

        public static IncomingInvoiceDto MapToIncomingInvoiceDto(InvoiceDTO invoiceDTO)
        {
            return new IncomingInvoiceDto
            {
                //Id = invoiceDTO.Id?.ToString(),
                //Conception = invoiceDTO.Conception,
                //ConceptionCode = invoiceDTO.ConceptionCode,
                Comment = invoiceDTO.Comments,
                //DocumentNumber = invoiceDTO.InvoiceNumber,
                
                DateIncoming = invoiceDTO.InvoiceDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                Invoice = invoiceDTO.InvoiceNumber,
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // надо забирать склады и их подставлять в инвойс
                // DefaultStore = invoiceDTO.DefaultStore,
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // надо забирать поставщика и его подставлять в инвойс
                Supplier = invoiceDTO.Supplier?.RMSSupplierId.ToString(),
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // брать из настроек поставщика и подставлять дату погашения.
                // если это фактура-ресибо, то эта дата равна дате документа
                DueDate = invoiceDTO.InvoiceDate.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                // дата заведения документа
                IncomingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                // false
                UseDefaultDocumentTime = false,

                Status = DocumentStatus.New,
                // опять номер накладной?
                IncomingDocumentNumber = invoiceDTO.InvoiceNumber,

                //EmployeePassToAccount = invoiceDTO.EmployeePassToAccount,
                //TransportInvoiceNumber = invoiceDTO.TransportInvoiceNumber,
                //LinkedOutgoingInvoiceId = invoiceDTO.LinkedOutgoingInvoiceId,
                DistributionAlgorithm = DistributionAlgorithmType.DistributionByAmount,

                
                Items = invoiceDTO.Products?.Select(static (item, index) => new IncomingInvoiceItemDto
                {
                    IsAdditionalExpense = false,
                    Amount = item.RMSContainer == null ? item.Quantity : item.Quantity * (item.RMSContainer.Count),
                    ActualAmount = item.RMSContainer == null ? item.Quantity : item.Quantity * (item.RMSContainer.Count),


                    // SupplierProduct = item.SupplierProduct,
                    // SupplierProductArticle = item.SupplierProductArticle,
                    Product = item.RMSProduct.RMSProductExtGuid.ToString(),
                    ProductArticle = item.RMSProduct.Num,
                    //Producer = item.Producer,
                    Num = index+1,
                    ContainerId = item.RMSContainer?.RMSContainerExtGuid?.ToString() ?? "",
                    AmountUnit = item.RMSProduct.MainUnit.ToString(),
                    //ActualUnitWeight = item.ActualUnitWeight,

                    

                    Sum =    item.ProductTotalValue + item.ProductTotalValue/100*GetTaxCategoryPercent(item.TaxCategory), 
                    VatSum = item.ProductTotalValue / 100 * GetTaxCategoryPercent(item.TaxCategory),

                    Price = (item.ProductTotalValue + item.ProductTotalValue / 100 * GetTaxCategoryPercent(item.TaxCategory)) / (item.Quantity),

                    PriceWithoutVat = item.ProductTotalValue/ (item.Quantity),


                    //DiscountSum = item.DiscountSum,
                    VatPercent = GetTaxCategoryPercent(item.TaxCategory),
                    //PriceUnit = item.PriceUnit,
                    //Code = item.RMSProduct.Num,
                    Store = item.RMSStorage.EntityExtGuid.ToString(),
                    //CustomsDeclarationNumber = item.CustomsDeclarationNumber,
                }).ToArray()
            };
        }

    }

}
