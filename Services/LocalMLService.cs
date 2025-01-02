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
using static UpRestEye3.Services.LocalMLService;
using Tensorflow;
using Microsoft.ML.Data;
using Google.Api;


namespace UpRestEye3.Services
{
    // Интерфейс сервиса по работе с ML
    public interface ILocalMLService
    {
        void UpdateModel(ImageDigest digest, ImageProcessingParameters parameters);
        ImageProcessingParameters Predict(ImageDigest digest);
        ImageProcessingParameters GetProbable(ImageDigest digest);

    }

    //Класс создает и обучает модель машинного обучения
    public class LocalMLService: ILocalMLService
    {
        private readonly MLContext _mlContext;
        private readonly Dictionary<string, ITransformer> _models;
        private Dictionary<string, IDataView> _trainingData;
        private readonly string _modelPath;
        private readonly string _dataPath;
        private readonly string[] _labels = {
            nameof(ImageProcessingParameters.medianBlurKernel),
            nameof(ImageProcessingParameters.convScaleContrast),
            nameof(ImageProcessingParameters.convScaleBrightness),
            nameof(ImageProcessingParameters.adThreshBlock),
            nameof(ImageProcessingParameters.cannyThreshold1),
            nameof(ImageProcessingParameters.cannyThreshold2),
            nameof(ImageProcessingParameters.sharpWeightA),
            nameof(ImageProcessingParameters.sharpWeightB)};

        public LocalMLService()
        {
            _mlContext = new MLContext();
            _models = new Dictionary<string, ITransformer>();
            _trainingData = new Dictionary<string, IDataView>();
            _modelPath = "model/model.zip";// configuration["LocalMLService:ModelPath"];
            _dataPath = "model/data.csv";// configuration["LocalMLService:DataPath"];
            

            LoadDataAndModel();
        }


        // Дообучение на новых данных
        public void UpdateModel(ImageDigest digest, ImageProcessingParameters parameters)
        {
            try
            {
                foreach (var _label in _labels)
                {
                    var imageForUpdate = new ImageData(digest, new ImageDataPrediction());
                    // Создание новой записи для обновления
                    switch (_label)
                    {
                        case nameof(ImageProcessingParameters.medianBlurKernel):
                            imageForUpdate.paramValue = (float)parameters.medianBlurKernel;
                            break;
                        case nameof(ImageProcessingParameters.convScaleContrast):
                            imageForUpdate.paramValue = (float)parameters.convScaleContrast; 
                            break;
                        case nameof(ImageProcessingParameters.convScaleBrightness):
                            imageForUpdate.paramValue = (float)parameters.convScaleBrightness;
                            break;
                        case nameof(ImageProcessingParameters.adThreshBlock):
                            imageForUpdate.paramValue = (float)parameters.adThreshBlock;
                            break;
                        case nameof(ImageProcessingParameters.cannyThreshold1):
                            imageForUpdate.paramValue = (float)parameters.cannyThreshold1;
                            break;
                        case nameof(ImageProcessingParameters.cannyThreshold2):
                            imageForUpdate.paramValue = (float)parameters.cannyThreshold2;
                            break;
                   case nameof(ImageProcessingParameters.sharpWeightA):
                            imageForUpdate.paramValue = (float)parameters.sharpWeightA;
                            break;
                   case nameof(ImageProcessingParameters.sharpWeightB):
                            imageForUpdate.paramValue = (float)parameters.sharpWeightB;
                            break;
                    }
                   
                    // Добавление новых данных
                    var newDataView = _mlContext.Data.LoadFromEnumerable([imageForUpdate]);                

                    if(_trainingData.ContainsKey(_label))
                        _trainingData[_label] = _mlContext.Data.LoadFromEnumerable((new[] { _trainingData[_label] }).Append(newDataView));
                    else
                        _trainingData[_label] = newDataView;
                    
                    // Пайплайн для одного параметра
                    var pipeline = _mlContext.Transforms.Concatenate("Features",
                            nameof(ImageData.brightness),
                            nameof(ImageData.contrast),
                            nameof(ImageData.noiseLevel),
                            nameof(ImageData.sharpness),
                            nameof(ImageData.aspectRatio))
                        .Append(_mlContext.Transforms.NormalizeMinMax("Features", fixZero: true))
                        .Append(_mlContext.Transforms.CopyColumns(outputColumnName:"Label", inputColumnName: nameof(ImageData.paramValue)))
                        .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features", maximumNumberOfIterations: 100));

                    // Обучение модели для одного параметра
                    _models[_label] = pipeline.Fit(_trainingData[_label]);
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
        public ImageProcessingParameters Predict(ImageDigest digest)
        {
            try
            {
                var prediction = new ImageProcessingParameters();

                foreach (var _label in _labels)
                {
                    // Создаём PredictionEngine для каждой модели
                    var imageForPredict = new ImageData(digest, new ImageDataPrediction());
                    var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, ImageDataPrediction>(_models[_label]);
                    var predictedValue = predictionEngine.Predict(imageForPredict).paramValue;

                    // Запись результата предсказания в соответствующее свойство
                    switch (_label)
                    {
                        case nameof(ImageProcessingParameters.medianBlurKernel):
                            prediction.medianBlurKernel = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.convScaleContrast):
                            prediction.convScaleContrast = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.convScaleBrightness):
                            prediction.convScaleBrightness = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.adThreshBlock):
                            prediction.adThreshBlock = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.cannyThreshold1):
                            prediction.cannyThreshold1 = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.cannyThreshold2):
                            prediction.cannyThreshold2 = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.sharpWeightA):
                            prediction.sharpWeightA = predictedValue;
                            break;
                        case nameof(ImageProcessingParameters.sharpWeightB):
                            prediction.sharpWeightB = predictedValue;
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
        // Прогнозирование
        public ImageProcessingParameters GetProbable(ImageDigest digest)
        {
            try
            {
                var prediction = new ImageProcessingParameters();


                // Адаптивная коррекция яркости и контраста
                //if (metrics.Brightness < 100)
                //{
                //    convScaleContrast 1.2, convScaleBrightness 10
                //}
                //else if (metrics.Brightness > 200)
                //{
                //    convScaleContrast 0.8, convScaleBrightness - 10
                //}


                // Вычисляем параметры на основе метрик
                prediction.convScaleContrast = 1.0;         // начальное значение для контраста
                prediction.convScaleBrightness = 0.0;       // начальное значение для яркости

                // Настройка для темных изображений
                if (digest.brightness < 100)
                {
                    // Плавная настройка alpha от 1.0 до 1.4
                    prediction.convScaleContrast = 1.0 + ((100 - digest.brightness) / 100.0) * 0.4;
                    // Плавная настройка beta от 0 до 30
                    prediction.convScaleBrightness = ((100 - digest.brightness) / 100.0) * 30;
                }
                // Настройка для светлых изображений
                else if (digest.brightness > 200)
                {
                    // Плавная настройка alpha от 1.0 до 0.6
                    prediction.convScaleContrast = 1.0 - ((digest.brightness - 200) / 55.0) * 0.4;
                    // Плавная настройка beta от 0 до -30
                    prediction.convScaleBrightness = -((digest.brightness - 200) / 55.0) * 30;
                }

                // Учитываем контраст
                if (digest.contrast < 30)
                {
                    // Увеличиваем alpha для повышения контраста
                    prediction.convScaleContrast *= 1.2;
                }



                // Адаптивное шумоподавление
                 if (digest.noiseLevel > 10) 
                    prediction.medianBlurKernel = (int)(digest.noiseLevel / 2);

                // Адаптивное усиление резкости в зависимости от уровня размытия
                if (digest.sharpness < 10)
                    prediction.sharpLevel = 1.0;
                else
                    prediction.sharpLevel = 1 + (digest.sharpness / 10.0);


                // Адаптивная бинаризация
                // adThreshBlock = metrics.Contrast < 30 ? 15 : 11;
                // double C = metrics.Contrast < 30 ? 4 : 2;

                // Адаптивная настройка размера блока
                prediction.adThreshBlock = Math.Max(3, Math.Min(19, (int)(11 + (30 - digest.contrast) / 2)));
                prediction.adThreshBlock += (prediction.adThreshBlock % 2 == 0) ? 1 : 0; // Убеждаемся что нечетное

                // Адаптивная настройка константы C
                prediction.adThreshC = Math.Max(1, Math.Min(5, 2 + (30 - digest.contrast) / 10));


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
                foreach (var _label in _labels)
                {
                    var dataPath = Path.Combine(Path.GetDirectoryName(_dataPath), $"{Path.GetFileNameWithoutExtension(_dataPath)}-{_label}{Path.GetExtension(_dataPath)}");
                    if (File.Exists(dataPath))
                    {
                        _trainingData[_label] = _mlContext.Data.LoadFromTextFile<ImageData>(dataPath, hasHeader: true, separatorChar: ',');
                    }

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
                // Сохранение модели
                foreach (var _label in _labels)
                {
                    var dataPath = Path.Combine(Path.GetDirectoryName(_dataPath), $"{Path.GetFileNameWithoutExtension(_dataPath)}-{_label}{Path.GetExtension(_dataPath)}");

                    // Проверка наличия файла данных, если его нет, создаем
                    if (!File.Exists(dataPath))
                    {
                        var directory = Path.GetDirectoryName(dataPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        File.Create(dataPath).Dispose();
                    }

                    // Сохранение данных
                    using (var writer = new StreamWriter(dataPath))
                    {
                        // Запись заголовков
                        writer.WriteLine($"{nameof(ImageData.brightness)},{nameof(ImageData.contrast)},{nameof(ImageData.noiseLevel)},{nameof(ImageData.sharpness)},{nameof(ImageData.aspectRatio)},{nameof(ImageData.paramValue)}");

                        var data = _mlContext.Data.CreateEnumerable<ImageData>(_trainingData[_label], reuseRowObject: false);
                        foreach (var row in data)
                        {
                            writer.WriteLine($"{row.brightness},{row.contrast},{row.noiseLevel},{row.sharpness},{row.aspectRatio},{row.paramValue}");
                        }
                    }

                
                    var newPathByModel = Path.Combine(Path.GetDirectoryName(_modelPath), $"{Path.GetFileNameWithoutExtension(_modelPath)}-{_label}{Path.GetExtension(_modelPath)}");
                    _mlContext.Model.Save(_models[_label], _trainingData[_label].Schema, newPathByModel);
                }

            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during saving data and model: {ex.Message}");
            }
        }

        // Вспомогательные классы
        public class ImageData
        {
            [LoadColumn(0)] public float brightness { get; set; }
            [LoadColumn(1)] public float contrast { get; set; }
            [LoadColumn(2)] public float noiseLevel { get; set; }
            [LoadColumn(3)] public float sharpness { get; set; }
            [LoadColumn(4)] public float aspectRatio { get; set; }
            [LoadColumn(5)] public float paramValue { get; set; }

            public ImageData() { }

            public ImageData(ImageDigest digest, ImageDataPrediction parameter)
            {
                brightness = (float)digest.brightness;
                contrast = (float)digest.contrast;
                noiseLevel = (float)digest.noiseLevel;
                sharpness = (float)digest.sharpness;
                aspectRatio = (float)digest.aspectRatio;
                paramValue = (float)parameter.paramValue;
            }
        }
        public class ImageDataPrediction
        {
            [ColumnName("Label")]
            public float paramValue { get; set; }
        }
    }

    

}
