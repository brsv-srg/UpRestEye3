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
using UpRestEye3.Services;
using Google.Protobuf.WellKnownTypes;
using ImageMagick;
using System.Text.RegularExpressions;


namespace UpRestEye3.NewServices
{
    

    // Класс параметризации обработки изображений
    public class ParameterPredictor
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private IDataView _trainingData;

        public ParameterPredictor(string modelPath)
        {
            _mlContext = new MLContext();
            _trainingData = _mlContext.Data.LoadFromEnumerable(new NewImageData[] { });
            _model = _mlContext.Model.Load(modelPath, out _);
        }

        public PredictionParameters PredictParameters(ImageDigest digest)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageDigest, PredictionParameters>(_model);
            return predictionEngine.Predict(digest);
        }

        public void UpdateModel(ImageDigest digest, PredictionParameters parameters)
        {
            // Преобразуем существующее IDataView в IEnumerable
            var existingData = _mlContext.Data.CreateEnumerable<NewImageData>(_trainingData, reuseRowObject: false);

            // Создаем новое обучение с объединением данных
            var newTrainingData = existingData.Concat(new[] {new NewImageData { Digest = digest, Parameters = parameters }});

            // Создаем новый IDataView с объединенными данными
            _trainingData = _mlContext.Data.LoadFromEnumerable(newTrainingData);

            // Обучаем модель заново
            _model = _mlContext.Transforms.Concatenate("Features", nameof(ImageDigest.Brightness), nameof(ImageDigest.Contrast), nameof(ImageDigest.NoiseLevel))
                .Append(_mlContext.Regression.Trainers.Sdca())
                .Fit(_trainingData);
        }
    }

    // Класс обработки изображения
    public class NewImageProcessor
    {
        private readonly ParameterPredictor _predictor;
        private readonly HttpClient _httpClient;

        public NewImageProcessor(string classifierModelPath, string predictorModelPath)
        {
            _predictor = new ParameterPredictor(predictorModelPath);
            _httpClient = new HttpClient();
        }

        public async Task<string> ProcessImageAsync(string imagePath)
        {

            // Шаг 2. Создание дайджеста изображения
            var digest = GenerateImageDigest(imagePath);

            // Шаг 3. Попытка распознать "в лоб"
            if (TryDecodeQRCode(imagePath, out string qrCodeText))
            {
                _predictor.UpdateModel(digest, new PredictionParameters()); // Обучение без параметров
                return qrCodeText;
            }

            // Шаг 4. Получение параметров обработки
            var parameters = _predictor.PredictParameters(digest);

            if (parameters.Any())
            {
                var processedImage = ApplyImageProcessing(imagePath, parameters);
                if (TryDecodeQRCode(processedImage, out qrCodeText))
                {
                    _predictor.UpdateModel(digest, parameters); // Обучение с параметрами
                    return qrCodeText;
                }
            }

            // Список параметров для итераций
            double[] medianBlurKernels = { 5, 7, 9 };                             // Для медианного фильтра - удаление шумов
            double[] convScaleContrasts = { 1.2, 1.5, 1.7, 2.0 };                 // Уровень контраста
            double[] convScaleBrightnesses = { -20.0, -10.0, 0.0, 10.0, 20.0 };     // Уровень яркости
            double[] adThresBlocks = { 11, 15, 19 };                             // Для адаптивной бинаризации
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
                                    _predictor.UpdateModel(digest, manualParams);
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



            var ocrClient = new GoogleVisionOCR();

            var recResult = await ocrClient.RecognizeAI(image.ToBytes());

            if (recResult != null)
            {
                foreach(var res in recResult)
                {
                    if (IsMatchingATQRCode(res))
                    {
                        _predictor.UpdateModel(digest, externalParams); // Обучение по данным API
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
            var blurredImage = new Mat();
            Cv2.GaussianBlur(image, blurredImage, new Size(5, 5), 0);
            var noiseLevel = Cv2.Mean(blurredImage).Val0;


            return new ImageDigest
            {
                Brightness = (float)brightness,
                Contrast = (float)contrast,
                NoiseLevel = (float)noiseLevel
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

    // Вспомогательные классы
    public class ImageInput
    {
        public string ImagePath { get; set; }
    }

    public class ImagePrediction
    {
        public string PredictedLabel { get; set; }
    }

    public class ImageDigest
    {
        public double Brightness { get; set; }      // Уровень яркости
        public double Contrast { get; set; }        // Уровень контраста
        public double NoiseLevel { get; set; }      // Уровень шума
        public double Sharpness { get; set; }       // Уровень резкости
    }

    public class NewImageData
    {
        public ImageDigest Digest { get; set; }
        public PredictionParameters Parameters { get; set; }
    }

    public class PredictionParameters
    {
        public double medianBlurKernel { get; set; } = 0.0;         // Для медианного фильтра - удаление шумов
        public double convScaleContrast { get; set; } = 0.0;        // Уровень контраста
        public double convScaleBrightness { get; set; } = 0.0;      // Уровень яркости
        public double adThresBlock { get; set; } = 0.0;             // Для адаптивной бинаризации
        public double cannyThreshold1 { get; set; } = 0.0;          // Для обнаружения линий, нижняя граница, для QR 80
        public double cannyThreshold2 { get; set; } = 0.0;          // Для обнаружения линий, верхняя граница, для QR 160

        public bool Any()
        { return medianBlurKernel != 0 || 
                    convScaleContrast != 0.0 ||
                    convScaleBrightness != 0.0 ||
                    adThresBlock != 0.0 ||
                    cannyThreshold1 != 0.0 ||
                    cannyThreshold2 != 0.0; }
    }

}