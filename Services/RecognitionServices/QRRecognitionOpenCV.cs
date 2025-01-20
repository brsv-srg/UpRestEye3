using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Services.Recognition
{
    public interface IQRRecognition
    {
        QRCodeData DecodeQRCode(Bitmap sourceImage);
    }

    public class QRRecognitionOpenCV : IQRRecognition
    {
        public QRCodeData DecodeQRCode(Bitmap sourceImage)
        {
            var detector = new QRCodeDetector();
            var matImage = sourceImage.ToMat();

            bool isDetected = detector.DetectMulti(matImage, out Point2f[] points);
            if (isDetected)
            {
                isDetected = detector.DecodeMulti(matImage, points, out string?[] results);
                if (isDetected)
                {
                    // Успешно распознаны
                    foreach (var result in results)
                    {
                        if (result != null && QRCodeData.IsMatchingATQRCode(result))
                        {
                            // Успешно распознан QR-код с нужной структурой
                            return new QRCodeData(result);
                        }
                    }
                }
            }

            if (!isDetected)
            {
                var result = detector.DetectAndDecode(matImage, out Point2f[] pointsSimple);
                if (result != null && QRCodeData.IsMatchingATQRCode(result))
                {
                    // Успешно распознан QR-код с нужной структурой
                    return new QRCodeData(result);
                }
            }
            return null;
        }
    }
}
