using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.Data;
using OpenCvSharp;
using Microsoft.ML;
using System;
using System.Collections.Generic;


namespace UpRestEye3.Services
{
    public class MLAService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private IDataView _trainingData;

        public MLAService()
        {
            _mlContext = new MLContext();

            // Создаем начальную модель
            var pipeline = _mlContext.Transforms.Concatenate("Features", new[] { "Brightness", "Contrast", "Sharpness", "NoiseLevel", "AspectRatio" })
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

            // Инициализируем пустую обучающую выборку
            _trainingData = _mlContext.Data.LoadFromEnumerable(new List<ImageData>());
            _model = pipeline.Fit(_trainingData);
        }

        public float[] GetParameters(ImageData digest)
        {
            // Прогнозируем параметры обработки
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, ParameterPrediction>(_model);
            var prediction = predictionEngine.Predict(digest);

            return new[] { prediction.Contrast, prediction.Threshold, prediction.BlurStrength };
        }

        public void UpdateModel(ImageData imageData)
        {
            // Обновляем модель с использованием новых данных
            var newData = new List<ImageData> { imageData };
            _trainingData = _mlContext.Data.LoadFromEnumerable(newData);
            _model = _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression().Fit(_trainingData);
        }

        public ImageData GenerateDigest(Mat image)
        {
            // Преобразуем изображение в оттенки серого
            Mat grayImage = new Mat();
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // Генерируем характеристики
            double brightness = Cv2.Mean(grayImage)[0];

            Mat mean = new Mat();
            Mat stddev = new Mat();
            Cv2.MeanStdDev(grayImage, mean, stddev);
            double contrast = stddev.At<double>(0);

            Mat laplacian = new Mat();
            Cv2.Laplacian(grayImage, laplacian, MatType.CV_64F);
            Scalar mu, sigma;
            Cv2.MeanStdDev(laplacian, out mu, out sigma);
            double sharpness = sigma.Val0 * sigma.Val0;

            Mat blurred = new Mat();
            Cv2.GaussianBlur(grayImage, blurred, new Size(5, 5), 0);
            Mat noise = grayImage - blurred;
            Scalar noiseLevel = Cv2.Mean(noise);

            int width = grayImage.Width;
            int height = grayImage.Height;
            double aspectRatio = (double)width / height;

            return new ImageData
            {
                Brightness = (float)brightness,
                Contrast = (float)contrast,
                Sharpness = (float)sharpness,
                NoiseLevel = (float)noiseLevel[0],
                AspectRatio = (float)aspectRatio
            };
        }
    }

    public class ImageData
    {
        public float Brightness { get; set; }
        public float Contrast { get; set; }
        public float Sharpness { get; set; }
        public float NoiseLevel { get; set; }
        public float AspectRatio { get; set; }
        public float Threshold { get; set; }
        public float BlurStrength { get; set; }
        public bool Success { get; set; }
    }

    public class ParameterPrediction
    {
        public float Contrast { get; set; }
        public float Threshold { get; set; }
        public float BlurStrength { get; set; }
    }


}
