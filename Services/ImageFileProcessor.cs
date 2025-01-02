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
using Google.Api;
using UpRestEye3.Components.Pages;



namespace UpRestEye3.Services
{
    
  

    public interface IImageFileProcessor
    {
        Task<QRCodeData?> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<QRCodeData?> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<Invoice> DeepTextRecognitionAsync(Bitmap sourceImage, string imagePath);

    }

    // Класс обработки изображения
    public class ImageFileProcessor: IImageFileProcessor
    {
        private readonly ILocalMLService _predictor;
        private readonly IGPTService _gptParser;
        private readonly IQRProcessing _qrProcessor;
        private readonly IEnumerable<IQRRecognition> _qrRecognizers;
        private readonly ITextRecognition _textRecognizer;

        public ImageFileProcessor(IQRProcessing qrProcessor, ILocalMLService predictor, IGPTService gptParser, IEnumerable<IQRRecognition> qrRecognizers, ITextRecognition textRecognizer)
        {
            _predictor = predictor;
            _qrProcessor = qrProcessor;
            _qrRecognizers = qrRecognizers;
            _textRecognizer = textRecognizer;
            _gptParser = gptParser;
        }

        public async Task<QRCodeData?> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
            // Шаг 0. Засерение изображения 
            var grayImage = await _qrProcessor.GrayScale(sourceImage);

            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(grayImage);

            // Шаг 2. Попытка распознать "в лоб"
            if (TryDecodeQRCode(grayImage, out QRCodeData? qrCodeData))
            {
                _predictor.UpdateModel(digest, new ImageProcessingParameters()); // Обучение без параметров
                return qrCodeData;
            }


            // Шаг 3. Получение предсказания параметров обработки,
            // обработка и распознование согласно предсказанию
            var parameters = _predictor.Predict(digest);

            if (parameters == null || !parameters.Any())
                parameters = _predictor.GetProbable(digest);
            
            var processedImage = _qrProcessor.ApplyImageProcessing2(grayImage, imagePath, parameters);
            if (TryDecodeQRCode(processedImage, out qrCodeData))
                 return qrCodeData;
            
            return null;
        }

        public async Task<QRCodeData?> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
            // Шаг 0. Засерение изображения (хотя скорее всего уже серое)
            var grayImage = await _qrProcessor.GrayScale(sourceImage);

            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(grayImage);

            // Шаг 4. Обработка и распознование через подбор параметров 

            if (TryImageProcessingAndDecodeQrCode(grayImage, imagePath, out ImageProcessingParameters parameters, out QRCodeData? qrCodeData))
            {
                _predictor.UpdateModel(digest, parameters); // Обучение
                return qrCodeData;
            }
            return null;
        }

        private bool TryImageProcessingAndDecodeQrCode(Bitmap sourceImage, string imagePath, out ImageProcessingParameters parameters, out QRCodeData? qrCodeData)
        {
            parameters = new ImageProcessingParameters();
            qrCodeData = new QRCodeData();
            var paramsHelper = new ImageProcessingHypotheses();

            // Подбор параметров во вложенных циклах
            foreach (var medianBlurKernel in paramsHelper.medianBlurKernels)
            {
                parameters.medianBlurKernel = medianBlurKernel;
                var processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, imagePath, parameters);
                if (TryDecodeQRCode(processedImage, out qrCodeData))
                    return true;

                foreach (var convScaleContrast in paramsHelper.convScaleContrasts)
                {
                    parameters.convScaleContrast = convScaleContrast;
                    processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, imagePath, parameters);
                    if (TryDecodeQRCode(processedImage, out qrCodeData))
                        return true;

                    foreach (var convScaleBrightness in paramsHelper.convScaleBrightnesses)
                    {
                        parameters.convScaleBrightness = convScaleBrightness;
                        processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, imagePath, parameters);
                        if (TryDecodeQRCode(processedImage, out qrCodeData))
                            return true;

                        foreach (var adThreshBlock in paramsHelper.adThreshBlocks)
                        {
                            parameters.adThreshBlock = adThreshBlock;
                            processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, imagePath, parameters);
                            if (TryDecodeQRCode(processedImage, out qrCodeData))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool TryDecodeQRCode(Bitmap sourceImage, out QRCodeData? qrCodeData)
        {
            qrCodeData = null;
            foreach (var _qrRecognizer in _qrRecognizers)
            {
                var qrCodeDataLocal = _qrRecognizer.DecodeQRCode(sourceImage);
                if (qrCodeDataLocal != null)
                {
                    qrCodeData = qrCodeDataLocal;
                    return true;
                }
            }
            return false;
        }



        public async Task<Invoice> DeepTextRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
            Invoice invoice = new Invoice();
            var parameters = new ImageProcessingParameters();
            parameters.SetMedium();

            // Шаг 0. Засерение изображения (хотя скорее всего уже серое)
            var grayImage = await _qrProcessor.GrayScale(sourceImage);

            // Шаг 1. Создание минимальная обработка изображения
            var processedImage = _qrProcessor.ApplyImageProcessing(grayImage, imagePath, parameters);

            // Шаг 6. Обращение к внешней модели
            var recognizedText = await _textRecognizer.TextRecognize(processedImage);
            if (recognizedText != null)
            {
                return await _gptParser.ParseReceiptWithLLM(recognizedText);
            }
            return new Invoice();
        }



        //    var recognizedDoc = await _textRecognizer.TextRecognize(processedImage); 
            
            
            
        //    var invoiceText = await TryExternalTextRecognition(grayImage, imagePath);
        //    if (invoiceText != null && invoiceText.TextBlocks.Count > 0)
        //    {
        //        return await _gptParser.ParseReceiptWithLLM(invoiceText);
        //    }

        //    return new Invoice();
        //}



        //private bool TryExternalTextRecognition(Bitmap sourceImage, string imagePath, out Invoice? invoice)
        //{
        //    invoice = new Invoice();
        //    var parameters = new ImageProcessingParameters();
        //    parameters.SetMedium();

        //    var processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, imagePath, parameters);

        //    var recognizedDoc = await _textRecognizer.TextRecognize(processedImage);  
            
            
        //    _textRecognizer. 
        //    // Convert OpenCvSharp.Mat to Google.Cloud.Vision.V1.Image
        //    byte[] imageBytes = image.ToBytes();
        //    var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);


        //    var clientIA = await ImageAnnotatorClient.CreateAsync();
        //    TextAnnotation text = clientIA.DetectDocumentText(googleImage);
        //    Console.WriteLine($"Text: {text.Text}");

        //    var jsonObject = new RecognizedDocument();
        //    int blockIndex = 0;


        //    var recognizedDocument = new RecognizedDocument();
        //    int blockNumber = 0;
        //    foreach (Page page in text.Pages)
        //    {
        //        foreach (var block in page.Blocks)
        //        {
        //            var textBlock = new TextBlock
        //            {
        //                BlockNumber = blockNumber++,
        //                BlockCoordinates = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
        //                Paragraphs = new List<TextParagraph>()
        //            };
        //            int paragraphNumber = 0;
        //            foreach (var paragraph in block.Paragraphs)
        //            {
        //                var paragraphText = new StringBuilder();
        //                foreach (var word in paragraph.Words)
        //                {
        //                    paragraphText.Append(string.Join("", word.Symbols.Select(s => s.Text))).Append(" ");
        //                }

        //                textBlock.Paragraphs.Add(new TextParagraph
        //                {
        //                    ParagraphNumber = paragraphNumber++,
        //                    ParagraphCoordinates = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
        //                    ParagraphText = paragraphText.ToString()
        //                });
        //            }

        //            recognizedDocument.TextBlocks.Add(textBlock);
        //        }
        //    }

        //    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(recognizedDocument));

        //    return recognizedDocument;
        //}



    }
}