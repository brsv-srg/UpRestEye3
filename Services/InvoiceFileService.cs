using UpRestEye3.Models;
using System.Drawing;



namespace UpRestEye3.Services
{
    
  

    public interface IInvoiceFileService
    {
        Task<bool> FileProcessAsync(string imagePath);
    }

    // Класс обработки изображения
    public class InvoiceFileService: IInvoiceFileService
    {
        private readonly IImageFileProcessor _imageProcessor;
        private readonly IInvoiceService _invoiceService;

        public InvoiceFileService(IInvoiceService invoiceService, IImageFileProcessor imageProcessor)
        {
            _invoiceService = invoiceService;
            _imageProcessor = imageProcessor;
        }

        public async Task<bool> FileProcessAsync(string filePath)
        {
            try
            {
                // Шаг 0. Загрузка изображения
                var image = new Bitmap(filePath);
                if (image == null)
                    throw new Exception("Failed to load image.");

                // Шаг 1. Распознование QR-кода "в лоб" и с помощью предсказания
                var (qrCode, basicProcessedImage) = await _imageProcessor.BasicQRRecognitionAsync(image, filePath);
                if (basicProcessedImage != null)
                { 
                    image.Dispose();
                    image = basicProcessedImage;
                }

                // Шаг 2. Сохранение предварительной накладной в базу данных
                var basicInvoice = new Invoice(qrCode, filePath);
                await _invoiceService.SaveInvoiceAsync(basicInvoice);

                // TODO сделать в отдельном потоке

                // Шаг 3. Распознавание QR-кода через подбор вариантов преобразований и обучение модели
                var (deepQRCode, deepProcessedImage) = await _imageProcessor.DeepQRRecognitionAsync(image, filePath);
                if (deepQRCode != null)
                {
                    // Внесение изменений в существующую накладную
                    basicInvoice.Update(deepQRCode, filePath);
                    // Сохранение изменений в базу данных
                    await _invoiceService.SaveInvoiceAsync(basicInvoice);
                }
                if (deepProcessedImage != null)
                {
                    image.Dispose();
                    image = deepProcessedImage;
                }

                // Шаг 3. Распознование текста и формирование полной накладной  
                var fullInvoice = await _imageProcessor.DeepTextRecognitionAsync(image, filePath);
                // Обработка результатов выполнения
                if (fullInvoice != null)
                {
                    // Внесение изменений в существующую накладную
                    basicInvoice.Update(fullInvoice);
                    // СОхранение изменений в базу данных
                    await _invoiceService.SaveInvoiceAsync(fullInvoice);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        } 
    }
}