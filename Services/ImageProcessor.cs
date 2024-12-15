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


namespace UpRestEye3.Services
{
    
  

    public interface IImageProcessor
    {
        Task<QRCodeData> ProcessImageAsync(string imagePath);

    }

    // Класс обработки изображения
    public class ImageProcessor: IImageProcessor
    {
        private readonly ILocalMLService _predictor;

        public ImageProcessor(ILocalMLService predictor)
        {
            _predictor = predictor;
        }

        public async Task<QRCodeData> ProcessImageAsync(string imagePath)
        {
            // Шаг 0. Загрузка изображения
            var image = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Шаг 1. Создание дайджеста изображения
            var digest = GenerateImageDigest(image);
/*
            // Шаг 2. Попытка распознать "в лоб"
            if (TryDecodeQRCode(image, out QRCodeData qrCodeData))
            {
                _predictor.UpdateModel(digest, new ImageProcessingParameters()); // Обучение без параметров
                return qrCodeData;
            }
            
            // Шаг 3. Получение предсказания параметров обработки,
            // обработка и распознование согласно предсказанию
            var parameters = _predictor.Predict(digest);

            if (parameters != null && parameters.Any())
            {
                image = ApplyImageProcessing(image, imagePath, parameters);
                if (TryDecodeQRCode(image, out  qrCodeData))
                {
                    _predictor.UpdateModel(digest, parameters); // Обучение с параметрами
                    return qrCodeData;
                }
            }


            // Шаг 4. Обработка и распознование через подбор параметров 

            if(TryImageProcessingAndDecodeQrCode(image, imagePath, out parameters, out  qrCodeData))
            {
                _predictor.UpdateModel(digest, parameters); // Обучение
                return qrCodeData;
            }
            */

            // Шаг 6. Обращение к внешней модели

            var qpCodeData = await TryExternalImageProcessingAndDecodeAsync(image, imagePath);
            if (qpCodeData != null)
            {
                return qpCodeData;
            }

            return new QRCodeData();
        }

        private ImageDigest GenerateImageDigest(Mat image)
        {
            var brightness = Cv2.Mean(image).Val0;

            Cv2.MeanStdDev(image, out _, out var stdDev);
            var contrast = stdDev.Val0;

            Mat laplacian = new Mat();
            Cv2.Laplacian(image, laplacian, MatType.CV_64F);
            Scalar mu, sigma;
            Cv2.MeanStdDev(laplacian, out mu, out sigma);
            double sharpness = sigma.Val0 * sigma.Val0;

            var blurredImage = new Mat();
            Cv2.GaussianBlur(image, blurredImage, new OpenCvSharp.Size(5, 5), 0);
            var noiseLevel = Cv2.Mean(blurredImage).Val0;

            int width = image.Width;
            int height = image.Height;
            double aspectRatio = (double)width / height;

            return new ImageDigest
            {
                brightness = (float)brightness,
                contrast = (float)contrast,
                noiseLevel = (float)noiseLevel,
                sharpness = (float)sharpness,
                aspectRatio = (float)aspectRatio
            };
        }


        private bool TryDecodeQRCode(Mat image, out QRCodeData qrCodeData)
        {
            qrCodeData = TryDecodeQRByOpenCV(image);
            if (qrCodeData != null)
                return true;

            qrCodeData = TryDecodeQRByZXin(image);
            if (qrCodeData != null)
                return true;
            else
                return false;
        }

        private QRCodeData TryDecodeQRByOpenCV(Mat image)
        {
            var detector = new QRCodeDetector();
            
            bool isDetected = detector.DetectMulti(image, out Point2f[] points);
            if (!isDetected)
                return null;
            
            isDetected = detector.DecodeMulti(image, points, out string?[] results);
            if (isDetected)
            {
                // Успешно распознаны
                foreach (var result in results)
                {
                    if (result != null && IsMatchingATQRCode(result))
                    {
                        // Успешно распознан QR-код с нужной структурой
                        return new QRCodeData(result);
                    }
                }
            }
            return null;
        }

        [SupportedOSPlatform("windows")]
        private QRCodeData TryDecodeQRByZXin(Mat image)
        {
            // Распознавание ZXing
            // Настройка 
            var barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            };

            // Конвертация в Bitmap для ZXing
            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);

            // Попытка распознать 
            var results = barcodeReader.DecodeMultiple(bitmap);
            if (results != null && results.Length > 0)
            {
                // Успешно распознаны
                foreach (var result in results)
                {
                    if (result != null && IsMatchingATQRCode(result.Text))
                    {
                        // Успешно распознан QR-код с нужной структурой
                        return new QRCodeData(result.Text);
                    }
                }
            }
            return null;
        }



        private bool TryImageProcessingAndDecodeQrCode(Mat image, string imagePath, out ImageProcessingParameters parameters, out QRCodeData qrCodeData)
        {
            parameters = new ImageProcessingParameters();
            qrCodeData = new QRCodeData();  
            // Список параметров для итераций
            double[] medianBlurKernels = { 3, 5, 7 };                             // Для медианного фильтра - удаление шумов
            double[] convScaleContrasts = { 1.7, 1.2, 1.5, 2.0 };                 // Уровень контраста
            double[] convScaleBrightnesses = { 20.0, -10.0, 10.0, -20.0, 0.0 };   // Уровень яркости
            double[] adThreshBlocks = { 100, 127, 150 };                          // Для  бинаризации
            //double[] adThreshBlocks = { 0, 3, 7, 11 };                          // Для адаптивной бинаризации
            double[,] cannyThresholds = new double[,] { { 0.0, 0.0 } }; // { 80, 160 }, { 50, 150 }, { 10, 100 }, { 100, 200 } }; // Массив пар значений для обнаружения линий
            double[,] sharpWeights = new double[,] { { 0.0, 0.0 } }; // { 1.5, -0.5 }, { 0.7, 0.3 } }; // Массив пар значений для веса резкозти

           
            // Подбор параметров во вложенных циклах
            foreach (var medianBlurKernel in medianBlurKernels)
            {
                parameters.Clear();
                parameters.medianBlurKernel = medianBlurKernel;
                var processedImage = ApplyImageProcessing(image, imagePath, parameters);
                if (TryDecodeQRCode(processedImage, out qrCodeData))
                {
                    return true;
                }
                
                foreach (var convScaleContrast in convScaleContrasts)
                {
                    parameters.Clear();
                    parameters.medianBlurKernel = medianBlurKernel;
                    parameters.convScaleContrast = convScaleContrast;
                    processedImage = ApplyImageProcessing(image, imagePath, parameters);
                    if (TryDecodeQRCode(processedImage, out qrCodeData))
                    {
                        return true;
                    }
                    foreach (var convScaleBrightness in convScaleBrightnesses)
                    {
                        parameters.Clear();
                        parameters.medianBlurKernel = medianBlurKernel;
                        parameters.convScaleContrast = convScaleContrast;
                        parameters.convScaleBrightness = convScaleBrightness;
                        processedImage = ApplyImageProcessing(image, imagePath, parameters);
                        if (TryDecodeQRCode(processedImage, out qrCodeData))
                        {
                            return true;
                        }
                        foreach (var adThreshBlock in adThreshBlocks)
                        {
                            parameters.Clear();
                            parameters.medianBlurKernel = medianBlurKernel;
                            parameters.convScaleContrast = convScaleContrast;
                            parameters.convScaleBrightness = convScaleBrightness;
                            parameters.adThreshBlock = adThreshBlock;
                            processedImage = ApplyImageProcessing(image, imagePath, parameters);
                            if (TryDecodeQRCode(processedImage, out qrCodeData))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private async Task<QRCodeData> TryExternalImageProcessingAndDecodeAsync(Mat image, string imagePath)
        {
            var parameters = new ImageProcessingParameters();
            parameters.medianBlurKernel = 1;
            parameters.convScaleContrast = 1.2;
            parameters.convScaleBrightness = 10;
            parameters.adThreshBlock = 0;
            parameters.cannyThreshold1 = 0;
            parameters.cannyThreshold2 = 0;
            parameters.sharpWeightA = 0;
            parameters.sharpWeightB = 0;

            image = ApplyImageProcessing(image, imagePath, parameters);

            // Convert OpenCvSharp.Mat to Google.Cloud.Vision.V1.Image
            byte[] imageBytes = image.ToBytes();
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);

            

            var clientIA = await ImageAnnotatorClient.CreateAsync();
            TextAnnotation text = clientIA.DetectDocumentText(googleImage);
            Console.WriteLine($"Text: {text.Text}");
            foreach (Page page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    string box = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                    Console.WriteLine($"Block {block.BlockType} at {box}");
                    foreach (var paragraph in block.Paragraphs)
                    {
                        box = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        Console.WriteLine($"  Paragraph at {box}");
                        foreach (var word in paragraph.Words)
                        {
                            Console.WriteLine($"    Word: {string.Join("", word.Symbols.Select(s => s.Text))}");
                        }
                    }
                }
            }




            return new QRCodeData();
        }


        private Mat ApplyImageProcessing(Mat image, string imagePath, ImageProcessingParameters parameters)
        {
            Mat resultImage = image;
            // 2. Преобразование в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(resultImage, gray, ColorConversionCodes.BGR2GRAY); //+
            resultImage = gray;

            // 3. Удаление шума
            if(parameters.medianBlurKernel > 0)
            {
                Mat denoised = new Mat();
                Cv2.MedianBlur(resultImage, denoised, (int)parameters.medianBlurKernel); //+ 
                resultImage = denoised;

                {// запись
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp1-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}{Path.GetExtension(imagePath)}");


                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }

            // 4. Усиление контраста
            if (parameters.convScaleContrast != 0.0 || parameters.convScaleBrightness != 0.0)
            {
                Mat enhanced = new Mat();
                Cv2.ConvertScaleAbs(resultImage, enhanced, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость
                resultImage = enhanced;


                {// запись
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp2-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}{Path.GetExtension(imagePath)}");


                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }


            // 5. Бинаризация
            if (parameters.adThreshBlock > 0)
            {
                Mat binary = new Mat();
                Cv2.Threshold(resultImage, binary, parameters.adThreshBlock, 255, ThresholdTypes.Otsu);
                //Cv2.AdaptiveThreshold(resultImage, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, (int)parameters.adThreshBlock, 2);
                resultImage = binary;


                {// запись
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp3-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}-{parameters.adThreshBlock}{Path.GetExtension(imagePath)}");

                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }

            //
            return resultImage;






            //// 5. Бинаризация
            //Mat binary = new Mat();
            //Cv2.AdaptiveThreshold(enhanced, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, (int)parameters.adThreshBlock, 2);
            //{// запись
            //    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary);
            //    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp3-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}-{parameters.adThreshBlock}{Path.GetExtension(imagePath)}");


            //    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            //    bitmap.Save(processedFilePath, ImageFormat.Png);
            //}


            //// 6. Выделение границ
            //Mat edgesCanny = new Mat();
            //Cv2.Canny(binary, edgesCanny, parameters.cannyThreshold1, parameters.cannyThreshold2);
            //{// запись
            //    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(edgesCanny);
            //    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp4-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}-{parameters.adThreshBlock}-{parameters.cannyThreshold1}-{parameters.cannyThreshold2}{Path.GetExtension(imagePath)}");


            //    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            //    bitmap.Save(processedFilePath, ImageFormat.Png);
            //}

            //Mat resImage = new Mat();
            //Cv2.AddWeighted(binary, parameters.sharpWeightA, edgesCanny, parameters.sharpWeightB, 0, resImage);

            //{// запись
            //    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resImage);
            //    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp5-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}-{parameters.adThreshBlock}-{parameters.cannyThreshold1}-{parameters.cannyThreshold2}-{parameters.sharpWeightA}-{parameters.sharpWeightB}{Path.GetExtension(imagePath)}");


            //    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            //    bitmap.Save(processedFilePath, ImageFormat.Png);
            //}
            //return resImage;
        }

        private bool IsMatchingATQRCode(string input)
        {
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";
            return Regex.IsMatch(input, pattern);
        }
    }
}