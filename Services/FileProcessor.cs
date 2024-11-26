using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UpRestEye3.Models;
using System.Runtime.Versioning;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Http;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;
using ZXing.QrCode.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using ZXing.Common;
using System.Text.RegularExpressions;
using System.Reflection.PortableExecutable;
using OpenCvSharp.XImgProc;
using ImageMagick;


namespace UpRestEye3.Services
{ 
public class FileProcessor
    {
        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> ProcessFileAsync(string filePath)
        {
            string processedFilePath = await PreprocessImageByAForgeAsync(filePath);
            var qrCodeData = await RecognizeQRCodeAsync(processedFilePath);
            if (qrCodeData == null)
            {
                processedFilePath = await PreprocessImageAlternativeAsync(filePath);
                qrCodeData = await RecognizeQRCodeAsync(processedFilePath);
                if (qrCodeData == null)
                {
                    await SaveUnrecognizedFileAsync(processedFilePath);
                }
            }
            return qrCodeData;
        }

        [SupportedOSPlatform("windows")]
        private async Task<string> PreprocessImageByAForgeAsync(string filePath)
        {
            using (var originalBitmap = new Bitmap(filePath))
            {
                var grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                var contrastFilter = new ContrastCorrection(50);
                var binaryFilter = new AForge.Imaging.Filters.Threshold(100);

                Bitmap preprocessedBitmap = binaryFilter.Apply(contrastFilter.Apply(grayFilter.Apply(originalBitmap)));

                string processedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "processed", Path.GetFileName(filePath));
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                preprocessedBitmap.Save(processedFilePath, ImageFormat.Png);

                return processedFilePath;
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task<string> PreprocessImageAlternativeAsync(string filePath)
        {
            // Чтение изображения
            Mat src = Cv2.ImRead(filePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // Улучшение контрастности с помощью CLAHE
            var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            Mat enhanced = new Mat();
            clahe.Apply(gray, enhanced);

            // Устранение шумов с медианным фильтром
            Mat denoised = new Mat();
            Cv2.MedianBlur(enhanced, denoised, 3);

            // Усиление краев с помощью фильтра Лапласа
            Mat edges = new Mat();
            Cv2.Laplacian(denoised, edges, MatType.CV_8U);

            // Легкое увеличение четкости через наложение краев
            Mat sharpened = new Mat();
            Cv2.AddWeighted(denoised, 1.5, edges, -0.5, 0, sharpened);

            using var preprocessedBitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);

            string processedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "processed_alternative", Path.GetFileName(filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            preprocessedBitmap.Save(processedFilePath, ImageFormat.Png);

            return processedFilePath;
        }

        [SupportedOSPlatform("windows")]
        private async Task<QRCodeData> RecognizeQRCodeAsync(string filePath)
        {
            using (var preprocessedBitmap = new Bitmap(filePath))
            {
                var reader = new BarcodeReader<Bitmap>(bitmap => new BitmapLuminanceSource(preprocessedBitmap));

                reader.Options.PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE };
                reader.AutoRotate = true;
                reader.Options.TryHarder = true;
                reader.Options.PureBarcode = false;

                var results = reader.DecodeMultiple(preprocessedBitmap);

                if (results != null && results.Length > 0)
                {
                    var qrInvoiceData = new QRCodeData(results.Last().Text);
                    return qrInvoiceData;
                }
            }
            return null;
        }

        [SupportedOSPlatform("windows")]
        private static async Task SaveUnrecognizedFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                string? directoryName = Path.GetDirectoryName(filePath);
                if (directoryName != null)
                {
                    string unrecognizedDir = Path.Combine(directoryName, "unrecognized");
                    Directory.CreateDirectory(unrecognizedDir);
                    string destinationPath = Path.Combine(unrecognizedDir, Path.GetFileName(filePath));
                    File.Move(filePath, destinationPath, true);
                }
            });
        }

        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode1(string imagePath)
        {
            QRCodeData? qrCodeData = null;
            
            // Удаление директории 
            string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed");
            Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.1, 1.2, 1.5, 2.0, 2.5, 3.0 }; // Для CLAHE
            int[] blurKernels = {1, 3 };         // Для медианного фильтра
            double[] sharpWeights = { 1.1, 1.2, 1.3, 1.5, 1.7, 2.0 }; // Для усиления резкости

            //double[] clipLimits = { 1.5, 2.0, 3.0 }; // Для CLAHE
            //int[] blurKernels = { 3, 5, 7 };         // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 }; // Для усиления резкости

            // Настройка ZXing
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

            // Итеративная обработка
            foreach (var clipLimit in clipLimits)
            {
                // Применение CLAHE
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);

                foreach (var kernel in blurKernels)
                {
                    // Устранение шумов
                    Mat denoised = new Mat();
                    Cv2.MedianBlur(enhanced, denoised, kernel);

                    foreach (var sharpWeight in sharpWeights)
                    {
                        // Усиление краев через наложение
                        Mat edges = new Mat();
                        Cv2.Laplacian(denoised, edges, MatType.CV_8U);

                        Mat sharpened = new Mat();
                        Cv2.AddWeighted(denoised, sharpWeight, edges, -0.5, 0, sharpened);

                        // Конвертация в Bitmap для ZXing
                        using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);

                        //using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);
                        // Попытка распознать QR-коды
                        var results = barcodeReader.DecodeMultiple(bitmap);
                        if (results != null && results.Length > 0)
                        {
                            // Успешно распознано
                            Console.WriteLine("QR codes have been found:");

                            foreach (var result in results)
                            {
                                Console.WriteLine($"Contents: {result.Text}");
                                if (IsMatchingATQRCode(result.Text))
                                {
                                    qrCodeData = new QRCodeData(result.Text);
                                }
                            }

                            // сохранить все в директорию в распознанными файлами
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
                            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                            bitmap.Save(processedFilePath, ImageFormat.Png);
                            return qrCodeData;
                        }
                        else
                        {
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
                            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                            bitmap.Save(processedFilePath, ImageFormat.Png);
                        }
                    }
                }
            }
            // Если не удалось распознать
            Console.WriteLine("Failed to find QR codes after all attempts.");
            return null;
        }

        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode2(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Удаление директории 
            string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed");
            Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.1, 1.2, 1.5, 2.0, 2.5, 3.0 };     // Для CLAHE
            int[] kernels = { 1, 3 };                                   // Для медианного фильтра
            double[] sharpWeights = { 1.1, 1.2, 1.3, 1.5, 1.7, 2.0 };   // Для усиления резкости
            int[] thrBlocks = { 9, 11, 13 };                           // Для адаптивной бинаризации

            //double[] clipLimits = { 1.0, 2.0, 3.0 };                  // Для CLAHE
            //int[] kernels = { 3, 5, 7 };                              // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 };                // Для усиления резкости
            //int[] thrBlocks = { 11, 15, 19 };                         // Для адаптивной бинаризации


            // Настройка ZXing
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

            // Итеративный перебор параметров
            foreach (var clipLimit in clipLimits)
            {
                // Применение CLAHE для улучшения контраста
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);

                foreach (var kernel in kernels)
                {
                    // Устранение шума медианным фильтром
                    Mat denoised = new Mat();
                    Cv2.MedianBlur(enhanced, denoised, kernel);

                    foreach (var blockSize in thrBlocks)
                    {
                        // Адаптивная бинаризация
                        Mat binary = new Mat();
                        Cv2.AdaptiveThreshold(denoised, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blockSize, 2);

                        foreach (var sharpWeight in sharpWeights)
                        {
                            // Усиление резкости через Unsharp Masking
                            Mat edges = new Mat();
                            Cv2.GaussianBlur(binary, edges, new OpenCvSharp.Size(9, 9), 0);

                            Mat sharpened = new Mat();
                            Cv2.AddWeighted(binary, sharpWeight, edges, -0.5, 0, sharpened);

                            // Конвертация изображения в Bitmap для ZXing
                            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);

                            // Попытка распознать QR-коды
                            var results = barcodeReader.DecodeMultiple(bitmap);
                            if (results != null && results.Length > 0)
                            {
                                // Успешно распознано
                                Console.WriteLine("QR codes have been found:");

                                foreach (var result in results)
                                {
                                    Console.WriteLine($"Contents: {result.Text}");
                                    if (IsMatchingATQRCode(result.Text))
                                    {
                                        qrCodeData = new QRCodeData(result.Text);
                                    }
                                }

                                
                                // сохранить все в директорию в распознанными файлами
                                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}{Path.GetExtension(imagePath)}");
                                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                bitmap.Save(processedFilePath, ImageFormat.Png);
                                return qrCodeData;
                            }
                            else
                            {
                                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}{Path.GetExtension(imagePath)}");
                                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                bitmap.Save(processedFilePath, ImageFormat.Png);
                            }
                        }
                    }
                }
            }



            // Если не удалось распознать
            Console.WriteLine("Failed to find QR codes after all attempts.");
            return null;
        }





        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode3(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Удаление директории 
            string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed");
            Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.1, 1.2, 1.5, 2.0, 2.5, 3.0 };     // Для CLAHE
            int[] kernels = { 1, 3 };                                   // Для медианного фильтра
            double[] sharpWeights = { 1.1, 1.2, 1.3, 1.5, 1.7, 2.0 };   // Для усиления резкости
            int[] thrBlocks = { 9, 11, 13 };                           // Для адаптивной бинаризации
            int[] houghThresholds = { 30, 50, 70 };        // Для порога Хафа
            OpenCvSharp.Size[] blurKernels = { new OpenCvSharp.Size(3, 3), new OpenCvSharp.Size(5, 5), new OpenCvSharp.Size(7, 7), new OpenCvSharp.Size(9, 9) };


            // Списки параметров для динамического перебора
            //double[] clipLimits = { 1.0, 2.0, 3.0 };       // Для CLAHE
            //int[] blurKernels = { 3, 5, 7 };               // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 };     // Для усиления резкости
            //int[] adaptiveThresholdBlocks = { 11, 15, 19 }; // Для адаптивной бинаризации
            //int[] houghThresholds = { 30, 50, 70 };        // Для порога Хафа
            //Size[] blurKernels = { new Size(3, 3), new Size(5, 5), new Size(7, 7), new Size(9, 9) };



            // Настройка ZXing
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


            // Итеративный перебор параметров
            foreach (var clipLimit in clipLimits)
            {
                // Применение CLAHE для улучшения контраста
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);

                foreach (var kernel in kernels)
                {
                    // Устранение шума медианным фильтром
                    Mat denoised = new Mat();
                    Cv2.MedianBlur(enhanced, denoised, kernel);

                    foreach (var blockSize in thrBlocks)
                    {
                        // Адаптивная бинаризация
                        Mat binary = new Mat();
                        Cv2.AdaptiveThreshold(denoised, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blockSize, 2);

                        foreach (var houghThreshold in houghThresholds)
                        {
                            // Преобразование Хафа для обнаружения линий
                            Mat edges = new Mat();
                            Cv2.Canny(binary, edges, 50, 150);

                            LineSegmentPoint[] lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, houghThreshold, minLineLength: 50, maxLineGap: 10);

                            // Наложение найденных линий на изображение
                            foreach (var line in lines)
                            {
                                Cv2.Line(binary, line.P1, line.P2, Scalar.White, 2); // Усиливаем линии
                            }

                            foreach (var sharpWeight in sharpWeights)
                            {
                                foreach (var blurKernel in blurKernels)
                                {
                                    // Усиление резкости через Unsharp Masking
                                    Mat blurred = new Mat();
                                    Cv2.GaussianBlur(binary, blurred, blurKernel, 0);
                                    Mat sharpened = new Mat();
                                    Cv2.AddWeighted(binary, sharpWeight, blurred, -0.5, 0, sharpened);

                                    // Конвертация изображения в Bitmap для ZXing
                                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);



                                    // Попытка распознать QR-коды
                                    var results = barcodeReader.DecodeMultiple(bitmap);
                                    if (results != null && results.Length > 0)
                                    {
                                        // Успешно распознано
                                        Console.WriteLine("QR codes have been found:");

                                        foreach (var result in results)
                                        {
                                            Console.WriteLine($"Contents: {result.Text}");
                                            if (IsMatchingATQRCode(result.Text))
                                            {
                                                qrCodeData = new QRCodeData(result.Text);
                                            }
                                        }


                                        // сохранить все в директорию в распознанными файлами
                                        string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}-houghThreshold{houghThreshold}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
                                        Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                        bitmap.Save(processedFilePath, ImageFormat.Png);
                                        return qrCodeData;
                                    }
                                    else
                                    {
                                        string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}-houghThreshold{houghThreshold}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
                                        Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                        bitmap.Save(processedFilePath, ImageFormat.Png);
                                    }
                                }
                            }
                        }
                    }
                }
            }



            // Если не удалось распознать
            Console.WriteLine("Failed to find QR codes after all attempts.");
            return null;
        }
        public bool IsMatchingATQRCode(string input)
        {
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";
            return Regex.IsMatch(input, pattern);
        }
    }
}
