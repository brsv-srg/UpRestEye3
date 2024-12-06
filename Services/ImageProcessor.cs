using Microsoft.ML;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.IO;
using System.Linq;
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


namespace UpRestEye3.Services
{
    
    // Класс обработки изображения
    public class ImageProcessor
    {
        private readonly MLImageParametersPredictor _predictor;

        public ImageProcessor(string predictorModelPath)
        {
            _predictor = new MLImageParametersPredictor(predictorModelPath);
        }

        public async Task<string> ProcessImageAsync(string imagePath)
        {

            // Шаг 1. Создание дайджеста изображения
            var digest = GenerateImageDigest(imagePath);

            // Шаг 2. Попытка распознать "в лоб"
            if (TryDecodeQRCode(imagePath, out string qrCodeText))
            {
                _predictor.UpdateModelOnline(digest, new PredictionParameters()); // Обучение без параметров
                return qrCodeText;
            }

            // Шаг 3. Получение параметров обработки
            var parameters = _predictor.PredictParameters(digest);

            if (parameters.Any())
            {
                var processedImage = ApplyImageProcessing(imagePath, parameters);
                if (TryDecodeQRCode(processedImage, out qrCodeText))
                {
                    _predictor.UpdateModelOnline(digest, parameters); // Обучение с параметрами
                    return qrCodeText;
                }
            }

            // Список параметров для итераций
            double[] medianBlurKernels = { 5, 7, 9 };                             // Для медианного фильтра - удаление шумов
            double[] convScaleContrasts = { 1.2, 1.5, 1.7, 2.0 };                 // Уровень контраста
            double[] convScaleBrightnesses = { -20.0, -10.0, 0.0, 10.0, 20.0 };   // Уровень яркости
            double[] adThresBlocks = { 11, 15, 19 };                              // Для адаптивной бинаризации
            double[,] cannyThresholds = new double[,] { { 80, 160 }, { 50, 150 }, { 10, 100 }, { 100, 200 } }; // Массив пар значений для обнаружения линий

            // Шаг 5. Подбор параметров во вложенных циклах
            foreach (var medianBlurKernel in medianBlurKernels)
            {
                foreach (var convScaleContrast in convScaleContrasts)
                {
                    foreach (var convScaleBrightness in convScaleBrightnesses)
                    {
                        foreach (var adThresBlock in adThresBlocks)
                        {
                            for (int i = 0; i < cannyThresholds.GetLength(0); i++)
                            {
                                var manualParams = new PredictionParameters()
                                {
                                    medianBlurKernel = medianBlurKernel,
                                    convScaleContrast = convScaleContrast,
                                    convScaleBrightness = convScaleBrightness,
                                    adThresBlock = adThresBlock,
                                    cannyThreshold1 = cannyThresholds[i, 0],
                                    cannyThreshold2 = cannyThresholds[i, 1]
                                };

                                var processedImage = ApplyImageProcessing(imagePath, manualParams);
                                if (TryDecodeQRCode(processedImage, out qrCodeText))
                                {
                                    _predictor.UpdateModelOnline(digest, manualParams);
                                    return qrCodeText;
                                }
                            }
                        }
                    }
                }
            }

            

            // Шаг 6. Обращение к внешней модели
            var externalParams = new PredictionParameters()
            {
                medianBlurKernel = 5,
                convScaleContrast = 1.2,
                convScaleBrightness = 20,
                adThresBlock = 11,
                cannyThreshold1 = 80,
                cannyThreshold2 = 160
            };

            var image = ApplyImageProcessing(imagePath, externalParams);



            var ocrClient = new GoogleVisionService();

            var recResult = await ocrClient.RecognizeAI(image.ToBytes());

            if (recResult != null)
            {
                foreach(var res in recResult)
                {
                    if (IsMatchingATQRCode(res))
                    {
                        _predictor.UpdateModelOnline(digest, externalParams); // Обучение по данным API
                        return res;
                    }
                }
            }

            return string.Empty;
        }

        private ImageDigest GenerateImageDigest(string imagePath)
        {
            var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            
            var brightness = Cv2.Mean(image).Val0;

            Cv2.MeanStdDev(image, out _, out var stdDev);
            var contrast = stdDev.Val0;

            Mat laplacian = new Mat();
            Cv2.Laplacian(image, laplacian, MatType.CV_64F);
            Scalar mu, sigma;
            Cv2.MeanStdDev(laplacian, out mu, out sigma);
            double sharpness = sigma.Val0 * sigma.Val0;

            var blurredImage = new Mat();
            Cv2.GaussianBlur(image, blurredImage, new Size(5, 5), 0);
            var noiseLevel = Cv2.Mean(blurredImage).Val0;

            int width = image.Width;
            int height = image.Height;
            double aspectRatio = (double)width / height;

            return new ImageDigest
            {
                Brightness = (float)brightness,
                Contrast = (float)contrast,
                NoiseLevel = (float)noiseLevel,
                Sharpness = (float)sharpness,
                AspectRatio = (float)aspectRatio
            };
        }

        private bool TryDecodeQRCode(string imagePath, out string qrCodeText)
        {
            var image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            return TryDecodeQRCode(image, out qrCodeText);
        }

        private bool TryDecodeQRCode(Mat image, out string qrCodeText)
        {
            Point2f[] points; 
            var detector = new QRCodeDetector();
            qrCodeText = detector.DetectAndDecode(image, out points);
            return !string.IsNullOrEmpty(qrCodeText);
        }

        public Mat ApplyImageProcessing(string imagePath, PredictionParameters parameters)
        {
            // 1. Загрузка изображения
            Mat image = Cv2.ImRead(imagePath, ImreadModes.Color); //+

            // 2. Преобразование в оттенки серого
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY); //+

            // 3. Удаление шума
            Mat denoised = new Mat();
            Cv2.MedianBlur(gray, denoised, (int)parameters.medianBlurKernel); //+

            // 4. Усиление контраста
            Mat enhanced = new Mat();
            Cv2.ConvertScaleAbs(denoised, enhanced, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость

            // 5. Бинаризация
            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(enhanced, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, (int)parameters.adThresBlock, 2);

            // 6. Выделение границ
            Mat edgesCanny = new Mat();
            Cv2.Canny(binary, edgesCanny, parameters.cannyThreshold1, parameters.cannyThreshold2);
            //Cv2.AddWeighted(binary, sharpWeight, edgesCanny, -0.5, 0, binary);

            return edgesCanny;
        }

        public bool IsMatchingATQRCode(string input)
        {
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";
            return Regex.IsMatch(input, pattern);
        }
    }
}