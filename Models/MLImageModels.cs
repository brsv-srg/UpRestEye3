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
        public double Brightness { get; set; }      // Уровень яркости
        public double Contrast { get; set; }        // Уровень контраста
        public double NoiseLevel { get; set; }      // Уровень шума
        public double Sharpness { get; set; }       // Уровень резкости
        public float AspectRatio { get; set; }      // Соотношение сторон

    }

    public class ImageData
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
        {
            return medianBlurKernel != 0 ||
                    convScaleContrast != 0.0 ||
                    convScaleBrightness != 0.0 ||
                    adThresBlock != 0.0 ||
                    cannyThreshold1 != 0.0 ||
                    cannyThreshold2 != 0.0;
        }
    }
}