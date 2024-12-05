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

namespace UpRestEye3.Services
{
    public class ImageProcessor
    {

        private readonly MLAService _qrProcessor;

        public ImageProcessor(MLAService qrProcessor)
        {
            _qrProcessor = qrProcessor;
        }

        public bool ProcessImage(string imagePath)
        {
            Mat inputImage = Cv2.ImRead(imagePath);

            // Генерация дайджеста изображения
            var digest = _qrProcessor.GenerateDigest(inputImage);

            // Запрос параметров обработки у модели
            float[] parameters = _qrProcessor.GetParameters(digest);

            // Применение параметров обработки
            Mat processedImage = ApplyProcessing(inputImage, parameters);

            // Попытка распознавания QR-кода
            if (TryDecode(processedImage, out string decodedText))
            {
                Console.WriteLine($"QR-код распознан: {decodedText}");

                // Сохраняем успешные параметры
                digest.Threshold = parameters[1];
                digest.BlurStrength = parameters[2];
                digest.Success = true;
                _qrProcessor.UpdateModel(digest);

                return true;
            }
            else
            {
                Console.WriteLine("QR-код не удалось распознать.");
                return false;
            }
        }

        private Mat ApplyProcessing(Mat image, float[] parameters)
        {
            Mat grayImage = new Mat();
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // Применяем контраст
            Cv2.ConvertScaleAbs(grayImage, grayImage, parameters[0]);

            // Применяем размытие
            Cv2.GaussianBlur(grayImage, grayImage, new Size((int)parameters[2], (int)parameters[2]), 0);

            // Применяем бинаризацию
            Mat binaryImage = new Mat();
            Cv2.Threshold(grayImage, binaryImage, parameters[1], 255, ThresholdTypes.Binary);

            return binaryImage;
        }

        private bool TryDecode(Mat image, out string decodedText)
        {
            Point2f[] points;
            QRCodeDetector qrDetector = new QRCodeDetector();
            decodedText = qrDetector.DetectAndDecode(image, out points);
            return !string.IsNullOrEmpty(decodedText);
        }
    }
}
