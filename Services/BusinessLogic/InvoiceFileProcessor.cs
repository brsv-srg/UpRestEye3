using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Drawing;
using UpRestEye3.Components.Pages;
using UpRestEye3.Controllers;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.Account;
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
        Task<int?> SaveInitialInvoiceAsync(int consumerId, string filePath);

    }

    // Класс обработки изображения
    public class InvoiceFileProcessor : IInvoiceFileProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory; // Добавлено: IServiceScopeFactory



        public InvoiceFileProcessor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }


        public async Task<int?> SaveInitialInvoiceAsync(int consumerId, string filePath)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                var invoiceService = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInvoiceService>();
                var consumerService = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IConsumerService>();
                var consumer = await consumerService.GetConsumerDTOByIdAsync(consumerId);

                if (consumer == null)
                    throw new Exception("ConsumerId not found");

                var invoice = new InvoiceDTO
                {
                    InvoiceNumber = filePath,
                    FilePath = filePath,
                    Stage = InvoiceStageEnum.New,
                    StageStatus = InvoiceStatusEnum.Ok
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

                // Prevent concurrent processing of the same invoice
                if (!await processingLockService.TryLockAsync(invoiceId))
                {
                    await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice is already being processed.");
                    return false;
                }

                try
                {

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
                    var image = imageLoader.LoadImage(workingInvoice.FilePath);

                    if (image == null)
                        throw new Exception("Failed to load image.");



                    int switchOffset = workingInvoice.StageStatus == InvoiceStatusEnum.Ok ? 0 : 1;


                    try
                    {
                        switch (workingInvoice.Stage - switchOffset)
                        {
                            // Распознвание QR-кода
                            case InvoiceStageEnum.New:
                                workingInvoice = await ProcessQRCodeAsync(workingInvoice, image, scope, hubContext);
                                if (workingInvoice.StageStatus != InvoiceStatusEnum.Ok)
                                    break;
                                else
                                    goto case InvoiceStageEnum.QRCodeProcessed;

                            // Распознование текста
                            case InvoiceStageEnum.QRCodeProcessed:
                                workingInvoice = await RecognizeTextAsync(workingInvoice, image, scope, hubContext);
                                if (workingInvoice.StageStatus != InvoiceStatusEnum.Ok)
                                    break;
                                else
                                    goto case InvoiceStageEnum.TextProcessed;

                            // Маппинг продуктов
                            case InvoiceStageEnum.TextProcessed:
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
                }
                finally
                {
                    // Release the lock after processing
                    await processingLockService.ReleaseLockAsync(invoiceId);
                }
            }
        }
        private async Task<InvoiceDTO> ProcessQRCodeAsync(InvoiceDTO invoice, Bitmap image, IServiceScope scope, IHubContext<NotificationHub> hubContext)
        {
            var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
            var consumerService = scope.ServiceProvider.GetRequiredService<IConsumerService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            invoice.Stage = InvoiceStageEnum.QRCodeProcessed;
            invoice.StageStatus = InvoiceStatusEnum.Ok;
            
            var (basicQRCode, basicProcessedImage) = await imageProcessor.BasicQRRecognitionAsync(image, invoice.FilePath);

            if (basicQRCode == null)
            {
                var (deepQRCode, deepProcessedImage) = await imageProcessor.DeepQRRecognitionAsync(image, invoice.FilePath);
                if (deepQRCode == null)
                    throw new Exception("Failed to recognize QR-code.");

                basicQRCode = deepQRCode;
            }

            var consumer = await consumerService.GetConsumerDTOByIdAsync((int)invoice.Consumer.Id);
            if (basicQRCode.CustomerTaxNumber != consumer.TaxNumber)
                throw new Exception("ConsumerId not match");

            InvoiceHelper.UpdateInvoiceByQR(invoice, basicQRCode, invoice.FilePath);

            // Валидация накладной после QR
            var validatorQR = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.QRCodeProcessed);
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

        private async Task<InvoiceDTO> RecognizeTextAsync(InvoiceDTO invoice, Bitmap image, IServiceScope scope, IHubContext<NotificationHub> hubContext)
        {
            var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
            var rmsMeasureUnitsService = scope.ServiceProvider.GetRequiredService<IRMSMeasureUnitService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            invoice.Stage = InvoiceStageEnum.TextProcessed;
            invoice.StageStatus = InvoiceStatusEnum.Ok;
            
            var measUnits = await rmsMeasureUnitsService.GetUnitsByConsumerIdAsync((int)invoice.Consumer.Id);
            if (measUnits == null)
                throw new Exception("Failed to get measure units.");


            invoice = await imageProcessor.DeepTextRecognitionAsync(image, null, invoice, measUnits);
            if (invoice.StageStatus != InvoiceStatusEnum.Ok)
                throw new Exception("Failed to recognize text.");

            var validatorText = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.TextProcessed);
            validatorText.Validate(invoice, invoice.Consumer.TaxNumber);

            await invoiceService.SaveInvoiceAsync(invoice);
            invoice = await invoiceService.GetInvoiceDTOByIdAsync((int)invoice.Id);

            if (invoice.StageStatus == InvoiceStatusEnum.Ok)
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognized successfully.");
            else
                await hubContext.Clients.All.SendAsync("ReceiveMessage", "Invoice text recognized with errors.");
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

            invoice.Stage = InvoiceStageEnum.ProductsMapped;
            invoice.StageStatus = InvoiceStatusEnum.Ok;

            var rmsProducts = await rmsProductService.GetProductsByConsumerIdAsync((int)invoice.Consumer.Id);
            var measureUnits = await measureService.GetUnitsByConsumerIdAsync((int)invoice.Consumer.Id);
            var storages = await rmsAccountService.GetAccountsByConsumerIdAsync((int)invoice.Consumer.Id);
            var conParam = await conParamService.GetConnectionParameterDTOByCustomerIdAsync((int)invoice.Consumer.Id);

            var mappingResult = await invoiceProcessor.MappingToRMSProductsAsync(invoice, rmsProducts, conParam, measureUnits, storages);
            var mappedInvoice = mappingResult.Item1;
            var newRmsProducts = mappingResult.Item2;

            if (mappedInvoice == null)
                throw new Exception("Failed to map products.");

            foreach (var newRMSProduct in newRmsProducts)
            {
                newRMSProduct.Id = await rmsProductService.SaveProductAsync(newRMSProduct);
                if (newRMSProduct.Id == null)
                    throw new Exception("Failed to save product.");

                var rmsToSaveId = mappedInvoice.Products
                    .Where(p => p.RMSProduct?.Name == newRMSProduct.Name && p.RMSProduct?.Id == null)
                    .FirstOrDefault();
                if (rmsToSaveId != null)
                    rmsToSaveId.Id = (int)newRMSProduct.Id;

            }

            InvoiceHelper.CopyInvoice(invoice, mappedInvoice);

            var validatorMapp = InvoiceValidatorBase.CreateValidator(InvoiceStageEnum.ProductsMapped);
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