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


namespace UpRestEye3.MLImageModels
{
    public class ImageDigest
    {
        public float brightness { get; set; }      // Уровень яркости
        public float contrast { get; set; }        // Уровень контраста
        public float noiseLevel { get; set; }      // Уровень шума
        public float sharpness { get; set; }       // Уровень резкости
        public float aspectRatio { get; set; }      // Соотношение сторон

    }


    public class ImageProcessingParameters
    {
        public double medianBlurKernel { get; set; } = 0.0;         // Для медианного фильтра - удаление шумов
        public double convScaleContrast { get; set; } = 0.0;        // Уровень контраста
        public double convScaleBrightness { get; set; } = 0.0;      // Уровень яркости
        public double adThreshBlock { get; set; } = 0.0;             // Для адаптивной бинаризации
        public double cannyThreshold1 { get; set; } = 0.0;          // Для обнаружения линий, нижняя граница, для QR 80
        public double cannyThreshold2 { get; set; } = 0.0;          // Для обнаружения линий, верхняя граница, для QR 160
        public double sharpWeightA { get; set; } = 0.0;              // Вес резкости альфа
        public double sharpWeightB { get; set; } = 0.0;              // Вес резкости бетта

        
        public bool Any()
        {
            return medianBlurKernel != 0 ||
                    convScaleContrast != 0.0 ||
                    convScaleBrightness != 0.0 ||
                    adThreshBlock != 0.0 ||
                    cannyThreshold1 != 0.0 ||
                    cannyThreshold2 != 0.0 ||
                    sharpWeightA !=0.0 ||
                    sharpWeightB !=0.0;
        }

        public void Clear()
        {
            medianBlurKernel = 0.0;
            convScaleContrast = 0.0;
            convScaleBrightness = 0.0;
            adThreshBlock = 0.0;
            cannyThreshold1 = 0.0;
            cannyThreshold2 = 0.0;
            sharpWeightA = 0.0;
            sharpWeightB = 0.0;
        }
    }

    public class ImageProcessingHypotheses 
    {

        // Список параметров для итераций
        // Для медианного фильтра - удаление шумов
        public double[] medianBlurKernels { get; } = { 0.0, 3, 5, 7 };
        
        // Уровень контраста
        public double[] convScaleContrasts { get; } = { 0.0, 1.7, 1.2, 1.5, 2.0 };

        // Уровень яркости
        public double[] convScaleBrightnesses { get; } = { 0.0, 20.0, -10.0, 10.0, -20.0 }; 
        
        // Для  бинаризации
        public double[] adThreshBlocks { get; } = { 0.0, 100, 127, 150 };

        // Для адаптивной бинаризации
        //double[] adThreshBlocks { get; } = { 0, 3, 7, 11 };                        

        // Для обнаружения линий
        public double[,] cannyThresholds { get; } = new double[,] { { 0.0, 0.0 }, { 80, 160 }, { 50, 150 }, { 10, 100 }, { 100, 200 } }; 

        // Вес резкости
        public double[,] sharpWeights { get; } = new double[,] { { 0.0, 0.0 }, { 1.5, -0.5 }, { 0.7, 0.3 } }; 
        
    }
}