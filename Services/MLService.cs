using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.Data;
using OpenCvSharp;
using Microsoft.ML;
using System;
using System.Configuration;
using System.Collections.Generic;
using UpRestEye3.MLImageModels;
using System.Linq;
using static UpRestEye3.Services.MLService;
using Tensorflow;
using Microsoft.ML.Data;


namespace UpRestEye3.Services
{
    // Интерфейс сервиса по работе с ML
    public interface IMLService
    {
        void UpdateModel(ImageDigest digest, PredictionParameters parameters);
        PredictionParameters Predict(ImageDigest digest);
    }

    //Класс создает и обучает модель машинного обучения
    public class MLService: IMLService
    {
        private readonly MLContext _mlContext;
        private readonly Dictionary<string, ITransformer> _models;
        private IDataView _trainingData;
        private readonly string _modelPath;
        private readonly string _dataPath;
        private readonly string[] _labels = {
            nameof(TrainingData.medianBlurKernel),
            nameof(TrainingData.convScaleContrast),
            nameof(TrainingData.convScaleBrightness),
            nameof(TrainingData.adThresBlock),
            nameof(TrainingData.cannyThreshold1),
            nameof(TrainingData.cannyThreshold2) };

        public MLService()
        {
            _mlContext = new MLContext();
            _models = new Dictionary<string, ITransformer>();
            _modelPath = "model/model.zip";// configuration["MLService:ModelPath"];
            _dataPath = "model/data.csv";// configuration["MLService:DataPath"];
            

            LoadDataAndModel();
        }


        // Дообучение на новых данных
        public void UpdateModel(ImageDigest digest, PredictionParameters parameters)
        {
            try
            {
                // Создание новой записи для обновления
                var newRecord = new TrainingData(digest, parameters);


                // Добавление новых данных
                var newDataView = _mlContext.Data.LoadFromEnumerable([newRecord]);
                _trainingData = _trainingData == null
                                                ? newDataView
                                                : _mlContext.Data.LoadFromEnumerable((new[] { _trainingData }).Append(newDataView));

                foreach (var _label in _labels)
                {
                    // Пайплайн для одного параметра
                    var pipeline = _mlContext.Transforms.Concatenate("Features",
                            nameof(TrainingData.brightness),
                            nameof(TrainingData.contrast),
                            nameof(TrainingData.noiseLevel),
                            nameof(TrainingData.sharpness),
                            nameof(TrainingData.aspectRatio))
                        .Append(_mlContext.Transforms.NormalizeMinMax("Features", fixZero: true))
                        .Append(_mlContext.Transforms.CopyColumns(outputColumnName:"Label", inputColumnName: _label))
                        .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features", maximumNumberOfIterations: 100));

                    // Обучение модели для одного параметра
                    _models[_label] = pipeline.Fit(_trainingData);

                }

                // Сохранение обновленной модели и данных
                SaveDataAndModel();
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during updating model: {ex.Message}");

            }
        }


        // Прогнозирование
        public PredictionParameters Predict(ImageDigest digest)
        {
            try
            {
                var prediction = new PredictionParameters();

                foreach (var _label in _labels)
                {
                    // Создаём PredictionEngine для каждой модели
                    var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageDigest, SinglePrediction>(_models[_label]);
                    var predictedValue = predictionEngine.Predict(digest).Score;

                    // Запись результата предсказания в соответствующее свойство
                    switch (_label)
                    {
                        case nameof(TrainingData.medianBlurKernel):
                            prediction.medianBlurKernel = predictedValue;
                            break;
                        case nameof(TrainingData.convScaleContrast):
                            prediction.convScaleContrast = predictedValue;
                            break;
                        case nameof(TrainingData.convScaleBrightness):
                            prediction.convScaleBrightness = predictedValue;
                            break;
                        case nameof(TrainingData.adThresBlock):
                            prediction.adThresBlock = predictedValue;
                            break;
                        case nameof(TrainingData.cannyThreshold1):
                            prediction.cannyThreshold1 = predictedValue;
                            break;
                        case nameof(TrainingData.cannyThreshold2):
                            prediction.cannyThreshold2 = predictedValue;
                            break;
                    }
                }

                return prediction;
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during prediction: {ex.Message}");
                // Return default or null to indicate failure
                return null;
            }
        }

        // Загрузка существующей модели и данных
        private void LoadDataAndModel()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    _trainingData = _mlContext.Data.LoadFromTextFile<TrainingData>(_dataPath, hasHeader: true, separatorChar: ',');
                }

                foreach (var _label in _labels)
                {
                    var modelPath = Path.Combine(Path.GetDirectoryName(_modelPath), $"{Path.GetFileNameWithoutExtension(_modelPath)}-{_label}{Path.GetExtension(_modelPath)}");
                    if (File.Exists(modelPath))
                    {
                        var model = _mlContext.Model.Load(modelPath, out _);
                        if (model != null)
                        {
                            _models[_label] = model;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during loading data and model: {ex.Message}");

            }
        }

        // Сохранение данных и модели
        private void SaveDataAndModel()
        {
            try
            {
                // Проверка наличия файла данных, если его нет, создаем
                if (!File.Exists(_dataPath))
                {
                    var directory = Path.GetDirectoryName(_dataPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.Create(_dataPath).Dispose();
                }

                // Сохранение данных
                using (var writer = new StreamWriter(_dataPath))
                {
                    // Запись заголовков
                    writer.WriteLine($"{nameof(TrainingData.brightness)},{nameof(TrainingData.contrast)},{nameof(TrainingData.noiseLevel)},{nameof(TrainingData.sharpness)},{nameof(TrainingData.aspectRatio)},{nameof(TrainingData.medianBlurKernel)},{nameof(TrainingData.convScaleContrast)},{nameof(TrainingData.convScaleBrightness)},{nameof(TrainingData.adThresBlock)},{nameof(TrainingData.cannyThreshold1)},{nameof(TrainingData.cannyThreshold2)}");

                    var data = _mlContext.Data.CreateEnumerable<TrainingData>(_trainingData, reuseRowObject: false);
                    foreach (var row in data)
                    {
                        writer.WriteLine($"{row.brightness},{row.contrast},{row.noiseLevel},{row.sharpness},{row.aspectRatio},{row.medianBlurKernel},{row.convScaleContrast},{row.convScaleBrightness},{row.adThresBlock},{row.cannyThreshold1},{row.cannyThreshold2}");
                    }
                }

                // Сохранение модели
                foreach (var _label in _labels)
                {
                    var newPathByModel = Path.Combine(Path.GetDirectoryName(_modelPath), $"{Path.GetFileNameWithoutExtension(_modelPath)}-{_label}{Path.GetExtension(_modelPath)}");
                    _mlContext.Model.Save(_models[_label], _trainingData.Schema, newPathByModel);
                }

            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during saving data and model: {ex.Message}");
            }
        }

        // Вспомогательные классы
        public class TrainingData
        {
            [LoadColumn(0)] public float brightness { get; set; }
            [LoadColumn(1)] public float contrast { get; set; }
            [LoadColumn(2)] public float noiseLevel { get; set; }
            [LoadColumn(3)] public float sharpness { get; set; }
            [LoadColumn(4)] public float aspectRatio { get; set; }
            [LoadColumn(5)] public float medianBlurKernel { get; set; }
            [LoadColumn(6)] public float convScaleContrast { get; set; }
            [LoadColumn(7)] public float convScaleBrightness { get; set; }
            [LoadColumn(8)] public float adThresBlock { get; set; }
            [LoadColumn(9)] public float cannyThreshold1 { get; set; }
            [LoadColumn(10)] public float cannyThreshold2 { get; set; }

            public TrainingData() { }

            public TrainingData(ImageDigest digest, PredictionParameters parameters)
            {
                brightness = (float)digest.brightness;
                contrast = (float)digest.contrast;
                noiseLevel = (float)digest.noiseLevel;
                sharpness = (float)digest.sharpness;
                aspectRatio = (float)digest.aspectRatio;
                medianBlurKernel = (float)parameters.medianBlurKernel;
                convScaleContrast = (float)parameters.convScaleContrast;
                convScaleBrightness = (float)parameters.convScaleBrightness;
                adThresBlock = (float)parameters.adThresBlock;
                cannyThreshold1 = (float)parameters.cannyThreshold1;
                cannyThreshold2 = (float)parameters.cannyThreshold2;
            }

            public float[] label
            {
                get => new[] { medianBlurKernel, convScaleContrast, convScaleBrightness, adThresBlock, cannyThreshold1, cannyThreshold2 };
                set
                {
                    if (value == null || value.Length != 6)
                    {
                        throw new ArgumentException("Label array must have exactly 6 elements.");
                    }
                    medianBlurKernel = value[0];
                    convScaleContrast = value[1];
                    convScaleBrightness = value[2];
                    adThresBlock = value[3];
                    cannyThreshold1 = value[4];
                    cannyThreshold2 = value[5];
                }
            }

        }
        private class SinglePrediction
        {
            public float Score { get; set; }
        }
    }

    

}
