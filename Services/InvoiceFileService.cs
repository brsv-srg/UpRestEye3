using Microsoft.ML;
using Newtonsoft.Json;
using OpenCvSharp;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using System;
using System.IO;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCvSharp;
using Microsoft.ML;
using Google.Protobuf.WellKnownTypes;
using ImageMagick;
using System.Text.RegularExpressions;
using UpRestEye3.MLImageModels;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models;
using System.Drawing.Imaging;
using System.Drawing;
using SkiaSharp;
using static UpRestEye3.Services.LocalMLService;
using AForge.Imaging.Filters;
using System.Runtime.Versioning;
using Google.Cloud.Vision.V1;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;
using Protobuf.Text;



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
                var qrCode = await _imageProcessor.BasicQRRecognitionAsync(image, filePath);


                // Шаг 2. Сохранение предварительной накладной в базу данных
                var basicInvoice = new Invoice(qrCode, filePath);
                await _invoiceService.SaveInvoiceAsync(basicInvoice);

                // Шаг 3. Распознование дополнительной информации в отдельном потоке (TODO)
                var fullInvoice = await _imageProcessor.DeepImageProcessAsync(image, filePath);
                // Обработка результатов выполнения
                if (fullInvoice != null)
                {
                    // Дополнительная обработка extInvoice
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