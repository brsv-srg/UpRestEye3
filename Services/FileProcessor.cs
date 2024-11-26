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
                var binaryFilter = new Threshold(100);

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
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode(string imagePath)
        {
            QRCodeData? qrCodeData = null;
            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.5, 2.0, 3.0 }; // Для CLAHE
            int[] blurKernels = { 3, 5, 7 };         // Для медианного фильтра
            double[] sharpWeights = { 1.2, 1.5, 2.0 }; // Для усиления резкости

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
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", Path.GetFileName(imagePath));
                            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                            bitmap.Save(processedFilePath, ImageFormat.Png);
                            return qrCodeData;
                        }
                        else
                        {
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "not_processed_in_step1", Path.GetFileName(imagePath));
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
        

        public bool IsMatchingATQRCode(string input)
        {
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";
            return Regex.IsMatch(input, pattern);
        }
    }
}
