using UpRestEye3.Models;
using System.Drawing;



namespace UpRestEye3.Services
{
    
  

    public interface IInvoiceFileService
    {
        Task<bool> FileProcessAsync(string imagePath);
    }

    // Класс обработки изображения
    public class FileService: IInvoiceFileService
    {
        private readonly IImageFileProcessor _imageProcessor;
        private readonly IInvoiceService _invoiceService;

        public FileService(IInvoiceService invoiceService, IImageFileProcessor imageProcessor)
        {
            _invoiceService = invoiceService;
            _imageProcessor = imageProcessor;
        }


// TODO сделать в отдельном потоке
        public async Task<bool> FileProcessAsync(string filePath)
        {
            try
            {
                // Загрузка изображения
                var image = new Bitmap(filePath);
                if (image == null)
                    throw new Exception("Failed to load image.");

                // Сохранение пустой накладной в базу данных
                var newInvoice = new Invoice();
                InvoiceHelper.UpdateInvoice(newInvoice, null, filePath);
                await _invoiceService.SaveInvoiceAsync(newInvoice);
                
                
                // Распознование QR-кода "в лоб" и с помощью предсказания
                var (basicQRCode, basicProcessedImage) = await _imageProcessor.BasicQRRecognitionAsync(image, filePath);

                
                if (basicQRCode != null)
                {
                    // Если QR-код распознан, сохраняем изменения и идем на распознавание текста

                    // Внесение изменений в существующую накладную и схранение изменений в базу данных
                    InvoiceHelper.UpdateInvoice(newInvoice, basicQRCode, filePath);
                    await _invoiceService.SaveInvoiceAsync(newInvoice);

                    // Если есть улучшенное изображение берем его
                    if (basicProcessedImage != null)
                    {
                        image.Dispose();
                        image = basicProcessedImage;
                    }
                }
                else
                {
                    // Если QR-код в лоб и с предсказанием не распознан, идем на распознавание через подбор вариантов
                    var (deepQRCode, deepProcessedImage) = await _imageProcessor.DeepQRRecognitionAsync(image, filePath);
                    if (deepQRCode != null)
                    {
                        // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                        InvoiceHelper.UpdateInvoice(newInvoice, deepQRCode, filePath);

                        await _invoiceService.SaveInvoiceAsync(newInvoice);

                        // Если есть улучшенное изображение берем его
                        if (deepProcessedImage != null)
                        {
                            image.Dispose();
                            image = deepProcessedImage;
                        }
                    }
                }

                // Распознование текста и формирование полной накладной  
                var fullInvoice = await _imageProcessor.DeepTextRecognitionAsync(image, newInvoice);
                // Если текст распознан, то сохраняем полный документ
                if (fullInvoice != null)
                {
                    // Внесение изменений в существующую накладную и сохранение изменений в базу данных
                    InvoiceHelper.UpdateInvoice(newInvoice, fullInvoice);

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