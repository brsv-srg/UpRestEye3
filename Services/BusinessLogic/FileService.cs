using Microsoft.AspNetCore.Components.Authorization;
using System.Drawing;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Account;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IInvoiceFileService
    {
        Task<bool> FileProcessAsync(string imagePath, int consumerID);
    }

    // Класс обработки изображения
    public class FileService : IInvoiceFileService
    {
        private readonly IImageFileProcessor _imageProcessor;
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceProcessor _invoiceProcessor;
        private readonly IRMSProductService _rmsProductService;
        private readonly IConsumerService _consumerService;


        public FileService(IInvoiceService invoiceService, IImageFileProcessor imageProcessor, IInvoiceProcessor invoiceProcessor, IRMSProductService rmsProductService, IConsumerService consumerService)
        {
            _invoiceService = invoiceService;
            _imageProcessor = imageProcessor;
            _invoiceProcessor = invoiceProcessor;
            _rmsProductService = rmsProductService;
            _consumerService = consumerService;
        }


        // TODO сделать в отдельном потоке
        public async Task<bool> FileProcessAsync(string filePath, int consumerId)
        {
            var workingInvoice = new InvoiceDTO();

            try
            {

                if (consumerId == null)
                    throw new Exception("ConsumerId not found");

                var consumer = await _consumerService.GetConsumerDTOByIdAsync(consumerId);

                if (consumer == null)
                    throw new Exception("ConsumerId not found");


                // Сохранение пустой накладной в базу данных
                workingInvoice.FilePath = filePath;
                workingInvoice.Status = InvoiceStatusEnum.New;
                workingInvoice.Consumer = new ConsumerDTO(){
                        Name = consumer.Name,
                        TaxNumber = consumer.TaxNumber
                    };
                var invoiceId = await _invoiceService.SaveInvoiceAsync(workingInvoice);

                if (invoiceId == null)
                    throw new Exception("Failed to save invoice.");
                else
                    workingInvoice.Id = invoiceId;


                // Загрузка изображения
                var image = new Bitmap(filePath);
                if (image == null)
                    throw new Exception("Failed to load image.");


                // Распознование QR-кода "в лоб" и с помощью предсказания
                var (basicQRCode, basicProcessedImage) = await _imageProcessor.BasicQRRecognitionAsync(image, filePath);


                if (basicQRCode != null)
                {
                    // Если QR-код распознан, сохраняем изменения и идем на распознавание текста

                    // Проверяем тот ли Consumer
                    if (basicQRCode.CustomerTaxNumber != consumer.TaxNumber)
                        throw new Exception("ConsumerId not match");

                    // Внесение изменений в существующую накладную и схранение изменений в базу данных
                    InvoiceHelper.UpdateInvoiceByQR(workingInvoice, basicQRCode, filePath);
                    await _invoiceService.SaveInvoiceAsync(workingInvoice);

                    // Если есть улучшенное изображение берем его
                    if (basicProcessedImage != null)
                        image = basicProcessedImage;
                }
                else
                {
                    // Если QR-код в лоб и с предсказанием не распознан, идем на распознавание через подбор вариантов
                    var (deepQRCode, deepProcessedImage) = await _imageProcessor.DeepQRRecognitionAsync(image, filePath);
                    if (deepQRCode != null)
                    {
                        // Проверяем тот ли Consumer
                        if (deepQRCode.CustomerTaxNumber != consumer.TaxNumber)
                            throw new Exception("ConsumerId not match");

                        // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                        InvoiceHelper.UpdateInvoiceByQR(workingInvoice, deepQRCode, filePath);

                        await _invoiceService.SaveInvoiceAsync(workingInvoice);

                        // Если есть улучшенное изображение берем его
                        if (deepProcessedImage != null)
                            image = deepProcessedImage;
                    }
                }

                // Распознование текста и формирование полной накладной  
                var recognisedInvoice = await _imageProcessor.DeepTextRecognitionAsync(image, workingInvoice);
                // Если текст распознан, то сохраняем полный документ
                if (recognisedInvoice != null)
                {
                    // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                    InvoiceHelper.CopyInvoice(workingInvoice, recognisedInvoice);

                    await _invoiceService.SaveInvoiceAsync(workingInvoice);
                }
                else
                    throw new Exception("Failed to recognize text.");




                // Получение продуктов из RMS
                var rmsProducts = await _rmsProductService.GetProductsByConsumerIdAsync((int)consumerId);

                // Мапинг на продукты из RMS, подготовка к сохранению в RMS  
                var mappingResult = await _invoiceProcessor.MappingToRMSProductsAsync(workingInvoice, rmsProducts);

                // Если Invoice замеплен и есть новые продукты, то связываем их с Invoice Products
                if (mappingResult.Invoice != null)
                {
                    var invoiceProducts = mappingResult.Invoice.Products;
                    foreach (var rmsProduct in mappingResult.NewRMSProducts)
                    {
                        if (rmsProduct.Status == RMSProductStatusEnum.NewProduct ||
                            rmsProduct.Status == RMSProductStatusEnum.NewContainer)
                        {
                            rmsProduct.Id = await _rmsProductService.SaveProductAsync(rmsProduct);
                        }
                        
                        var matchingProducts = invoiceProducts.Where(p => p.RMSProductName == rmsProduct.Name).ToList();
                        foreach (var invoiceProduct in matchingProducts)
                        {
                            invoiceProduct.RMSProductId = rmsProduct.Id;
                        }
                    }

                    // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                    InvoiceHelper.CopyInvoice(workingInvoice, mappingResult.Invoice);
                    await _invoiceService.SaveInvoiceAsync(workingInvoice);
                }
                else
                    throw new Exception("Failed to map products.");



                return true;
            }
            catch (Exception ex)
            {
                workingInvoice.Comments = ex.Message;
                workingInvoice.Status = InvoiceStatusEnum.Error;
                await _invoiceService.SaveInvoiceAsync(workingInvoice);

                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}