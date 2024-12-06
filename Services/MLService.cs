using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.Data;
using OpenCvSharp;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using UpRestEye3.MLImageModels;
using System.Linq;


namespace UpRestEye3.Services
{
    //Класс создает и обучает модель машинного обучения
    public class MLAService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private IDataView _trainingData;
        private const string ModelPath = "qr_code_model.zip";
        private const string DataPath = "training_data.csv";

        public MLAService()
        {
            _mlContext = new MLContext();
            LoadDataAndModel();
        }

        // Загрузка существующей модели и данных
        private void LoadDataAndModel()
        {
            if (File.Exists(DataPath))
            {
                _trainingData = _mlContext.Data.LoadFromTextFile<TrainingRecord>(DataPath, hasHeader: true, separatorChar: ',');
            }

            if (File.Exists(ModelPath))
            {
                _model = _mlContext.Model.Load(ModelPath, out _);
            }
        }

        // Дообучение модели на новых данных
        public void UpdateModel(IEnumerable<TrainingData> newTrainingData, string modelPath, string trainingDataPath)
        {
            // Объединяем старые и новые данные
            var newDataView = _mlContext.Data.LoadFromEnumerable(newTrainingData);
            _trainingData = _trainingData == null ? newDataView : _mlContext.Data.LoadFromEnumerable(_mlContext.Data.CreateEnumerable<TrainingData>(_trainingData, reuseRowObject: false).Concat(newTrainingData));


            // Пайплайн обучения
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(TrainingData.Brightness),
                    nameof(TrainingData.Contrast),
                    nameof(TrainingData.NoiseLevel),
                    nameof(TrainingData.Sharpness),
                    nameof(TrainingData.AspectRatio))
                .Append(_mlContext.Transforms.Concatenate("Label",
                    nameof(TrainingData.MedianBlurKernel),
                    nameof(TrainingData.ConvScaleContrast),
                    nameof(TrainingData.ConvScaleBrightness),
                    nameof(TrainingData.AdThresBlock),
                    nameof(TrainingData.CannyThreshold1),
                    nameof(TrainingData.CannyThreshold2)))
                .Append(_mlContext.Regression.Trainers.Sdca());

            // Дообучение модели
            _model = pipeline.Fit(_trainingData);

            // Сохранение обновленной модели и данных
            SaveModel(modelPath, trainingDataPath);
        }


        // Дообучение на новых данных
        public void UpdateModel(ImageDigest digest, PredictionParameters parameters)
        {
            // Создание новой записи для обновления
            var newRecord = new TrainingData
            {
                Brightness = digest.Brightness,
                Contrast = digest.Contrast,
                NoiseLevel = digest.NoiseLevel,
                Sharpness = digest.Sharpness,
                AspectRatio = digest.AspectRatio,
                MedianBlurKernel = parameters.medianBlurKernel,
                ConvScaleContrast = parameters.convScaleContrast,
                ConvScaleBrightness = parameters.convScaleBrightness,
                AdThresBlock = parameters.adThresBlock,
                CannyThreshold1 = parameters.cannyThreshold1,
                CannyThreshold2 = parameters.cannyThreshold2
            };

            // Добавление новых данных
            var newDataView = _mlContext.Data.LoadFromEnumerable(new[] { newRecord });
            _trainingData = _trainingData == null
                ? newDataView
                : _mlContext.Data.Append(_trainingData, newDataView);

            _trainingData = _trainingData == null 
                                            ? newDataView 
                                            : _mlContext.Data.LoadFromEnumerable(_mlContext.Data.CreateEnumerable<TrainingData>(_trainingData, reuseRowObject: false).Concat(newDataView));

            // Пайплайн обучения
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(TrainingData.Brightness),
                    nameof(TrainingData.Contrast),
                    nameof(TrainingData.NoiseLevel),
                    nameof(TrainingData.Sharpness),
                    nameof(TrainingData.AspectRatio))
                .Append(_mlContext.Transforms.Concatenate("Label",
                    nameof(TrainingData.MedianBlurKernel),
                    nameof(TrainingData.ConvScaleContrast),
                    nameof(TrainingData.ConvScaleBrightness),
                    nameof(TrainingData.AdThresBlock),
                    nameof(TrainingData.CannyThreshold1),
                    nameof(TrainingData.CannyThreshold2)))
                .Append(_mlContext.Regression.Trainers.Sdca());

            // Обучение модели
            _model = pipeline.Fit(_trainingData);

            // Сохранение обновленной модели и данных
            SaveDataAndModel();
        }

        // Прогнозирование
        public PredictionParameters Predict(ImageDigest digest)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageDigest, PredictionParameters>(_model);
            return predictionEngine.Predict(digest);
        }

        // Сохранение данных и модели
        private void SaveDataAndModel()
        {
            // Сохранение данных
            using (var writer = new StreamWriter(DataPath))
            {
                var data = _mlContext.Data.CreateEnumerable<TrainingRecord>(_trainingData, reuseRowObject: false);
                foreach (var row in data)
                {
                    writer.WriteLine($"{row.Brightness},{row.Contrast},{row.NoiseLevel},{row.Sharpness},{row.AspectRatio},{row.MedianBlurKernel},{row.ConvScaleContrast},{row.ConvScaleBrightness},{row.AdThresBlock},{row.CannyThreshold1},{row.CannyThreshold2}");
                }
            }

            // Сохранение модели
            _mlContext.Model.Save(_model, _trainingData.Schema, ModelPath);
        }



        public PredictionParameters Predict(ImageDigest digest)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageDigest, PredictionParameters>(_model);
            return predictionEngine.Predict(digest);
        }
       
        public void UpdateModel(ImageDigest imageData)
        {
            // Обновляем модель с использованием новых данных
            var newData = new List<ImageDigest> { imageData };
            _trainingData = _mlContext.Data.LoadFromEnumerable(newData);
            _model = _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression().Fit(_trainingData);
        }

        // Вспомогательные классы
        public class TrainingData
        {
            public double Brightness { get; set; }
            public double Contrast { get; set; }
            public double NoiseLevel { get; set; }
            public double Sharpness { get; set; }
            public float AspectRatio { get; set; }
            public double MedianBlurKernel { get; set; }
            public double ConvScaleContrast { get; set; }
            public double ConvScaleBrightness { get; set; }
            public double AdThresBlock { get; set; }
            public double CannyThreshold1 { get; set; }
            public double CannyThreshold2 { get; set; }
        }
    }


    // Класс работы с моделью машинного обучения. Получает параметры, онлайн дообучает модель. 
    public class MLImageParametersPredictor
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private IDataView _trainingData;

        public MLImageParametersPredictor(string modelPath)
        {
            _mlContext = new MLContext();
            _trainingData = _mlContext.Data.LoadFromEnumerable(new ImageData[] { });
            _model = _mlContext.Model.Load(modelPath, out _);
        }

        public PredictionParameters PredictParameters(ImageDigest digest)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageDigest, PredictionParameters>(_model);
            return predictionEngine.Predict(digest);
        }

        public void UpdateModelOnline(ImageDigest digest, PredictionParameters parameters)
        {
            // Преобразуем существующее IDataView в IEnumerable
            var existingData = _mlContext.Data.CreateEnumerable<ImageData>(_trainingData, reuseRowObject: false);

            // Создаем новое обучение с объединением данных
            var newTrainingData = existingData.Concat(new[] { new ImageData { Digest = digest, Parameters = parameters } });

            // Создаем новый IDataView с объединенными данными
            _trainingData = _mlContext.Data.LoadFromEnumerable(newTrainingData);

            // Обучаем модель заново
            _model = _mlContext.Transforms.Concatenate("Features", nameof(ImageDigest.Brightness), nameof(ImageDigest.Contrast), nameof(ImageDigest.NoiseLevel))
                .Append(_mlContext.Regression.Trainers.Sdca())
                .Fit(_trainingData);
        }
    }

}
