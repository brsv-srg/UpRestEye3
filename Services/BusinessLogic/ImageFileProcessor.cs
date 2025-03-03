using Microsoft.AspNetCore.Components.Authorization;
using System.Drawing;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UpRestEye3.Controllers;
using System.Collections.Generic;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IImageFileProcessor
    {
        Task<bool> FileProcessAsync(string imagePath, int consumerID);
    }

    // Класс обработки изображения
    public class ImageFileProcessor : IImageFileProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory; // Добавлено: IServiceScopeFactory



        public ImageFileProcessor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }


        // TODO сделать в отдельном потоке
        public async Task<bool> FileProcessAsync(string filePath, int consumerId)
        {
            using (var scope = _serviceScopeFactory.CreateScope()) // Добавлено: создание нового скоупа
            {
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                var consumerService = scope.ServiceProvider.GetRequiredService<IConsumerService>();
                var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
                var invoiceProcessor = scope.ServiceProvider.GetRequiredService<IProductMappingService>();
                var rmsProductService = scope.ServiceProvider.GetRequiredService<IRMSProductService>();
                var rmsMeasureUnitsService = scope.ServiceProvider.GetRequiredService<IRMSMeasureUnitService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>(); 

                var workingInvoice = new InvoiceDTO();

                try
                {

                    if (consumerId == null)
                        throw new Exception("ConsumerId not found");

                    var consumer = await consumerService.GetConsumerDTOByIdAsync(consumerId);

                    if (consumer == null)
                        throw new Exception("ConsumerId not found");


                    // Сохранение пустой накладной в базу данных
                    workingInvoice.FilePath = filePath;
                    workingInvoice.Status = InvoiceStatusEnum.New;
                    workingInvoice.Consumer = new ConsumerDTO()
                    {
                        Name = consumer.Name,
                        TaxNumber = consumer.TaxNumber
                    };
                    var invoiceId = await invoiceService.SaveInvoiceAsync(workingInvoice);

                    if (invoiceId == null)
                        throw new Exception("Failed to save invoice.");
                    
                    workingInvoice.Id = invoiceId;
                    await hubContext.Clients.All.SendAsync("ReceiveMessage", "File saved successfully.");


                    // Загрузка изображения
                    var image = new Bitmap(filePath);
                    if (image == null)
                        throw new Exception("Failed to load image.");


                    // Распознование QR-кода "в лоб" и с помощью предсказания
                    var (basicQRCode, basicProcessedImage) = await imageProcessor.BasicQRRecognitionAsync(image, filePath);


                    if (basicQRCode != null)
                    {
                        // Если QR-код распознан, сохраняем изменения и идем на распознавание текста

                        // Проверяем тот ли Consumer
                        if (basicQRCode.CustomerTaxNumber != consumer.TaxNumber)
                            throw new Exception("ConsumerId not match");

                        // Внесение изменений в существующую накладную и схранение изменений в базу данных
                        InvoiceHelper.UpdateInvoiceByQR(workingInvoice, basicQRCode, filePath);
                        await invoiceService.SaveInvoiceAsync(workingInvoice);

                        // Если есть улучшенное изображение берем его
                        if (basicProcessedImage != null)
                            image = basicProcessedImage;
                        
                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "QR-code recognized successfully.");

                    }
                    else
                    {
                        // Если QR-код в лоб и с предсказанием не распознан, идем на распознавание через подбор вариантов
                        var (deepQRCode, deepProcessedImage) = await imageProcessor.DeepQRRecognitionAsync(image, filePath);
                        if (deepQRCode != null)
                        {
                            // Проверяем тот ли Consumer
                            if (deepQRCode.CustomerTaxNumber != consumer.TaxNumber)
                                throw new Exception("ConsumerId not match");

                            // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                            InvoiceHelper.UpdateInvoiceByQR(workingInvoice, deepQRCode, filePath);

                            await invoiceService.SaveInvoiceAsync(workingInvoice);

                            // Если есть улучшенное изображение берем его
                            if (deepProcessedImage != null)
                                image = deepProcessedImage;
                            
                            await hubContext.Clients.All.SendAsync("ReceiveMessage", "QR-code recognized successfully.");
                        }
                    }



                    var measUnits = await rmsMeasureUnitsService.GetUnitsByConsumerIdAsync((int)consumerId);
                    if (measUnits == null)
                        throw new Exception("Failed to get measure units.");

                    // Распознование текста и формирование полной накладной  
                    var recognisedInvoice = await imageProcessor.DeepTextRecognitionAsync(image, workingInvoice, measUnits);
                    // Если текст распознан, то сохраняем полный документ
                    if (recognisedInvoice != null)
                    {
                        // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                        InvoiceHelper.CopyInvoice(workingInvoice, recognisedInvoice);
                        invoiceId = await invoiceService.SaveInvoiceAsync(workingInvoice);

                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognized successfully.");

                        if (invoiceId == null)
                            throw new Exception("Failed to save invoice.");

                        workingInvoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoiceId);


                    }
                    else
                        throw new Exception("Failed to recognize text.");




                    // Получение продуктов из RMS
                    var rmsProducts = await rmsProductService.GetProductsByConsumerIdAsync((int)consumerId);
                    // Получение единиц изменения

                    // Мапинг на продукты из RMS, подготовка к сохранению в RMS  
                    var mappingResult = await invoiceProcessor.MappingToRMSProductsAsync(workingInvoice, rmsProducts, measUnits);
                    var mappedInvoice = mappingResult.Item1;
                    var newRmsProducts = mappingResult.Item2;

                    // Если Invoice замеплен и есть новые продукты, то связываем их с Invoice Products
                    if (mappedInvoice != null)
                    {


                        foreach (var newRMSProduct in newRmsProducts)
                        {
                            // Сохранение нового продукта в БД
                            newRMSProduct.Id = await rmsProductService.SaveProductAsync(newRMSProduct);
                            if (newRMSProduct.Id == null)
                                throw new Exception("Failed to save product.");

                            // Сохраняем ID сохраненного продукта в накладной
                            var rmsToSaveId = mappedInvoice.Products.Where(p => p.RMSProduct.Name == newRMSProduct.Name && p.RMSProduct.Id == null).FirstOrDefault();
                            if (rmsToSaveId != null)
                                rmsToSaveId.Id = (int)newRMSProduct.Id;

                        }

                        // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                        InvoiceHelper.CopyInvoice(workingInvoice, mappedInvoice);
                        invoiceId = await invoiceService.SaveInvoiceAsync(workingInvoice);
                        if (invoiceId == null)
                            throw new Exception("Failed to save invoice.");

                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "Product mapped successfully.");
                    }
                    else
                        throw new Exception("Failed to map products.");



                    return true;
                }
                catch (Exception ex)
                {
                    workingInvoice.Comments = ex.Message;
                    workingInvoice.Status = InvoiceStatusEnum.Error;
                    await invoiceService.SaveInvoiceAsync(workingInvoice);

                    await hubContext.Clients.All.SendAsync("ReceiveMessage", "Failed to recognize invoice image.");

                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }
    }
}