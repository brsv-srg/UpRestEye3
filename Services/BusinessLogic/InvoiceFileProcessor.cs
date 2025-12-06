using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Drawing;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.Account;
using UpRestEye3.Services.Controllers;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Integration;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;
using static System.Formats.Asn1.AsnWriter;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IInvoiceFileProcessor
    {
        Task<bool> InvoiceFileProcessAsync(int invoiceId, int consumerID);
        Task<int?> SaveInitialInvoiceAsync(int consumerId, string fileName, string fileFullName);

    }

    // Класс обработки изображения
    public class InvoiceFileProcessor : IInvoiceFileProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory; // Добавлено: IServiceScopeFactory



        public InvoiceFileProcessor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }


        public async Task<int?> SaveInitialInvoiceAsync(int consumerId, string fileName, string fileFullName)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                var invoiceService = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInvoiceService>();
                var consumerService = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IConsumerService>();
                var consumer = await consumerService.GetConsumerDTOByIdAsync(consumerId);

                if (consumer == null)
                    throw new Exception("ConsumerId not found");

                var filePager = new ImageLoader();
                var pagedFilePaths = filePager.SplitMultiPageFiles(fileFullName);

                var invoice = new InvoiceDTO
                {
                    InvoiceNumber = fileName,
                    FilePath = pagedFilePaths,
                    Stage = InvoiceStageEnum.New,
                    StageStatus = InvoiceStatusEnum.Processing
                };
                invoice.Consumer = new ConsumerDTO
                {
                    Name = consumer.Name,
                    TaxNumber = consumer.TaxNumber
                };

                var invoiceId = await invoiceService.SaveInvoiceAsync(invoice);
                if (invoiceId == null)
                    throw new Exception("Failed to save invoice.");

                await hubContext.Clients.All.SendAsync("ReceiveMessage", "File saved successfully.");
                return invoiceId;
            }
        }


        public async Task<bool> InvoiceFileProcessAsync(int invoiceId, int consumerId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                var processingLockService = scope.ServiceProvider.GetRequiredService<IProcessingLockService>();

                //// Prevent concurrent processing of the same invoice
                //if (!await processingLockService.TryLockAsync(invoiceId))
                //{
                //    await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice is already being processed.");
                //    return false;
                //}

                //try
                //{

                    var workingInvoice = await invoiceService.GetInvoiceDTOByIdAsync(invoiceId);
                    if (workingInvoice == null)
                    {
                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice not found.");
                        return false;
                    }
                    if (workingInvoice.Consumer?.Id != consumerId)
                    {
                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "ConsumerId not match.");
                        return false;
                    }

                    // Проверка, конвертация и загрузка изображения
                    var imageLoader = new ImageLoader();
                    var images = imageLoader.LoadImage(workingInvoice.FilePath);

                    if (images == null || images.Count == 0)
                        throw new Exception("Failed to load image.");



                    int switchOffset = (workingInvoice.StageStatus == InvoiceStatusEnum.Ok || workingInvoice.StageStatus == InvoiceStatusEnum.NA) ? 0 : 1;


                    try
                    {
                        switch (workingInvoice.Stage - switchOffset)
                        {
                            // Распознвание QR-кода
                            case InvoiceStageEnum.New:
                                workingInvoice = await ProcessQRCodeAsync(workingInvoice, images, scope, hubContext);
                                if (workingInvoice.StageStatus != InvoiceStatusEnum.Ok && workingInvoice.StageStatus != InvoiceStatusEnum.NA)
                                    break;
                                else
                                    goto case InvoiceStageEnum.QRCodeRecognition;

                            // Распознование текста
                            case InvoiceStageEnum.QRCodeRecognition:
                                workingInvoice = await RecognizeTextAsync(workingInvoice, images.Select(i => i.Item1).ToList(), scope, hubContext);
                                if (workingInvoice.StageStatus != InvoiceStatusEnum.Ok)
                                    break;
                                else
                                    goto case InvoiceStageEnum.TextRecognition;

                            // Маппинг продуктов
                            case InvoiceStageEnum.TextRecognition:
                                workingInvoice = await MapProductsAsync(workingInvoice, scope, hubContext);
                                break;

                            default:
                                throw new Exception("Unknown invoice stage.");
                        }

                        await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice processing is already complete.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await HandleErrorAsync(workingInvoice, ex, invoiceService, hubContext);
                        return false;
                    }
                //}
                //finally
                //{
                //    // Release the lock after processing
                //    await processingLockService.ReleaseLockAsync(invoiceId);
                //}
            }
        }
        private async Task<InvoiceDTO> ProcessQRCodeAsync(InvoiceDTO invoice, List<(Bitmap, string)> images, IServiceScope scope, IHubContext<NotificationHub> hubContext)
        {
            var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
            var consumerService = scope.ServiceProvider.GetRequiredService<IConsumerService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            invoice.Stage = InvoiceStageEnum.QRCodeRecognition;
            invoice.StageStatus = InvoiceStatusEnum.Processing;
            await invoiceService.SaveInvoiceAsync(invoice);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "QR-code recognizing started..");

            var imagesQR = images.Select(i => (i.Item1, i.Item2, new QRCodeData())).ToList();

            for (int i = 0; i < imagesQR.Count; i++)
            {
                var image = imagesQR[i].Item1;
                var filePath = imagesQR[i].Item2;

                var basicQRResult = await imageProcessor.BasicQRRecognitionAsync(image, filePath);


                if (basicQRResult.Item1 == null)
                {
                    var deepQRResult = await imageProcessor.DeepQRRecognitionAsync(image, filePath);


                    if (deepQRResult.Item1 != null)
                        basicQRResult = deepQRResult;
                }

                imagesQR[i] = (imagesQR[i].Item1, imagesQR[i].Item2, basicQRResult.Item1);
            }

            var basicQRCodes = imagesQR.Where(i => i.Item3 != null).Select(i => i.Item3).Distinct().ToList();

            if (basicQRCodes.Count > 1)
            {
                throw new Exception("Failed to recognize: multiple QR codes have been detected.");

            }

            var consumer = await consumerService.GetConsumerDTOByIdAsync((int)invoice.Consumer.Id);
            if (consumer == null)
                throw new Exception("ConsumerId not found");

            if (basicQRCodes.Count != 0)
            {

                var basicQRCode = basicQRCodes.FirstOrDefault();
                if (basicQRCode.CustomerTaxNumber != consumer.TaxNumber)
                    throw new Exception("ConsumerId not match");

                InvoiceHelper.UpdateInvoiceByQR(invoice, basicQRCode, invoice.FilePath);
            }

            // Валидация накладной после QR
            var validatorQR = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.QRCodeRecognition);
            validatorQR.Validate(invoice, consumer.TaxNumber);

            // Сохранение изменений в базу данных
            await invoiceService.SaveInvoiceAsync(invoice);
            invoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);

            if(invoice.StageStatus == InvoiceStatusEnum.Ok)
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "QR-code recognized successfully.");
            else
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "QR-code recognized with errors.");

            return invoice;
        }

        private async Task<InvoiceDTO> RecognizeTextAsync(InvoiceDTO invoice, List<Bitmap> images, IServiceScope scope, IHubContext<NotificationHub> hubContext)
        {
            // TODO доделать тут
            var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
            var rmsMeasureUnitsService = scope.ServiceProvider.GetRequiredService<IRMSMeasureUnitService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            invoice.Stage = InvoiceStageEnum.TextRecognition;
            invoice.StageStatus = InvoiceStatusEnum.Processing;
            await invoiceService.SaveInvoiceAsync(invoice);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognizing started..");

            invoice = await imageProcessor.DeepTextRecognitionAsync(images, invoice);

            var validatorText = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.TextRecognition);
            validatorText.Validate(invoice, invoice.Consumer.TaxNumber);

            await invoiceService.SaveInvoiceAsync(invoice);
            invoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);

            if (invoice.StageStatus == InvoiceStatusEnum.Ok)
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognized successfully!");
            else
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognized with errors!");
            return invoice;
        }

        private async Task<InvoiceDTO> MapProductsAsync(InvoiceDTO invoice, IServiceScope scope, IHubContext<NotificationHub> hubContext)
        {
            var rmsProductService = scope.ServiceProvider.GetRequiredService<IRMSProductService>();
            var measureService = scope.ServiceProvider.GetRequiredService<IRMSMeasureUnitService>();
            var rmsAccountService = scope.ServiceProvider.GetRequiredService<IRMSAccountsService>();
            var invoiceProcessor = scope.ServiceProvider.GetRequiredService<IProductMappingService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var conParamService = scope.ServiceProvider.GetRequiredService<IConnectionParameterService>();
            var ragService = scope.ServiceProvider.GetRequiredService<IRagDataService>();

            invoice.Stage = InvoiceStageEnum.ProductsMapping;
            invoice.StageStatus = InvoiceStatusEnum.Processing;
            await invoiceService.SaveInvoiceAsync(invoice);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Products mapping started..");

            var rmsProducts = await rmsProductService.GetProductsByConsumerIdAsync((int)invoice.Consumer.Id);
            var measureUnits = await measureService.GetUnitsByConsumerIdAsync((int)invoice.Consumer.Id);
            var storages = await rmsAccountService.GetAccountsByConsumerIdAsync((int)invoice.Consumer.Id);
            var conParam = await conParamService.GetConnectionParameterDTOByCustomerIdAsync((int)invoice.Consumer.Id);
            var ragVectorStoreId = await ragService.GetRagDTOByCustomerIdAsync((int)invoice.Consumer.Id);

            var mappingResult = await invoiceProcessor.MappingToRMSProductsAsync(invoice, rmsProducts, conParam, measureUnits, storages, ragVectorStoreId.VectorStoreId);
            var mappedInvoice = mappingResult.Item1;
            var newRmsProducts = mappingResult.Item2;

            if (mappedInvoice == null)
                throw new Exception("Failed to map products.");

            foreach (var newRMSProduct in newRmsProducts)
            {
                var updatedRMSProduct = await rmsProductService.SaveProductAsync(newRMSProduct);
                if (updatedRMSProduct == null)
                    throw new Exception("Failed to save product.");

                if (newRMSProduct.Status == RMSProductStatusEnum.NewProduct)
                {

                    var rmsToSaveId = mappedInvoice.Products
                        .Where(p => p.RMSProduct?.Name == updatedRMSProduct.Name && p.RMSProduct?.Id == null)
                        .FirstOrDefault();
                    if (rmsToSaveId != null)
                        rmsToSaveId.RMSProduct.Id = (int)updatedRMSProduct.Id;
                }

                if (newRMSProduct.Status == RMSProductStatusEnum.NewContainer)
                {
                    // Сохраняем все новые контейнеры
                    // Сперва надо отобрать все продукты по ID (один продукт может быть несолько раз в накладной)
                    var mappedProducts = mappedInvoice.Products
                        .Where(p => p.RMSProduct?.Id == updatedRMSProduct.Id)
                        .ToList();

                    // Проходим по всем продуктам и обновляем контейнеры
                    foreach (var mappedProduct in mappedProducts)
                    {
                        if (mappedProduct.RMSContainer != null)
                            mappedProduct.RMSContainer.Id = updatedRMSProduct.Containers.FirstOrDefault(c => c.Name == mappedProduct.RMSContainer?.Name).Id;

                    }
                
                }

            }

            InvoiceHelper.CopyInvoice(invoice, mappedInvoice);

            var validatorMapp = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.ProductsMapping);
            validatorMapp.Validate(invoice, invoice.Consumer.TaxNumber);

            await invoiceService.SaveInvoiceAsync(invoice);
            invoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);


            if (invoice.StageStatus == InvoiceStatusEnum.Ok)
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Products mapped successfully.");
            else
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Products mapped with errors.");

            return invoice;
        }

        private async Task HandleErrorAsync(InvoiceDTO invoice, Exception ex, IInvoiceService invoiceService, IHubContext<NotificationHub> hubContext)
        {
            if (!string.IsNullOrWhiteSpace(invoice.Comments))
                invoice.Comments += "; ";
            invoice.Comments += ex.Message;

            invoice.StageStatus = InvoiceStatusEnum.Error;
            var prevInvoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);
            if (prevInvoice != null)
            {
                prevInvoice.Comments = invoice.Comments;
                prevInvoice.Stage = invoice.Stage;
                prevInvoice.StageStatus = invoice.StageStatus;
            }

            await invoiceService.SaveInvoiceAsync(prevInvoice);
            invoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);

            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Failed to process invoice: " + ex.Message);

            Console.WriteLine(ex.Message);
        }
    }
}