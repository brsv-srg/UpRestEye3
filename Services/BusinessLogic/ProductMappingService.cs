 using System.Drawing;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;
using Tensorflow;
using System.Collections.Generic;



namespace UpRestEye3.Services.BusinessLogic
{



    public interface IProductMappingService
    {
        Task<(InvoiceDTO, List<RMSProductDTO> newRmsProducts)> MappingToRMSProductsAsync(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages);
    }

    // Класс обработки изображения
    public class ProductMappingService : IProductMappingService
    {
        private readonly IGPTMappingService _gptParser;

        public ProductMappingService(IGPTMappingService gptParser)
        {
            _gptParser = gptParser;
        }

        public async Task<(InvoiceDTO, List<RMSProductDTO> newRmsProducts)> MappingToRMSProductsAsync(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages)
        {
            try
            {
                List<RMSProductDTO> newRmsProducts = new List<RMSProductDTO>();
                var currentConsumerId = currentInvoice.Consumer.Id;
                var currentConsumerTaxId = currentInvoice.Consumer.TaxNumber;

                var mappingResult = await _gptParser.ReceiptMappingByLLM(currentInvoice, rmsProducts, conParam, measUnits, storages);

                // Если Invoice замеплен и есть новые продукты, то связываем их с Invoice Products
                if (mappingResult != null)
                {
                    currentInvoice.Stage = InvoiceStageEnum.ProductsMapped;
                    currentInvoice.StageStatus = InvoiceStatusEnum.Ok;

                    // Проходим по всем замапленным продуктам
                    foreach (var mappedProducts in mappingResult)
                    {
                        // Получаем единицу изменения
                        Guid measUnitGuid = Guid.TryParse(mappedProducts.RMSProduct.MainUnit, out var parsedGuid) 
                                            ? parsedGuid
                                            : measUnits.Where(i => i.Name == mappedProducts.RMSProduct?.MainUnit)?.FirstOrDefault()?.EntityExtGuid ?? Guid.Empty;

                        // Создаем замапленный RMSProduct
                        var mappedRmsProduct = new RMSProductDTO()
                        {
                            Id = mappedProducts.RMSProduct.Id,
                            Name = mappedProducts.RMSProduct.Name,
                            Description = mappedProducts.RMSProduct.Description,
                            Num = mappedProducts.RMSProduct.Num,
                            MainUnit = measUnitGuid,
                            ConsumerId = currentConsumerId,
                            ConsumerTaxId = currentConsumerTaxId,

                            Containers = new List<RMSContainerDTO>(mappedProducts.RMSProduct.Containers.Select(c => new RMSContainerDTO()
                            {
                                Id = c.Id,
                                Num = c.Num,
                                Name = c.Name,
                                Count = c.Count
                            }))
                        };

                        // Если RMSProduct новый, добавляем его в список новых продуктов 
                        if (mappedProducts.NewRMSProduct)
                        {
                            newRmsProducts.Add(mappedRmsProduct);
                        }

                        RMSContainerDTO mappedRMSContainer = null;
                        // Если контейнер задан, то создаем его
                        if (mappedProducts.RMSContainer != null)
                        {

                            // Создаем новый замапленный контейнер
                            mappedRMSContainer = new RMSContainerDTO()
                            {
                                Id = mappedProducts.RMSContainer.Id,
                                Num = mappedProducts.RMSContainer.Num,
                                Name = mappedProducts.RMSContainer.Name,
                                Count = mappedProducts.RMSContainer.Count
                            };


                            // Если контейнер новый и его еще нет в RMSProduct, то добавляем его в RMSProduct
                            if (mappedProducts.NewRMSContainer &&
                                mappedRmsProduct.Containers.Where(c => c.Name == mappedProducts.RMSContainer.Name).Count() == 0)
                            {
                                mappedRmsProduct.Containers.Add(mappedRMSContainer);
                                // И добавляем продукт для сохранения в список новых продуктов
                                newRmsProducts.Add(mappedRmsProduct);

                            }
                        }

                        // Теперь найдем продукт в накладной по Id и присвоим ему замапленный RMSProduct
                        var invoiceProduct = currentInvoice.Products.Where(p => p.Id == mappedProducts.InvoiceProduct.Id).FirstOrDefault();

                        //invoiceProduct.RMSProduct = RMSProductHelper.CopyRMSProductDTO(mappedRmsProduct);
                        invoiceProduct.RMSProduct = mappedRmsProduct;
                        invoiceProduct.RMSContainer = mappedRMSContainer;


                        var storage = storages.Where(s => s.Name == mappedProducts.Storage).FirstOrDefault();
                        invoiceProduct.RMSStorage = storage;


                    }
                }
                return (currentInvoice, newRmsProducts);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to map products to RMS.", ex);
            }
        }
    }
}