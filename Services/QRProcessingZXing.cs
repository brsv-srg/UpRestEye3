using System.Drawing;
using System.Runtime.Versioning;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using OpenCvSharp;
using ZXing;
using ZXing.Windows.Compatibility;
using ZXing.Common;
using UpRestEye3.Models;


namespace UpRestEye3.Services
{ 
public class QRProcessingZXing
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

                string processedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "processed0", Path.GetFileName(filePath));
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

            string processedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "processed01", Path.GetFileName(filePath));
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
                    string destinationPath = Path.Combine(directoryName, $"NOTrecognazed{Path.GetFileName(filePath)}");
                    File.Move(filePath, destinationPath, true);
                }
            });
        }

        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode1(string imagePath)
        {
            QRCodeData? qrCodeData = null;
            
            // Удаление директории 
            //string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed1");
            //Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.1, 3.0 }; // Для CLAHE
            int[] Kernels = {5, 7, 9 };         // Для медианного фильтра
            double[] sharpWeights = { 1.5, 2.0 }; // Для усиления резкости

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
                // Применение CLAHE контраст
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);

                foreach (var kernel in Kernels)
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
                                if (QRCodeData.IsMatchingATQRCode(result.Text))
                                {
                                    qrCodeData = new QRCodeData(result.Text);
                                }
                            }

                            // сохранить все в директорию в распознанными файлами
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed1", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
                            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                            bitmap.Save(processedFilePath, ImageFormat.Png);
                            return qrCodeData;
                        }
                        else
                        {
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed1", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
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
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode15(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Удаление директории 
            //string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed1");
            //Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.5, 3.0 }; // Для CLAHE
            int[] Kernels = { 3, 5, 7, 9 };         // Для медианного фильтра
            double[] sharpWeights = { 1.5, 2.0 }; // Для усиления резкости

            //double[] clipLimits = { 1.5, 2.0, 3.0 }; // Для CLAHE
            //int[] blurKernels = { 3, 5, 7 };         // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 }; // Для усиления резкости

            //// Настройка ZXing
            //var barcodeReader = new BarcodeReader
            //{
            //    AutoRotate = true,
            //    Options = new DecodingOptions
            //    {
            //        TryHarder = true,
            //        TryInverted = true,
            //        PossibleFormats = new[] { BarcodeFormat.QR_CODE },
            //        PureBarcode = false
            //    }
            //};

            // Итеративная обработка
            foreach (var clipLimit in clipLimits)
            {
                // Применение CLAHE контраст
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);

                foreach (var kernel in Kernels)
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

                        // Бинаризация
                        Mat binaryImage = new Mat();
                        Cv2.Threshold(sharpened, binaryImage, 100, 255, ThresholdTypes.Binary); //была -  Otsu


                        // Обнаружение линий
                        Mat edgesCanny = new Mat();
                        Cv2.Canny(binaryImage, edgesCanny, 50, 150);
                       
                        Cv2.AddWeighted(binaryImage, sharpWeight, edgesCanny, -0.5, 0, binaryImage);

                        //Обнаружение линий
                        /*
                        // 6. Обнаружение линий
                        LineSegmentPoint[] lines = Cv2.HoughLinesP(
                            binaryImage,         // Бинарное изображение
                            rho: 1,              // Разрешение по радиусу
                            theta: Math.PI / 180, // Разрешение по углу
                            threshold: 30,       // Минимальное число точек на линии - это можно давать как параметр
                            minLineLength: 30,   // Минимальная длина линии
                            maxLineGap: 10       // Максимальный разрыв между точками на одной линии
                        );

                        // Рисуем найденные линии
                        foreach (var line in lines)
                        {
                            Cv2.Line(binaryImage, line.P1, line.P2, Scalar.White, 2);
                        }
                        */

                        // Конвертация в Bitmap 
                        using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);

                        // Создаем детектор QR-кодов
                        QRCodeDetector qrDetector = new QRCodeDetector();

                        // Распознаем и декодируем QR-код
                        Point2f[] points;
                        string[] results = Array.Empty<string>();
                        bool isDetected = qrDetector.DetectMulti(binaryImage, out points);
                        if (isDetected)
                        {
                            qrDetector.DecodeMulti(binaryImage, points, out results);

                            foreach (var result in results)
                            {
                                // Конвертация изображения в Bitmap 
                                if (QRCodeData.IsMatchingATQRCode(result))
                                {
                                    // Успешно распознано
                                    Console.WriteLine("QR codes have been found:");
                                    Console.WriteLine($"Contents: {result}");

                                    qrCodeData = new QRCodeData(result);

                                    // сохранить все в директорию в распознанными файлами
                                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed15", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
                                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));

                                    bitmap.Save(processedFilePath, ImageFormat.Png);
                                    return qrCodeData;
                                }
                            }
                        }
                        if (qrCodeData is null)
                        {
                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed15", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
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
            //string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2");
            //Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = {1.0, 2.0, 3.0 };     // Для CLAHE
            int[] kernels = {3, 5, 7, 9};                // Для медианного фильтра
            double[] sharpWeights = {1.2, 1.5, 3.0 };   // Для усиления резкости
            int[] thrBlocks = {11, 15, 19 };           // Для адаптивной бинаризации
            OpenCvSharp.Size[] blurKernels = { new OpenCvSharp.Size(3, 3), new OpenCvSharp.Size(5, 5), new OpenCvSharp.Size(9, 9) };


            //double[] clipLimits = { 1.0, 2.0, 3.0 };                  // Для CLAHE
            //int[] kernels = { 3, 5, 7 };                              // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 };                // Для усиления резкости
            //int[] thrBlocks = { 11, 15, 19 };                         // Для адаптивной бинаризации

/*
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
*/
            // Итеративный перебор параметров
            foreach (var clipLimit in clipLimits)
            {
                // Применение CLAHE для улучшения контраста
                var clahe = Cv2.CreateCLAHE(clipLimit: clipLimit, tileGridSize: new OpenCvSharp.Size(8, 8));
                Mat enhanced = new Mat();
                clahe.Apply(gray, enhanced);
                
                {// запись
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(enhanced); 
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"1processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-{Path.GetExtension(imagePath)}");
                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }

                foreach (var kernel in kernels)
                {
                    // Устранение шума медианным фильтром
                    Mat denoised = new Mat();
                    Cv2.MedianBlur(enhanced, denoised, kernel);
                    
                    {//запись
                        using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(denoised);
                        string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"2processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}{Path.GetExtension(imagePath)}");
                        Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                        bitmap.Save(processedFilePath, ImageFormat.Png);
                    }

                    foreach (var sharpWeight in sharpWeights)
                    {
                        foreach (var blurKernel in blurKernels)
                        {
                            // Усиление резкости через Unsharp Masking
                            Mat edges = new Mat();
                            Cv2.GaussianBlur(denoised, edges, blurKernel, 0);
                            {// запись
                                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(edges);
                                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"3processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
                                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                bitmap.Save(processedFilePath, ImageFormat.Png);
                            }

                            Mat sharpened = new Mat();
                            Cv2.AddWeighted(denoised, sharpWeight, edges, -0.5, 0, sharpened);
                            {// запись
                                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);
                                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"4processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
                                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                bitmap.Save(processedFilePath, ImageFormat.Png);
                            }

                            foreach (var blockSize in thrBlocks)
                            {
                                // Адаптивная бинаризация
                                Mat binaryImage = new Mat();
                                //Cv2.Threshold(sharpened, binary, 127, 255, ThresholdTypes.Otsu);
                                Cv2.AdaptiveThreshold(sharpened, binaryImage, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blockSize, 2);
                                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);


                                // Конвертация изображения в Bitmap для ZXing
                                //using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(sharpened);

                                // Попытка распознать QR-коды
                                //var results = barcodeReader.DecodeMultiple(bitmap);
                                //string result = qrDetector.DetectAndDecode(binaryImage, out points);


                                // Создаем детектор QR-кодов
                                QRCodeDetector qrDetector = new QRCodeDetector();

                                // Распознаем и декодируем QR-код
                                Point2f[] points;
                                string[] results = Array.Empty<string>();
                                bool isDetected = qrDetector.DetectMulti(binaryImage, out points);
                                if (isDetected)
                                    qrDetector.DecodeMulti(binaryImage, points, out results);

                                if (isDetected)
                                {
                                    foreach (var result in results)
                                    {
                                        // Конвертация изображения в Bitmap 
                                        if (QRCodeData.IsMatchingATQRCode(result))
                                        {
                                            // Успешно распознано
                                            Console.WriteLine("QR codes have been found:");
                                            Console.WriteLine($"Contents: {result}");

                                            qrCodeData = new QRCodeData(result);

                                            // сохранить все в директорию в распознанными файлами
                                            string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                                            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));

                                            bitmap.Save(processedFilePath, ImageFormat.Png);
                                            return qrCodeData;
                                        }
                                    }
                                }
                                if (qrCodeData is null)
                                {
                                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed2", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-sharpWeight{sharpWeight}{Path.GetExtension(imagePath)}");
                                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                    bitmap.Save(processedFilePath, ImageFormat.Png);
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





        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode3(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Удаление директории 
            //string deletedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed3");
            //Directory.Delete(Path.GetDirectoryName(deletedFilePath), true);

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Конвертация в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

            // Список параметров для итераций
            double[] clipLimits = { 1.0,3.0 };     // Для CLAHE
            int[] kernels = { 5, 7, 9 };                                   // Для медианного фильтра
            double[] sharpWeights = { 1.2, 2.0 };   // Для усиления резкости
            int[] thrBlocks = { 11, 19 };                           // Для адаптивной бинаризации
            int[] houghThresholds = { 30, 70 };        // Для порога Хафа
            OpenCvSharp.Size[] blurKernels = { new OpenCvSharp.Size(3, 3), new OpenCvSharp.Size(9, 9) };


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

                    foreach (var sharpWeight in sharpWeights)
                    {
                        foreach (var blurKernel in blurKernels)
                        {
                            // Усиление резкости через Unsharp Masking
                            Mat blurred = new Mat();
                            Cv2.GaussianBlur(denoised, blurred, blurKernel, 0);
                            Mat sharpened = new Mat();
                            Cv2.AddWeighted(denoised, sharpWeight, blurred, -0.5, 0, sharpened);

                            foreach (var blockSize in thrBlocks)
                            {
                                // Адаптивная бинаризация
                                Mat binary = new Mat();
                                Cv2.AdaptiveThreshold(sharpened, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blockSize, 2);

                                foreach (var houghThreshold in houghThresholds)
                                {
                                    // Преобразование Хафа для обнаружения линий
                                    Mat edges = new Mat();
                                    //!!!!!!!!!!!!!!!!!!!!!!1 отличие от 1 й
                                    Cv2.Canny(binary, edges, 50, 150);
                                    //!!!!!!!!!!!!!!!!!!!!!!1 отличие от 1 й
                                    LineSegmentPoint[] lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, houghThreshold, minLineLength: 50, maxLineGap: 10);

                                    // Наложение найденных линий на изображение
                                    foreach (var line in lines)
                                    {
                                        Cv2.Line(binary, line.P1, line.P2, Scalar.White, 2); // Усиливаем линии
                                    }

                        

                                    // Конвертация изображения в Bitmap для ZXing
                                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary);

                                    // Попытка распознать QR-коды
                                    var results = barcodeReader.DecodeMultiple(bitmap);
                                    if (results != null && results.Length > 0)
                                    {
                                        // Успешно распознано
                                        Console.WriteLine("QR codes have been found:");

                                        foreach (var result in results)
                                        {
                                            Console.WriteLine($"Contents: {result.Text}");
                                            if (QRCodeData.IsMatchingATQRCode(result.Text))
                                            {
                                                qrCodeData = new QRCodeData(result.Text);
                                            }
                                        }


                                        // сохранить все в директорию в распознанными файлами
                                        string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed3", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}-houghThreshold{houghThreshold}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
                                        Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                                        bitmap.Save(processedFilePath, ImageFormat.Png);
                                        return qrCodeData;
                                    }
                                    else
                                    {
                                        string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed3", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}-clipLimit{clipLimit}-kernel{kernel}-sharpWeight{sharpWeight}-thrBlock{blockSize}-houghThreshold{houghThreshold}-blurKernel{blurKernel}{Path.GetExtension(imagePath)}");
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


        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode4(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);


            //double[] clipLimits = { 1.0, 2.0, 3.0 };       // Для CLAHE
            //int[] blurKernels = { 3, 5, 7 };               // Для медианного фильтра
            //double[] sharpWeights = { 1.2, 1.5, 2.0 };     // Для усиления резкости
            //int[] adaptiveThresholdBlocks = { 11, 15, 19 }; // Для адаптивной бинаризации
            //int[] houghThresholds = { 30, 50, 70 };        // Для порога Хафа
            //Size[] blurKernels = { new Size(3, 3), new Size(5, 5), new Size(7, 7), new Size(9, 9) };


            // 2. Преобразование в оттенки серого
            Mat grayImage = new Mat(); 
            Cv2.CvtColor(original, grayImage, ColorConversionCodes.BGR2GRAY);


            // 3. Улучшение контраста
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            Mat enhancedImage = new Mat();
            clahe.Apply(grayImage, enhancedImage);

            // 4. Фильтрация шума
            Mat filteredImage = new Mat();
            Cv2.MedianBlur(enhancedImage, filteredImage, 7);

            // 5. Бинаризация изображения
            Mat binaryImage = new Mat();
            Cv2.AdaptiveThreshold(filteredImage, binaryImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 2);

            
            // Создаем детектор QR-кодов
            QRCodeDetector qrDetector = new QRCodeDetector();

            // Распознаем и декодируем QR-код
            Point2f[] points;
            string[] results = Array.Empty<string>();
            bool isDetected = qrDetector.DetectMulti(binaryImage, out points);
            if(isDetected) 
                qrDetector.DecodeMulti(binaryImage, points, out results);

            //string result = qrDetector.DetectAndDecode(binaryImage, out points);

            foreach (var result in results)
            {
                if (QRCodeData.IsMatchingATQRCode(result))
                {
                    // Успешно распознано
                    Console.WriteLine("QR codes have been found:");
                    Console.WriteLine($"Contents: {result}");

                    qrCodeData = new QRCodeData(result);

                    // сохранить все в директорию в распознанными файлами
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed4", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    // Конвертация изображения в Bitmap 
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                    return qrCodeData;
                }
            }
            if (qrCodeData == null)
            {
                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed4", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                // Конвертация изображения в Bitmap
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);
                bitmap.Save(processedFilePath, ImageFormat.Png);
            }
            return qrCodeData;
        }


        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> AutoProcessAndDecodeQRCode5(string imagePath)
        {
            QRCodeData? qrCodeData = null;

            // Чтение изображения
            Mat original = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (original.Empty())
            {
                Console.WriteLine("Failed to load the image.");
                return qrCodeData;
            }

            // Создаем детектор QR-кодов
            QRCodeDetector qrDetector = new QRCodeDetector();

            // Шаг 1: Попытка прямого распознавания
            Point2f[] points;
            string result = qrDetector.DetectAndDecode(original, out points);
            if (!string.IsNullOrEmpty(result) && QRCodeData.IsMatchingATQRCode(result))
            {
                // Успешно распознано
                Console.WriteLine("QR codes have been found:");
                Console.WriteLine($"Contents: {result}");

                qrCodeData = new QRCodeData(result);

                DrawQRCodeBounds(original, points);
                // сохранить все в директорию в распознанными файлами
                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed5", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                // Конвертация изображения в Bitmap 
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(original);
                bitmap.Save(processedFilePath, ImageFormat.Png);
                return qrCodeData;
            }
            if (qrCodeData == null)
            {
                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed5", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                // Конвертация изображения в Bitmap
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(original);
                bitmap.Save(processedFilePath, ImageFormat.Png);
            }
            

            // Шаг 2: Преобразование в оттенки серого
            Console.WriteLine("Pre-treatment attempt...");
            Mat grayImage = new Mat();
            Cv2.CvtColor(original, grayImage, ColorConversionCodes.BGR2GRAY);

            // Шаг 3: Применение адаптивной фильтрации
            Mat enhancedImage = EnhanceImage(grayImage);

            // Шаг 4: Повторное распознавание после обработки
            result = qrDetector.DetectAndDecode(enhancedImage, out points);
            if (!string.IsNullOrEmpty(result) && QRCodeData.IsMatchingATQRCode(result))
            {
                // Успешно распознано
                Console.WriteLine("QR codes have been found:");
                Console.WriteLine($"Contents: {result}");

                qrCodeData = new QRCodeData(result);

                DrawQRCodeBounds(original, points);
                // сохранить все в директорию в распознанными файлами
                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed5", $"processed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                // Конвертация изображения в Bitmap 
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(enhancedImage);
                bitmap.Save(processedFilePath, ImageFormat.Png);
                return qrCodeData;
            }
            if (qrCodeData == null)
            {
                string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed5", $"NOTprocessed-{Path.GetFileNameWithoutExtension(imagePath)}{Path.GetExtension(imagePath)}");
                Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                // Конвертация изображения в Bitmap
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(enhancedImage);
                bitmap.Save(processedFilePath, ImageFormat.Png);
            }
            return qrCodeData;
        }  

        /// <summary>
        /// Улучшает качество изображения для распознавания QR-кодов.
        /// </summary>
        static Mat EnhanceImage(Mat grayImage)
        {
            Mat enhancedImage = new Mat();

            // Применяем размытие для устранения шумов
            Cv2.GaussianBlur(grayImage, enhancedImage, new OpenCvSharp.Size(5, 5), 0);

            // Увеличиваем контрастность и резкость
            Cv2.EqualizeHist(enhancedImage, enhancedImage);

            // Применяем адаптивный порог
            Cv2.AdaptiveThreshold(
                enhancedImage,
                enhancedImage,
                255,
                AdaptiveThresholdTypes.MeanC,
                ThresholdTypes.Binary,
                11,
                2
            );

            return enhancedImage;
        }

        /// <summary>
        /// Рисует границы вокруг обнаруженного QR-кода.
        /// </summary>
        static void DrawQRCodeBounds(Mat image, Point2f[] points)
        {
            if (points != null && points.Length > 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Point2f start = points[i];
                    Point2f end = points[(i + 1) % points.Length];
                    Cv2.Line(image, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y, Scalar.Green, 2);
                }
            }
            
        }

    }
}
